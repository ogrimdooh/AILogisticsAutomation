using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Game;
using Sandbox.Common.ObjectBuilders;
using VRage.Game.ModAPI;
using System.Linq;
using Sandbox.Game.Entities;
using System.Collections.Generic;
using System.Collections.Concurrent;
using VRage;
using Sandbox.Game;
using Sandbox.Definitions;

namespace AILogisticsAutomation
{

    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_OreDetector), false, "AIRefineryController", "AIRefineryControllerReskin")]
    public class AIRefineryControllerBlock : BaseAIBlock<IMyOreDetector, AIRefineryControllerSettings, AIRefineryControllerSettingsData>
    {

        protected override bool GetHadWorkToDo()
        {
            return Settings.DefaultOres.Count() > 0 || Settings.GetDefinitions().Any(x => x.Value.Ores.Count() > 0);
        }

        protected override bool GetIsValidToWork()
        {
            return CountAIRefineryController(Grid) == 1;
        }

        protected override void OnInit(MyObjectBuilder_EntityBase objectBuilder)
        {
            Settings = new AIRefineryControllerSettings();
            base.OnInit(objectBuilder);
            NeedsUpdate |= MyEntityUpdateEnum.EACH_100TH_FRAME;
        }

        protected int CountAIRefineryController(IMyCubeGrid grid)
        {
            var count = 0;
            var validSubTypes = new string[] { "AIRefineryController", "AIRefineryControllerReskin" };
            foreach (var item in validSubTypes)
            {
                count += grid?.CountBlocks(new MyDefinitionId(typeof(MyObjectBuilder_OreDetector), item)) ?? 0;
            }
            return count;
        }

        private IEnumerable<MyCubeBlock> DoApplyBasicFilter(HashSet<MyCubeBlock> inventories, bool ignoreFunctional = false)
        {
            return inventories.Where(x =>
                (
                    (x.IsFunctional && ((x as IMyFunctionalBlock)?.Enabled ?? true)) ||
                    ignoreFunctional
                ) &&
                !Settings.GetIgnoreRefinery().Contains(x.EntityId) &&
                x.BlockDefinition.Id.IsRefinery() &&
                (
                    Settings.DefaultOres.Any() ||
                    Settings.GetDefinitions().Any(y => y.Key == x.EntityId && y.Value.Ores.Any())
                )
            );
        }

        public IEnumerable<MyCubeBlock> ValidInventories
        {
            get
            {
                return DoApplyBasicFilter(CubeGrid.Inventories);
            }
        }

        public IEnumerable<MyCubeBlock> ValidInventoriesWithNoFunctional
        {
            get
            {
                return DoApplyBasicFilter(CubeGrid.Inventories, true);
            }
        }

        private float CalcPowerFromBlocks(float power, IEnumerable<MyCubeBlock> blocks)
        {
            var totalInventories = blocks.Count();

            // Get Default power
            power += AILogisticsAutomationSettings.Instance.EnergyCost.DefaultBlockCost * totalInventories;

            // Get single refinery power
            power += AILogisticsAutomationSettings.Instance.EnergyCost.DefaultPullCost * Settings.GetDefinitions().Count;

            // Get filter power
            var totalFilters = Settings.DefaultOres.Count() + Settings.GetDefinitions().Values.Sum(x => x.Ores.Count());
            power += AILogisticsAutomationSettings.Instance.EnergyCost.FilterCost * totalFilters;

            return power;
        }

        private float GetPowerConsumption()
        {
            if (!IsValidToWork)
                return 0;

            var power = CalcPowerFromBlocks(0, ValidInventories);

            return power;
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
            entityList.AddRange(Settings.GetIgnoreRefinery());
            entityList.RemoveAll(x => CubeGrid.Inventories.Any(y => y.EntityId == x));
            foreach (var item in entityList)
            {
                Settings.GetDefinitions().Remove(item);
                needComuniteChange = true;
            }
            if (needComuniteChange)
            {
                SendToClient();
            }
        }

        protected override void DoExecuteCycle()
        {
            var power = GetPowerConsumption();
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
                var inventoryManager = GetAIInventoryManager();
                if (inventoryManager != null && inventoryManager.Settings.GetPullFromRefinary())
                {
                    DoCheckRefineryList(ValidInventories.ToArray(), inventoryManager);
                }
            }
        }

        public class MyInventoryOreMap
        {

            public MyDefinitionId ItemId { get; set; }
            public float TotalAmount { get; set; }
            public ConcurrentDictionary<long, float> CargoAmount { get; set; } = new ConcurrentDictionary<long, float>();

        }

        private bool DoPushOre(MyDefinitionId oreId, ConcurrentDictionary<MyDefinitionId, MyInventoryOreMap> oreMap, MyInventory inventory, 
            float targetVolume, AIInventoryManagerBlock inventoryManager)
        {
            if (oreMap.ContainsKey(oreId))
            {
                var oreAmount = (float)inventory.GetItemAmount(oreId);
                var oreDef = MyDefinitionManager.Static.GetPhysicalItemDefinition(oreId);
                var targetAmount = targetVolume / oreDef.Volume;
                if (oreAmount < targetAmount)
                {
                    var oreToTransfer = targetAmount - oreAmount;
                    while (oreMap[oreId].TotalAmount > 0 && oreMap[oreId].CargoAmount.Any() && oreToTransfer > 0)
                    {
                        if (inventory.VolumeFillFactor >= 1)
                            break;
                        var targetCargo = oreMap[oreId].CargoAmount.Keys.FirstOrDefault();
                        var targetCargoCube = CubeGrid.Inventories.FirstOrDefault(x => x.EntityId == targetCargo);
                        var targetCargoInventory = targetCargoCube?.GetInventory(0);
                        if (targetCargoInventory == null)
                        {
                            oreMap[oreId].CargoAmount.Remove(targetCargo);
                            continue;
                        }
                        var transferAmoun = oreMap[oreId].CargoAmount[targetCargo] > oreToTransfer ? oreToTransfer : oreMap[oreId].CargoAmount[targetCargo];
                        if ((targetCargoInventory as IMyInventory).CanTransferItemTo(inventory, oreId) && targetCargoInventory.VolumeFillFactor < 1)
                        {
                            oreToTransfer -= transferAmoun;
                            oreMap[oreId].CargoAmount[targetCargo] -= transferAmoun;
                            oreMap[oreId].TotalAmount -= transferAmoun;
                            if (oreMap[oreId].CargoAmount[targetCargo] <= 0)
                                oreMap[oreId].CargoAmount.Remove(targetCargo);
                            InvokeOnGameThread(() =>
                            {
                                MyInventory.Transfer(targetCargoInventory, inventory, oreId, MyItemFlags.None, (MyFixedPoint)transferAmoun);
                            });
                        }
                        else
                        {
                            oreMap[oreId].CargoAmount.Remove(targetCargo);
                        }
                    }
                }
                else if (oreAmount > targetAmount)
                {
                    var amountToRemove = oreAmount - targetAmount;
                    var validTargets = inventoryManager.Settings.GetDefinitions().Values.Where(x =>
                        (x.ValidIds.Contains(oreId) || x.ValidTypes.Contains(oreId.TypeId)) &&
                        (!x.IgnoreIds.Contains(oreId) && !x.IgnoreTypes.Contains(oreId.TypeId))
                    );
                    if (validTargets.Any())
                    {
                        foreach (var validTarget in validTargets)
                        {
                            var targetBlockToSend = CubeGrid.Inventories.FirstOrDefault(x => x.EntityId == validTarget.EntityId);
                            if (targetBlockToSend != null)
                            {
                                var targetInventoryToSend = targetBlockToSend.GetInventory(0);
                                if ((inventory as IMyInventory).CanTransferItemTo(targetInventoryToSend, oreId) && targetInventoryToSend.VolumeFillFactor < 1)
                                {
                                    InvokeOnGameThread(() =>
                                    {
                                        MyInventory.Transfer(inventory, targetInventoryToSend, oreId, MyItemFlags.None, (MyFixedPoint)amountToRemove);
                                    });
                                }
                            }
                        }
                    }
                }
                return false;
            }
            return true;
        }

        private void DoCheckRefineryList(MyCubeBlock[] listaToCheck, AIInventoryManagerBlock inventoryManager)
        {
            var oreType = typeof(MyObjectBuilder_Ore);
            var pullCargos = inventoryManager.Settings.GetDefinitions().Where(x => x.Value.ValidIds.Any(y => y.TypeId == oreType) || x.Value.ValidTypes.Contains(oreType)).Select(x => x.Key).ToArray();

            if (pullCargos.Any())
            {
                var oreMap = new ConcurrentDictionary<MyDefinitionId, MyInventoryOreMap>();
                foreach (var cargos in pullCargos)
                {
                    var map = inventoryManager.GetMap(cargos);
                    if (map != null)
                    {
                        foreach (var item in map.GetItems().Where(x => x.TypeId == oreType))
                        {
                            var itemMap = map.GetItem(item);
                            if (!oreMap.ContainsKey(item))
                                oreMap[item] = new MyInventoryOreMap() { ItemId = item };
                            oreMap[item].TotalAmount += (float)itemMap.TotalAmount;
                            oreMap[item].CargoAmount[cargos] = (float)itemMap.TotalAmount;
                        }
                    }
                }

                for (int i = 0; i < listaToCheck.Length; i++)
                {

                    if (!listaToCheck[i].BlockDefinition.Id.IsRefinery())
                        continue;

                    var inventory = listaToCheck[i].GetInventory(0);

                    var oreFilter = Settings.GetDefinitions().ContainsKey(listaToCheck[i].EntityId) ? Settings.GetDefinitions()[listaToCheck[i].EntityId].Ores : Settings.DefaultOres;

                    bool useConveyorSystem = true;
                    if (oreFilter.Any())
                    {
                        // Add ore to refinery
                        var sourceOres = oreFilter.GetAll().Where(x => inventory.GetItemAmount(new MyDefinitionId(oreType, x)) > 0 || oreMap.ContainsKey(new MyDefinitionId(oreType, x))).ToArray();
                        if (sourceOres.Any() && oreMap.Any())
                        {
                            var maxVolume = (float)inventory.MaxVolume * 0.8f;
                            var maxOthersVolume = (float)inventory.MaxVolume * 0.15f;
                            var targetVolume = maxVolume / sourceOres.Count();
                            var othersVolume = oreMap.Count > sourceOres.Count() ? maxOthersVolume / (oreMap.Count - sourceOres.Count()) : 0;
                            foreach (var ore in sourceOres)
                            {
                                var oreId = new MyDefinitionId(oreType, ore);
                                var push = DoPushOre(
                                    oreId,
                                    oreMap,
                                    inventory,
                                    targetVolume,
                                    inventoryManager
                                );
                                useConveyorSystem = useConveyorSystem && push;
                            }
                            if (othersVolume > 0)
                            {
                                var others = oreMap.Keys.Where(x => !sourceOres.Contains(x.SubtypeName)).ToArray();
                                foreach (var ore in others)
                                {
                                    var push = DoPushOre(
                                        ore,
                                        oreMap,
                                        inventory,
                                        othersVolume,
                                        inventoryManager
                                    );
                                    useConveyorSystem = useConveyorSystem && push;
                                }
                            }
                        }
                        // Sort
                        DoSort(inventory, oreFilter);
                    }
                    (listaToCheck[i] as IMyRefinery).UseConveyorSystem = useConveyorSystem;

                }
            }
        }

        private void DoSort(MyInventory targetInventory, AIRefineryControllerPrioritySettings priority)
        {
            if (priority.Any())
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
                        int dif = 0;
                        if (priority.Contains(item.Value.Type.SubtypeId) && !priority.Contains(itemBefore.Value.Type.SubtypeId))
                        {
                            dif = 1;
                        }
                        else if (!priority.Contains(item.Value.Type.SubtypeId) && priority.Contains(itemBefore.Value.Type.SubtypeId))
                        {
                            dif = -1;
                        }
                        else if (priority.Contains(item.Value.Type.SubtypeId) && priority.Contains(itemBefore.Value.Type.SubtypeId))
                        {
                            dif = priority.GetIndex(item.Value.Type.SubtypeId).CompareTo(priority.GetIndex(itemBefore.Value.Type.SubtypeId)) * -1;
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

    }

}