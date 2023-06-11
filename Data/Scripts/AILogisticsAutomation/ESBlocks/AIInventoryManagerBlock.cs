using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Game;
using System.Collections.Generic;
using System;
using Sandbox.Game.Entities;
using System.Linq;
using Sandbox.Common.ObjectBuilders;
using VRage.Game.ModAPI;
using VRage;
using Sandbox.Game;
using Sandbox.Definitions;
using System.Collections.Concurrent;
using VRageMath;
using Sandbox.Game.Gui;
using Sandbox.Common.ObjectBuilders.Definitions;

namespace AILogisticsAutomation
{

    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_OreDetector), false, "AIInventoryManager", "AIInventoryManagerReskin")]
    public class AIInventoryManagerBlock : BaseAIBlock<IMyOreDetector, AIInventoryManagerSettings, AIInventoryManagerSettingsData>
    {

        private const float IDEAL_COMPOSTER_ORGANIC = 100;
        private const float IDEAL_FISHTRAP_BAIT = 5;
        private const float IDEAL_FISHTRAP_NOBLEBAIT = 2.5f;

        private readonly ConcurrentDictionary<long, MyInventoryMap> inventoryMap = new ConcurrentDictionary<long, MyInventoryMap>();
        
        public MyInventoryMap GetMap(long entityId)
        {
            if (inventoryMap.ContainsKey(entityId))
                return inventoryMap[entityId];
            return null;
        }

        public MyFixedPoint GetItemAmount(MyDefinitionId id)
        {
            MyFixedPoint c = 0;
            foreach (var key in inventoryMap.Keys)
            {
                var mapItem = inventoryMap[key].GetItem(id);
                if (mapItem != null)
                    c += mapItem.TotalAmount;
            }
            return c;
        }

        protected override bool GetHadWorkToDo()
        {
            return Settings?.GetDefinitions().Any() ?? false;
        }

        protected override bool GetIsValidToWork()
        {
            return true;
        }

        protected override void OnInit(MyObjectBuilder_EntityBase objectBuilder)
        {
            Settings = new AIInventoryManagerSettings();
            base.OnInit(objectBuilder);
            NeedsUpdate |= MyEntityUpdateEnum.EACH_100TH_FRAME;
        }

        private void PopulateSubGrids(MyCubeGrid target, ConcurrentDictionary<long, MyCubeGrid> store)
        {
            var lista = target.GetConnectedGrids(GridLinkTypeEnum.Mechanical).Where(x => x.EntityId != Grid.EntityId && CountAIInventoryManager(x) == 0).ToList();
            foreach (var item in lista)
            {
                if (!store.ContainsKey(item.EntityId))
                {
                    store[item.EntityId] = item;
                    PopulateSubGrids(item, store);
                }
            }
        }

        private ConcurrentDictionary<long, MyCubeGrid> GetSubGrids(MyCubeGrid target)
        {
            var grids = new ConcurrentDictionary<long, MyCubeGrid>();
            PopulateSubGrids(target, grids);
            return grids;
        }

        private ConcurrentDictionary<long, MyCubeGrid> GetSubGrids()
        {
            return GetSubGrids(CubeGrid);
        }

        public struct ShipConnected
        {

            public IMyShipConnector Connector;
            public MyCubeGrid Grid;

            public ShipConnected(IMyShipConnector connector)
            {
                Connector = connector;
                Grid = connector.CubeGrid as MyCubeGrid;
            }

        }

        private Dictionary<IMyShipConnector, ShipConnected> GetConnectedGrids()
        {
            return GetConnectedGrids(Grid);
        }

        private Dictionary<IMyShipConnector, ShipConnected> GetConnectedGrids(IMyCubeGrid target)
        {
            var data = new Dictionary<IMyShipConnector, ShipConnected>();
            List<IMySlimBlock> connectors = new List<IMySlimBlock>();
            if (ExtendedSurvivalCoreAPI.Registered)
            {
                connectors = ExtendedSurvivalCoreAPI.GetGridBlocks(target.EntityId, typeof(MyObjectBuilder_ShipConnector), null);
            }
            else
            {
                target.GetBlocks(connectors, x => x.BlockDefinition.Id.TypeId == typeof(MyObjectBuilder_ShipConnector));
            }
            if (connectors != null && connectors.Any())
            {
                foreach (var connector in connectors)
                {
                    var c = (connector.FatBlock as IMyShipConnector);
                    if (!data.ContainsKey(c) && c.IsConnected && c.OtherConnector.CubeGrid.EntityId != target.EntityId && CountAIInventoryManager(c.OtherConnector.CubeGrid) == 0)
                    {
                        data.Add(c, new ShipConnected(c.OtherConnector));
                    }
                }
            }
            return data;
        }

        public IEnumerable<MyCubeBlock> ValidInventories
        {
            get
            {
                return DoApplyBasicFilter(CubeGrid.Inventories, new long[] { });
            }
        }

        public IEnumerable<MyCubeBlock> ValidInventoriesWithNoFunctional
        {
            get
            {
                return DoApplyBasicFilter(CubeGrid.Inventories, new long[] { }, true);
            }
        }

        private IEnumerable<MyCubeBlock> DoApplyBasicFilter(HashSet<MyCubeBlock> inventories, IEnumerable<long> customIgnoreList, bool ignoreFunctional = false)
        {
            return inventories.Where(x =>
                (
                    (x.IsFunctional && ((x as IMyFunctionalBlock)?.Enabled ?? true)) ||
                    ignoreFunctional
                ) &&
                !customIgnoreList.Contains(x.EntityId) &&
                !Settings.GetIgnoreCargos().Contains(x.EntityId) &&
                !Settings.GetIgnoreFunctionalBlocks().Contains(x.EntityId) &&
                !Settings.GetIgnoreConnectors().Contains(x.EntityId) &&
                !x.BlockDefinition.Id.IsHydrogenEngine() &&
                !x.BlockDefinition.Id.IsParachute() &&
                !x.BlockDefinition.Id.IsGun() &&
                !x.BlockDefinition.Id.IsTurret() &&
                (Settings.GetPullFromComposter() || !x.BlockDefinition.Id.IsComposter()) &&
                (Settings.GetPullFishTrap() || !x.BlockDefinition.Id.IsFishTrap()) &&
                (Settings.GetPullRefrigerator() || !x.BlockDefinition.Id.IsRefrigerator()) &&
                (Settings.GetPullFromRefinary() || !x.BlockDefinition.Id.IsRefinery()) &&
                (Settings.GetPullFromAssembler() || !x.BlockDefinition.Id.IsAssembler()) &&
                (Settings.GetPullFromReactor() || !x.BlockDefinition.Id.IsReactor()) &&
                (Settings.GetPullFromGasGenerator() || !x.BlockDefinition.Id.IsGasGenerator()) &&
                (Settings.GetPullFromGasTank() || !x.BlockDefinition.Id.IsGasTank())
            );
        }

        private float CalcPowerFromBlocks(float power, IEnumerable<MyCubeBlock> blocks)
        {
            var totalInventories = blocks.Count();

            power += AILogisticsAutomationSettings.Instance.EnergyCost.DefaultBlockCost * totalInventories;

            if (Settings.GetFillReactor())
            {
                var totalReactors = blocks.Count(x => x.BlockDefinition.Id.IsReactor());
                power += AILogisticsAutomationSettings.Instance.EnergyCost.FillReactorCost * totalReactors;
            }

            if (Settings.GetFillGasGenerator())
            {
                var totalGasGenerator = blocks.Count(x => x.BlockDefinition.Id.IsGasGenerator());
                power += AILogisticsAutomationSettings.Instance.EnergyCost.FillGasGeneratorCost * totalGasGenerator;
            }

            if (Settings.GetFillRefrigerator())
            {
                var totalReactors = blocks.Count(x => x.BlockDefinition.Id.IsRefrigerator());
                power += AILogisticsAutomationSettings.Instance.EnergyCost.ExtendedSurvival.FillRefrigeratorCost * totalReactors;
            }

            if (Settings.GetFillFishTrap())
            {
                var totalReactors = blocks.Count(x => x.BlockDefinition.Id.IsFishTrap());
                power += AILogisticsAutomationSettings.Instance.EnergyCost.ExtendedSurvival.FillFishTrapCost * totalReactors;
            }

            if (Settings.GetFillComposter())
            {
                var totalReactors = blocks.Count(x => x.BlockDefinition.Id.IsComposter());
                power += AILogisticsAutomationSettings.Instance.EnergyCost.ExtendedSurvival.FillComposterCost * totalReactors;
            }

            if (Settings.GetFillBottles())
            {
                var totalBottleTargets = blocks.Count(x => x.BlockDefinition.Id.IsBottleTaget());
                power += AILogisticsAutomationSettings.Instance.EnergyCost.FillBottlesCost * totalBottleTargets;
            }

            return power;
        }

        private float GetPowerConsumption(out List<MyCubeGrid> subgrids, out Dictionary<IMyShipConnector, ShipConnected> connectedGrids)
        {
            subgrids = null;
            connectedGrids = null;

            if (!IsValidToWork)
                return 0;

            // Get base power
            var power = CalcPowerFromBlocks(0, ValidInventories);

            // Get pull containers power
            power += (
                        AILogisticsAutomationSettings.Instance.EnergyCost.DefaultPullCost + 
                        (Settings.GetSortItensType() > 0 ? AILogisticsAutomationSettings.Instance.EnergyCost.SortCost : 0) +
                        (Settings.GetStackIfPossible() ? AILogisticsAutomationSettings.Instance.EnergyCost.StackCost : 0)
                    ) * Settings.GetDefinitions().Count;
            
            // Get filter power
            var totalFilters = Settings.GetDefinitions().Values.Sum(x => x.IgnoreIds.Count + x.IgnoreTypes.Count + x.ValidTypes.Count + x.ValidIds.Count);
            power += AILogisticsAutomationSettings.Instance.EnergyCost.FilterCost * totalFilters;

            // Get subgrids
            if (Settings.GetPullSubGrids())
            {
                subgrids = GetSubGrids().Values.ToList();
                foreach (var grid in subgrids)
                {
                    var query = DoApplyBasicFilter(grid.Inventories, new long[] { });
                    power = CalcPowerFromBlocks(power, query);
                }
            }

            // Get connected grids
            if (Settings.GetPullFromConnectedGrids())
            {
                connectedGrids = GetConnectedGrids();
                foreach (var connector in connectedGrids.Keys)
                {
                    if (!Settings.GetIgnoreConnectors().Contains(connector.EntityId))
                    {
                        var query = DoApplyBasicFilter(connectedGrids[connector].Grid.Inventories, new long[] { });
                        power = CalcPowerFromBlocks(power, query);
                    }
                }
            }

            return power;
        }

        private void DoCheckInventoryList(MyCubeBlock[] listaToCheck, ref List<IMyReactor> reactors, ref List<IMyGasGenerator> gasGenerators, 
            ref List<IMyGasTank> gasTanks, ref List<IMyGasGenerator> composters, ref List<IMyGasGenerator> fishTraps, ref List<IMyGasGenerator> refrigerators)
        {
            for (int i = 0; i < listaToCheck.Length; i++)
            {

                if (listaToCheck[i].BlockDefinition.Id.IsGasTank() && !Settings.GetPullFromGasTank())
                    continue;

                if (listaToCheck[i].BlockDefinition.Id.IsGasGenerator() && !Settings.GetPullFromGasGenerator())
                    continue;

                if (listaToCheck[i].BlockDefinition.Id.IsReactor() && !Settings.GetPullFromReactor())
                    continue;

                if (listaToCheck[i].BlockDefinition.Id.IsAssembler() && !Settings.GetPullFromAssembler())
                    continue;

                if (listaToCheck[i].BlockDefinition.Id.IsRefinery() && !Settings.GetPullFromRefinary())
                    continue;

                // Pula inventorios de despejo
                if (Settings.GetDefinitions().Values.Any(x => x.EntityId == listaToCheck[i].EntityId))
                    continue;

                int targetInventory = 0;
                IMyReactor reactor = null;
                if (listaToCheck[i].BlockDefinition.Id.IsReactor())
                {
                    reactor = (listaToCheck[i] as IMyReactor);
                    if (!reactors.Contains(reactor))
                        reactors.Add(reactor);
                }
                IMyGasGenerator gasGenerator = null;
                if (listaToCheck[i].BlockDefinition.Id.IsGasGenerator())
                {
                    gasGenerator = (listaToCheck[i] as IMyGasGenerator);
                    if (!gasGenerators.Contains(gasGenerator))
                        gasGenerators.Add(gasGenerator);
                }
                if (listaToCheck[i].BlockDefinition.Id.IsFishTrap())
                {
                    gasGenerator = (listaToCheck[i] as IMyGasGenerator);
                    if (!fishTraps.Contains(gasGenerator))
                        fishTraps.Add(gasGenerator);
                }
                if (listaToCheck[i].BlockDefinition.Id.IsRefrigerator())
                {
                    gasGenerator = (listaToCheck[i] as IMyGasGenerator);
                    if (!refrigerators.Contains(gasGenerator))
                        refrigerators.Add(gasGenerator);
                }
                if (listaToCheck[i].BlockDefinition.Id.IsComposter())
                {
                    gasGenerator = (listaToCheck[i] as IMyGasGenerator);
                    if (!composters.Contains(gasGenerator))
                        composters.Add(gasGenerator);
                }
                IMyGasTank gasTank = null;
                if (listaToCheck[i].BlockDefinition.Id.IsGasTank())
                {
                    gasTank = (listaToCheck[i] as IMyGasTank);
                    if (!gasTanks.Contains(gasTank))
                        gasTanks.Add(gasTank);
                }
                if (listaToCheck[i].BlockDefinition.Id.IsAssembler() || listaToCheck[i].BlockDefinition.Id.IsRefinery())
                    targetInventory = 1;
                var inventoryBase = listaToCheck[i].GetInventory(targetInventory);
                if (inventoryBase != null)
                {                    
                    if (inventoryBase.GetItemsCount() > 0)
                    {
                        // Move itens para o iventário possivel
                        TryToFullFromInventory(listaToCheck[i].BlockDefinition.Id, inventoryBase, listaToCheck, null, reactor, gasGenerator);
                        // Verifica se tem algo nos inventários de produção, se for uma montadora
                        if (listaToCheck[i].BlockDefinition.Id.IsAssembler())
                        {
                            var assembler = (listaToCheck[i] as IMyAssembler);
                            var inventoryProd = listaToCheck[i].GetInventory(0);
                            TryToFullFromInventory(listaToCheck[i].BlockDefinition.Id, inventoryProd, listaToCheck, assembler, null, null);
                        }
                    }
                    if (inventoryBase.GetItemsCount() > 0)
                        DoSort(inventoryBase);
                }

            }
        }

        private void DoTryFillBottle(MyInventoryMap map, MyInventoryMap.MyInventoryMapEntry bottleMap, MyDefinitionId targetGas, List<IMyGasGenerator> gasGenerators, List<IMyGasTank> gasTanks)
        {
            if (bottleMap != null)
            {
                var gasTankQuery = gasTanks.Where(x => x.FilledRatio > 0 && x.BlockDefinition.IsGasTank(targetGas) && !x.GetInventory().IsFull);
                var gasGeneratorQuery = gasGenerators.Where(x => !x.GetInventory().IsFull && x.GetInventory().GetItemAmount(ItensConstants.ICE_ID.DefinitionId) > 0);
                foreach (var itemId in bottleMap.Slots)
                {
                    var bottleItem = map.Inventory.GetItemByID(itemId);
                    if (bottleItem.HasValue)
                    {
                        var bottleContent = bottleItem.Value.Content as MyObjectBuilder_GasContainerObject;
                        if (bottleContent.GasLevel < 1)
                        {
                            if (gasTankQuery.Any())
                            {
                                foreach (var targetTank in gasTankQuery)
                                {
                                    var targetInventory = (MyInventory)targetTank.GetInventory();
                                    if ((map.Inventory as IMyInventory).CanTransferItemTo(targetInventory, bottleMap.ItemId))
                                    {
                                        InvokeOnGameThread(() =>
                                        {
                                            MyInventory.Transfer(map.Inventory, targetInventory, itemId);
                                        });
                                        targetTank.RefillBottles();
                                        break;
                                    }
                                }
                            }
                            else if (gasGeneratorQuery.Any())
                            {
                                foreach (var targetGasGen in gasGeneratorQuery)
                                {
                                    var targetInventory = (MyInventory)targetGasGen.GetInventory();
                                    if ((map.Inventory as IMyInventory).CanTransferItemTo(targetInventory, bottleMap.ItemId))
                                    {
                                        InvokeOnGameThread(() =>
                                        {
                                            MyInventory.Transfer(map.Inventory, targetInventory, itemId);
                                        });
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private void DoFillBottles(List<IMyGasGenerator> gasGenerators, List<IMyGasTank> gasTanks)
        {
            if (Settings.GetFillBottles())
            {
                foreach (var key in inventoryMap.Keys)
                {
                    var h2Bottle = inventoryMap[key].GetItem(ItensConstants.HYDROGENBOTTLE_ID.DefinitionId);
                    DoTryFillBottle(inventoryMap[key], h2Bottle, ItensConstants.HYDROGEN_ID.DefinitionId, gasGenerators, gasTanks);
                    var o2Bottle = inventoryMap[key].GetItem(ItensConstants.OXYGENBOTTLE_ID.DefinitionId);
                    DoTryFillBottle(inventoryMap[key], o2Bottle, ItensConstants.OXYGENBOTTLE_ID.DefinitionId, gasGenerators, gasTanks);
                }
            }
        }

        private void DoFillFishTrap(List<IMyGasGenerator> fishTraps)
        {
            if (Settings.GetFillFishTrap())
            {
                foreach (var fishTrap in fishTraps)
                {
                    var fishTrapDef = MyDefinitionManager.Static.GetCubeBlockDefinition(fishTrap.BlockDefinition) as MyOxygenGeneratorDefinition;
                    var fishTrapInventory = fishTrap.GetInventory(0) as MyInventory;
                    if (fishTrapInventory.VolumeFillFactor >= 1)
                        continue;
                    var size = fishTrapDef.Size.X * fishTrapDef.Size.Y * fishTrapDef.Size.Z;
                    var targetFuelsId = new Dictionary<MyDefinitionId, float> 
                    {
                        { ItensConstants.FISH_NOBLE_BAIT_ID.DefinitionId, IDEAL_FISHTRAP_NOBLEBAIT },
                        { ItensConstants.FISH_BAIT_SMALL_ID.DefinitionId, IDEAL_FISHTRAP_BAIT }
                    };
                    foreach (var targetFuelId in targetFuelsId.Keys)
                    {
                        var fuelInFishTrap = (float)fishTrapInventory.GetItemAmount(targetFuelId);
                        var value = targetFuelsId[targetFuelId];
                        var targetFuel = value * size;
                        if (targetFuel > 0)
                        {
                            if (fuelInFishTrap < targetFuel)
                            {
                                var fuelToAdd = targetFuel - fuelInFishTrap;
                                var keys = Settings.GetDefinitions().Keys.ToArray();
                                for (int i = 0; i < keys.Length; i++)
                                {
                                    var def = Settings.GetDefinitions()[keys[i]];
                                    var targetBlock = ValidInventories.FirstOrDefault(x => x.EntityId == def.EntityId);
                                    var targetInventory = targetBlock.GetInventory(0);
                                    var fuelAmount = (float)targetInventory.GetItemAmount(targetFuelId);
                                    if (fuelAmount > 0)
                                    {
                                        if ((targetInventory as IMyInventory).CanTransferItemTo(fishTrapInventory, targetFuelId))
                                        {
                                            var builder = ItensConstants.GetPhysicalObjectBuilder(new UniqueEntityId(targetFuelId));
                                            var amountToTransfer = fuelAmount > fuelToAdd ? fuelToAdd : fuelAmount;
                                            InvokeOnGameThread(() =>
                                            {
                                                MyInventory.Transfer(targetInventory, fishTrapInventory, targetFuelId, amount: (MyFixedPoint)amountToTransfer);
                                            });
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }                    
                }
            }
        }

        private void DoFillComposter(List<IMyGasGenerator> composters)
        {
            if (Settings.GetFillComposter())
            {
                foreach (var composter in composters)
                {
                    var composterDef = MyDefinitionManager.Static.GetCubeBlockDefinition(composter.BlockDefinition) as MyOxygenGeneratorDefinition;
                    var targetFuelId = ItensConstants.ORGANIC_ID.DefinitionId;
                    var composterInventory = composter.GetInventory(0) as MyInventory;
                    if (composterInventory.VolumeFillFactor >= 1)
                        continue;
                    var fuelInComposter = (float)composterInventory.GetItemAmount(targetFuelId);
                    var size = composterDef.Size.X * composterDef.Size.Y * composterDef.Size.Z;
                    var value = IDEAL_COMPOSTER_ORGANIC;
                    var targetFuel = value * size;
                    if (targetFuel > 0)
                    {
                        if (fuelInComposter < targetFuel)
                        {
                            var fuelToAdd = targetFuel - fuelInComposter;
                            var keys = Settings.GetDefinitions().Keys.ToArray();
                            for (int i = 0; i < keys.Length; i++)
                            {
                                var def = Settings.GetDefinitions()[keys[i]];
                                var targetBlock = ValidInventories.FirstOrDefault(x => x.EntityId == def.EntityId);
                                var targetInventory = targetBlock.GetInventory(0);
                                var fuelAmount = (float)targetInventory.GetItemAmount(targetFuelId);
                                if (fuelAmount > 0)
                                {
                                    if ((targetInventory as IMyInventory).CanTransferItemTo(composterInventory, targetFuelId))
                                    {
                                        var builder = ItensConstants.GetPhysicalObjectBuilder(new UniqueEntityId(targetFuelId));
                                        var amountToTransfer = fuelAmount > fuelToAdd ? fuelToAdd : fuelAmount;
                                        InvokeOnGameThread(() =>
                                        {
                                            MyInventory.Transfer(targetInventory, composterInventory, targetFuelId, amount: (MyFixedPoint)amountToTransfer);
                                        });
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private void DoFillRefrigerator(List<IMyGasGenerator> refrigerators)
        {
            if (Settings.GetFillRefrigerator())
            {
                MyObjectBuilderType targetType = typeof(MyObjectBuilder_ConsumableItem);
                var query = Settings.GetDefinitions().Values.Where(x => x.ValidTypes.Contains(targetType) || x.ValidIds.Any(y => y.TypeId == targetType));
                if (query.Any())
                {
                    foreach (var def in query)
                    {
                        var targetBlock = ValidInventories.FirstOrDefault(x => x.EntityId == def.EntityId);
                        var targetInventory = targetBlock.GetInventory(0);
                        if (targetInventory.VolumeFillFactor >= 1)
                            continue;
                        if (targetInventory.ItemCount > 0)
                        {
                            /* start in the end */
                            for (int i = targetInventory.ItemCount - 1; i >= 0; i--)
                            {
                                var item = targetInventory.GetItemAt(i);
                                if (item.HasValue)
                                {
                                    MyObjectBuilderType itemType;
                                    if (MyObjectBuilderType.TryParse(item.Value.Type.TypeId, out itemType))
                                    {
                                        if (itemType == targetType)
                                        {
                                            foreach (var refrigerator in refrigerators)
                                            {
                                                var refrigeratorInventory = refrigerator.GetInventory(0) as MyInventory;
                                                if ((targetInventory as IMyInventory).CanTransferItemTo(refrigeratorInventory, item.Value.Type))
                                                {
                                                    var itemSlot = targetInventory.GetItemByID(item.Value.ItemId);
                                                    if (itemSlot.HasValue)
                                                    {
                                                        InvokeOnGameThread(() =>
                                                        {
                                                            MyInventory.Transfer(targetInventory, refrigeratorInventory, itemSlot.Value.ItemId);
                                                        });
                                                        break;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private void DoFillGasGenerator(List<IMyGasGenerator> gasGenerators)
        {
            if (Settings.GetFillGasGenerator())
            {
                foreach (var gasGenerator in gasGenerators)
                {
                    var gasGeneratorDef = MyDefinitionManager.Static.GetCubeBlockDefinition(gasGenerator.BlockDefinition) as MyOxygenGeneratorDefinition;
                    var targetFuelId = ItensConstants.ICE_ID.DefinitionId;
                    var gasGeneratorInventory = gasGenerator.GetInventory(0) as MyInventory;
                    if (gasGeneratorInventory.VolumeFillFactor >= 1)
                        continue;
                    var fuelInGasGenerator = (float)gasGeneratorInventory.GetItemAmount(targetFuelId);
                    var size = gasGeneratorDef.Size.X * gasGeneratorDef.Size.Y * gasGeneratorDef.Size.Z;
                    var value = gasGeneratorDef.CubeSize == MyCubeSize.Large ? Settings.GetLargeGasGeneratorAmount() : Settings.GetSmallGasGeneratorAmount();
                    var targetFuel = value * size;
                    if (targetFuel > 0)
                    {
                        if (fuelInGasGenerator < targetFuel)
                        {
                            var fuelToAdd = targetFuel - fuelInGasGenerator;
                            var keys = Settings.GetDefinitions().Keys.ToArray();
                            for (int i = 0; i < keys.Length; i++)
                            {
                                var def = Settings.GetDefinitions()[keys[i]];
                                var targetBlock = ValidInventories.FirstOrDefault(x => x.EntityId == def.EntityId);
                                var targetInventory = targetBlock.GetInventory(0);
                                var fuelAmount = (float)targetInventory.GetItemAmount(targetFuelId);
                                if (fuelAmount > 0)
                                {
                                    if ((targetInventory as IMyInventory).CanTransferItemTo(gasGeneratorInventory, targetFuelId))
                                    {
                                        var builder = ItensConstants.GetPhysicalObjectBuilder(new UniqueEntityId(targetFuelId));
                                        var amountToTransfer = fuelAmount > fuelToAdd ? fuelToAdd : fuelAmount;
                                        InvokeOnGameThread(() =>
                                        {
                                            MyInventory.Transfer(targetInventory, gasGeneratorInventory, targetFuelId, amount: (MyFixedPoint)amountToTransfer);
                                        });
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private void DoFillReactors(List<IMyReactor> reactors)
        {
            if (Settings.GetFillReactor())
            {
                foreach (var reactor in reactors)
                {
                    var reactorDef = MyDefinitionManager.Static.GetCubeBlockDefinition(reactor.BlockDefinition) as MyReactorDefinition;
                    if (!reactorDef.FuelInfos.Any())
                        continue;
                    var targetFuelId = reactorDef.FuelInfos[0].FuelId;
                    var reactorInventory = reactor.GetInventory(0) as MyInventory;
                    if (reactorInventory.VolumeFillFactor >= 1)
                        continue;
                    var fuelInReactor = (float)reactorInventory.GetItemAmount(targetFuelId);
                    var size = reactorDef.Size.X * reactorDef.Size.Y * reactorDef.Size.Z;
                    var value = reactorDef.CubeSize == MyCubeSize.Large ? Settings.GetLargeReactorFuelAmount() : Settings.GetSmallReactorFuelAmount();
                    var targetFuel = value * size;
                    if (targetFuel > 0)
                    {
                        if (fuelInReactor < targetFuel)
                        {
                            var fuelToAdd = targetFuel - fuelInReactor;
                            var keys = Settings.GetDefinitions().Keys.ToArray();
                            for (int i = 0; i < keys.Length; i++)
                            {
                                var def = Settings.GetDefinitions()[keys[i]];
                                var targetBlock = ValidInventories.FirstOrDefault(x => x.EntityId == def.EntityId);
                                var targetInventory = targetBlock.GetInventory(0);
                                var fuelAmount = (float)targetInventory.GetItemAmount(targetFuelId);
                                if (fuelAmount > 0)
                                {
                                    if ((targetInventory as IMyInventory).CanTransferItemTo(reactorInventory, targetFuelId))
                                    {
                                        var builder = ItensConstants.GetPhysicalObjectBuilder(new UniqueEntityId(targetFuelId));
                                        var amountToTransfer = fuelAmount > fuelToAdd ? fuelToAdd : fuelAmount;
                                        InvokeOnGameThread(() =>
                                        {
                                            MyInventory.Transfer(targetInventory, reactorInventory, targetFuelId, amount: (MyFixedPoint)amountToTransfer);
                                        });
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private void DoCheckAnyCanGoInOtherInventory()
        {
            var keys = Settings.GetDefinitions().Keys.ToArray();
            for (int i = 0; i < keys.Length; i++)
            {
                var def = Settings.GetDefinitions()[keys[i]];
                var targetBlock = ValidInventories.FirstOrDefault(x => x.EntityId == def.EntityId);
                var targetInventory = targetBlock.GetInventory(0);
                if (targetInventory.ItemCount > 0)
                {
                    for (int p = targetInventory.ItemCount - 1; p >= 0; p--)
                    {
                        var item = targetInventory.GetItemAt(p);
                        if (!item.HasValue)
                            break;
                        var itemid = new MyDefinitionId(MyObjectBuilderType.Parse(item.Value.Type.TypeId), item.Value.Type.SubtypeId);
                        if (!(
                                (def.ValidIds.Contains(itemid) || def.ValidTypes.Contains(itemid.TypeId)) &&
                                !def.IgnoreIds.Contains(itemid) &&
                                !def.IgnoreTypes.Contains(itemid.TypeId)
                            ))
                        {
                            var validTargets = Settings.GetDefinitions().Values.Where(x =>
                                (x.ValidIds.Contains(itemid) || x.ValidTypes.Contains(itemid.TypeId)) &&
                                (!x.IgnoreIds.Contains(itemid) && !x.IgnoreTypes.Contains(itemid.TypeId))
                            );
                            if (validTargets.Any())
                            {
                                foreach (var validTarget in validTargets)
                                {
                                    var targetBlockToSend = ValidInventories.FirstOrDefault(x => x.EntityId == validTarget.EntityId);
                                    if (targetBlockToSend != null)
                                    {
                                        var targetInventoryToSend = targetBlockToSend.GetInventory(0);
                                        if ((targetInventory as IMyInventory).CanTransferItemTo(targetInventoryToSend, itemid) && targetInventoryToSend.VolumeFillFactor < 1)
                                        {
                                            InvokeOnGameThread(() =>
                                            {
                                                MyInventory.Transfer(targetInventory, targetInventoryToSend, item.Value.ItemId);
                                            });
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private void DoManageInventory(long ownerId, MyInventory targetInventory)
        {
            if (!inventoryMap.ContainsKey(ownerId))
                inventoryMap[ownerId] = new MyInventoryMap(ownerId, targetInventory);
            inventoryMap[ownerId].Update();
        }

        private void DoStacks(long ownerId)
        {
            if (Settings.GetStackIfPossible())
            {
                if (inventoryMap.ContainsKey(ownerId) && inventoryMap[ownerId].HadAnyStackable())
                {
                    var stackItens = inventoryMap[ownerId].GetStackableItems();
                    foreach (var item in stackItens)
                    {
                        var map = inventoryMap[ownerId].GetItem(item);
                        var targetSlot = map.Slots.FirstOrDefault();
                        var removeSlots = map.Slots.Where(x => x != targetSlot).ToArray();
                        var index = inventoryMap[ownerId].Inventory.GetItemIndexById(targetSlot);
                        InvokeOnGameThread(() =>
                        {
                            foreach (var slot in removeSlots)
                            {
                                MyInventory.Transfer(inventoryMap[ownerId].Inventory, inventoryMap[ownerId].Inventory, slot, index);
                            }
                        });
                        map.Slots.RemoveWhere(x => removeSlots.Contains(x));
                    }
                }
            }
        }

        private void DoSort(MyInventory targetInventory)
        {
            if (Settings.GetSortItensType() != 0)
            {
                if (targetInventory.ItemCount > 1)
                {
                    int p = 1;
                    while (p < targetInventory.ItemCount)
                    {
                        var itemBefore = targetInventory.GetItemAt(p - 1);
                        var item = targetInventory.GetItemAt(p);
                        if (!item.HasValue || !itemBefore.HasValue)
                            break;
                        var itemBeforeDef = MyDefinitionManager.Static.GetPhysicalItemDefinition(itemBefore.Value.Type);
                        var itemDef = MyDefinitionManager.Static.GetPhysicalItemDefinition(item.Value.Type);
                        int dif = 0;
                        MyFixedPoint itemTotalMass = 0;
                        MyFixedPoint itemBeforeTotalMass = 0;
                        switch (Settings.GetSortItensType())
                        {
                            case 1: /* NAME */
                                dif = itemDef.DisplayNameText.CompareTo(itemBeforeDef.DisplayNameText) * -1; /* A -> Z */
                                break;
                            case 2: /* MASS */
                                itemTotalMass = itemDef.Mass * item.Value.Amount;
                                itemBeforeTotalMass = itemBeforeDef.Mass * itemBefore.Value.Amount;
                                dif = itemTotalMass < itemBeforeTotalMass ? -1 : (itemTotalMass > itemBeforeTotalMass ? 1 : 0); /* + -> - */
                                break;
                            case 3: /* TYPE NAME (ITEM NAME) */
                                dif = itemDef.Id.TypeId.ToString().CompareTo(itemBeforeDef.Id.TypeId.ToString()) * -1; /* A -> Z */
                                if (dif == 0)
                                    dif = itemDef.DisplayNameText.CompareTo(itemBeforeDef.DisplayNameText) * -1; /* A -> Z */
                                break;
                            case 4: /* TYPE NAME (ITEM MASS) */
                                dif = itemDef.Id.TypeId.ToString().CompareTo(itemBeforeDef.Id.TypeId.ToString()) * -1; /* A -> Z */
                                if (dif == 0)
                                {
                                    itemTotalMass = itemDef.Mass * item.Value.Amount;
                                    itemBeforeTotalMass = itemBeforeDef.Mass * itemBefore.Value.Amount;
                                    dif = itemTotalMass < itemBeforeTotalMass ? -1 : (itemTotalMass > itemBeforeTotalMass ? 1 : 0); /* + -> - */
                                }
                                break;
                        }
                        if (dif > 0)
                        {
                            InvokeOnGameThread(() =>
                            {
                                MyInventory.Transfer(targetInventory, targetInventory, item.Value.ItemId, p - 1);
                            });
                        }
                        else
                            p++;
                    }
                }
            }
        }

        private void DoCheckPullInventories()
        {
            var keys = Settings.GetDefinitions().Keys.ToArray();
            for (int i = 0; i < keys.Length; i++)
            {
                var def = Settings.GetDefinitions()[keys[i]];
                var targetBlock = ValidInventories.FirstOrDefault(x => x.EntityId == def.EntityId);
                var targetInventory = targetBlock.GetInventory(0);
                DoSort(targetInventory);
                DoManageInventory(keys[i], targetInventory);
                DoStacks(keys[i]);
            }
        }

        private void CheckEntitiesExist()
        {
            bool needComuniteChange = false;
            var entityList = Settings.GetDefinitions().Keys.ToList();
            entityList.RemoveAll(x => CubeGrid.Inventories.Any(y => y.EntityId == x));
            foreach (var item in entityList)
            {
                Settings.GetDefinitions().Remove(item);
                needComuniteChange = true;
            }
            entityList.Clear();
            entityList.AddRange(Settings.GetIgnoreCargos());
            entityList.RemoveAll(x => CubeGrid.Inventories.Any(y => y.EntityId == x));
            foreach (var item in entityList)
            {
                Settings.GetIgnoreCargos().Remove(item);
                needComuniteChange = true;
            }
            entityList.Clear();
            entityList.AddRange(Settings.GetIgnoreFunctionalBlocks());
            entityList.RemoveAll(x => CubeGrid.Inventories.Any(y => y.EntityId == x));
            foreach (var item in entityList)
            {
                Settings.GetIgnoreFunctionalBlocks().Remove(item);
                needComuniteChange = true;
            }
            entityList.Clear();
            entityList.AddRange(Settings.GetIgnoreConnectors());
            entityList.RemoveAll(x => CubeGrid.Inventories.Any(y => y.EntityId == x));
            foreach (var item in entityList)
            {
                Settings.GetIgnoreConnectors().Remove(item);
                needComuniteChange = true;
            }
            if (needComuniteChange)
            {
                SendToClient();
            }
        }

        private List<long> scanedGrids = new List<long>();

        protected IMySlimBlock GetAIIgnoreMapBlock(IMyCubeGrid target)
        {
            var validSubTypes = new string[] { "AIIgnoreMap", "AIIgnoreMapSmall", "AIIgnoreMapReskin", "AIIgnoreMapReskinSmall" };
            foreach (var item in validSubTypes)
            {
                var block = target.GetBlocks(new MyDefinitionId(typeof(MyObjectBuilder_OreDetector), item))?.FirstOrDefault();
                if (block != null)
                    return block;
            }
            return null;
        }

        private void DoPullFromSubGridList(List<MyCubeGrid> subgrids, ref List<IMyReactor> reactors, ref List<IMyGasGenerator> gasGenerators,
            ref List<IMyGasTank> gasTanks, ref List<IMyGasGenerator> composters, ref List<IMyGasGenerator> fishTraps, ref List<IMyGasGenerator> refrigerators)
        {
            if (Settings.GetPullSubGrids() && subgrids != null && subgrids.Any())
            {
                foreach (var grid in subgrids)
                {
                    if (scanedGrids.Contains(grid.EntityId))
                        continue;
                    scanedGrids.Add(grid.EntityId);

                    var ignoreMap = GetAIIgnoreMapBlock(grid);
                    var aiIgnoreBlock = ignoreMap?.FatBlock?.GameLogic?.GetAs<AIIgnoreMapBlock>();
                    IEnumerable<long> ignoreList = new List<long>(); 
                    if (aiIgnoreBlock != null && aiIgnoreBlock.IsWorking)
                    {
                        ignoreList = aiIgnoreBlock?.Settings.GetIgnoreBlocks();
                    }

                    DoCheckInventoryList(DoApplyBasicFilter(grid.Inventories, ignoreList).ToArray(), ref reactors, ref gasGenerators, ref gasTanks, ref composters, ref fishTraps, ref refrigerators);
                    if (Settings.GetPullFromConnectedGrids())
                    {
                        var connectedGrids = GetConnectedGrids(grid);
                        DoPullFromConnectedGridList(connectedGrids, ref reactors, ref gasGenerators, ref gasTanks, ref composters, ref fishTraps, ref refrigerators);
                    }
                }
            }
        }

        private void DoPullFromConnectedGridList(Dictionary<IMyShipConnector, ShipConnected> connectedGrids, ref List<IMyReactor> reactors, ref List<IMyGasGenerator> gasGenerators,
            ref List<IMyGasTank> gasTanks, ref List<IMyGasGenerator> composters, ref List<IMyGasGenerator> fishTraps, ref List<IMyGasGenerator> refrigerators)
        {
            if (Settings.GetPullFromConnectedGrids() && connectedGrids != null && connectedGrids.Any())
            {
                foreach (var connector in connectedGrids.Keys)
                {
                    if (scanedGrids.Contains(connectedGrids[connector].Grid.EntityId))
                        continue;
                    scanedGrids.Add(connectedGrids[connector].Grid.EntityId);

                    var ignoreMap = GetAIIgnoreMapBlock(connector.CubeGrid);
                    var aiIgnoreBlock = ignoreMap?.FatBlock?.GameLogic?.GetAs<AIIgnoreMapBlock>();
                    IEnumerable<long> ignoreList = new List<long>();
                    if (aiIgnoreBlock != null && aiIgnoreBlock.IsWorking)
                    {
                        ignoreList = aiIgnoreBlock?.Settings.GetIgnoreBlocks();
                    }

                    if (!Settings.GetIgnoreConnectors().Contains(connector.EntityId) && !ignoreList.Contains(connector.EntityId))
                    {
                        DoCheckInventoryList(DoApplyBasicFilter(connectedGrids[connector].Grid.Inventories, ignoreList).ToArray(), ref reactors, ref gasGenerators, ref gasTanks, ref composters, ref fishTraps, ref refrigerators);
                        if (Settings.GetPullSubGrids())
                        {
                            var subGridsFromConnectedGrid = GetSubGrids(connectedGrids[connector].Grid).Values.ToList();
                            DoPullFromSubGridList(subGridsFromConnectedGrid, ref reactors, ref gasGenerators, ref gasTanks, ref composters, ref fishTraps, ref refrigerators);
                        }
                    }
                }
            }
        }

        protected override void DoExecuteCycle()
        {
            List<MyCubeGrid> subgrids;
            Dictionary<IMyShipConnector, ShipConnected > connectedGrids;
            var power = GetPowerConsumption(out subgrids, out connectedGrids);
            if (power != Settings.GetPowerConsumption())
            {
                Settings.SetPowerConsumption(power);
                SendPowerToClient();
                CurrentEntity.RefreshCustomInfo();
            }
            if (IsWorking)
            {
                CheckEntitiesExist();
                if (!IsWorking)
                    return;
                scanedGrids.Clear();
                List<IMyReactor> reactors = new List<IMyReactor>();
                List<IMyGasGenerator> gasGenerators = new List<IMyGasGenerator>();
                List<IMyGasTank> gasTanks = new List<IMyGasTank>();
                List<IMyGasGenerator> composters = new List<IMyGasGenerator>();
                List<IMyGasGenerator> fishTraps = new List<IMyGasGenerator>();
                List<IMyGasGenerator> refrigerators = new List<IMyGasGenerator>();
                DoCheckInventoryList(ValidInventories.ToArray(), ref reactors, ref gasGenerators, ref gasTanks, ref composters, ref fishTraps, ref refrigerators);
                DoPullFromSubGridList(subgrids, ref reactors, ref gasGenerators, ref gasTanks, ref composters, ref fishTraps, ref refrigerators);
                DoPullFromConnectedGridList(connectedGrids, ref reactors, ref gasGenerators, ref gasTanks, ref composters, ref fishTraps, ref refrigerators);
                DoFillReactors(reactors);
                DoFillGasGenerator(gasGenerators);
                DoFillFishTrap(fishTraps);
                DoFillRefrigerator(refrigerators);
                DoFillComposter(composters);
                DoCheckAnyCanGoInOtherInventory();
                DoCheckPullInventories();
                DoFillBottles(gasGenerators, gasTanks);
            }
        }

        private void TryToFullFromInventory(MyDefinitionId blockId, MyInventory inventoryBase, MyCubeBlock[] listaToCheck, IMyAssembler assembler, IMyReactor reactor, IMyGasGenerator gasGenerator)
        {
            var pullAll = true;
            var ignoreTypes = new List<MyObjectBuilderType>();
            var ignoreIds = new ConcurrentDictionary<MyDefinitionId, MyFixedPoint>();
            var maxForIds = new ConcurrentDictionary<MyDefinitionId, MyFixedPoint>();
            if (assembler != null)
            {
                pullAll = assembler.IsQueueEmpty;
                if (!pullAll)
                {
                    foreach (var queue in assembler.GetQueue())
                    {
                        var blueprint = queue.Blueprint as MyBlueprintDefinitionBase;
                        foreach (var item in blueprint.Prerequisites)
                        {
                            if (!ignoreIds.ContainsKey(item.Id))
                                ignoreIds[item.Id] = item.Amount * queue.Amount;
                            ignoreIds[item.Id] += item.Amount * queue.Amount;
                        }                        
                    }
                }
            }
            if (reactor != null)
            {
                pullAll = false;
                var reactorDef = MyDefinitionManager.Static.GetCubeBlockDefinition(reactor.BlockDefinition) as MyReactorDefinition;
                foreach (var item in reactorDef.FuelInfos)
                {
                    ignoreIds[item.FuelId] = (MyFixedPoint)int.MaxValue;
                }
            }
            if (gasGenerator != null || blockId.IsGasTank())
            {
                pullAll = false;
                if (blockId.IsGasGenerator() || blockId.IsGasTank())
                {
                    ignoreIds[ItensConstants.ICE_ID.DefinitionId] = (MyFixedPoint)int.MaxValue;
                    ignoreIds[ItensConstants.HYDROGENBOTTLE_ID.DefinitionId] = 1;
                    ignoreIds[ItensConstants.OXYGENBOTTLE_ID.DefinitionId] = 1;
                }
                else if (blockId.IsFishTrap())
                {
                    ignoreIds[ItensConstants.FISH_BAIT_SMALL_ID.DefinitionId] = (MyFixedPoint)int.MaxValue;
                    ignoreIds[ItensConstants.FISH_NOBLE_BAIT_ID.DefinitionId] = (MyFixedPoint)int.MaxValue;
                }
                else if (blockId.IsComposter())
                {
                    ignoreIds[ItensConstants.ORGANIC_ID.DefinitionId] = (MyFixedPoint)IDEAL_COMPOSTER_ORGANIC;
                }
                else if (blockId.IsRefrigerator())
                {
                    ignoreTypes.Add(typeof(MyObjectBuilder_ConsumableItem));
                }
            }
            var itemsToCheck = inventoryBase.GetItems().ToArray();
            for (int j = 0; j < itemsToCheck.Length; j++)
            {
                var itemid = itemsToCheck[j].Content.GetId();

                if (!pullAll)
                {
                    if (ignoreIds.ContainsKey(itemid))
                    {
                        if (ItensConstants.GAS_TYPES.Contains(itemid.TypeId))
                        {
                            var gasInfo = itemsToCheck[j].Content as MyObjectBuilder_GasContainerObject;
                            if (gasInfo.GasLevel < 1)
                            {
                                continue;
                            }
                        }
                        else
                        {
                            var invAmount = inventoryBase.GetItemAmount(itemid);
                            if (invAmount <= ignoreIds[itemid])
                            {
                                continue;
                            }
                            maxForIds[itemid] = invAmount - ignoreIds[itemid];
                        }
                    }
                    else if (ignoreTypes.Contains(itemid.TypeId))
                    {
                        continue;
                    }
                }

                var validTargets = Settings.GetDefinitions().Values.Where(x =>
                    (x.ValidIds.Contains(itemid) || x.ValidTypes.Contains(itemid.TypeId)) &&
                    (!x.IgnoreIds.Contains(itemid) && !x.IgnoreTypes.Contains(itemid.TypeId))
                );
                if (validTargets.Any())
                {
                    foreach (var validTarget in validTargets)
                    {
                        var targetBlock = ValidInventories.FirstOrDefault(x => x.EntityId == validTarget.EntityId);
                        if (targetBlock != null)
                        {
                            var targetInventory = targetBlock.GetInventory(0);
                            if ((inventoryBase as IMyInventory).CanTransferItemTo(targetInventory, itemid) && targetInventory.VolumeFillFactor < 1)
                            {
                                InvokeOnGameThread(() =>
                                {
                                    MyInventory.Transfer(inventoryBase, targetInventory, itemsToCheck[j].ItemId);
                                });
                                break;
                            }
                        }
                    }
                }
            }
        }

    }

}