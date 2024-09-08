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
using Sandbox.Game.Entities.Cube;
using VRage.Game.Entity;
using Sandbox.ModAPI.Interfaces;

namespace AILogisticsAutomation
{

    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_OreDetector), false, "AIRefineryController", "AIRefineryControllerReskin")]
    public class AIRefineryControllerBlock : BaseAIBlock<IMyOreDetector, AIRefineryControllerSettings, AIRefineryControllerSettingsData>
    {

        private const float IDEAL_FIRST_AMOUNT = 1000;
        private const float IDEAL_OTHERS_AMOUNT = 100;

        protected override bool GetHadWorkToDo()
        {
            return Settings.DefaultOres.Count() > 0 || Settings.GetDefinitions().Any(x => x.Value.Ores.Count() > 0) || Settings.GetTriggers().Any(x => x.Value.Any());
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
            var totalFilters = Settings.DefaultOres.Count() + Settings.GetDefinitions().Values.Sum(x => x.Ores.Count()) + Settings.GetTriggers().Values.Sum(x => x.Count() + x.Conditions.Count);
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

        protected bool _rangeReset = false;
        protected int _tryResetCount = 0;
        protected override void DoExecuteCycle()
        {
            if (!_rangeReset && _tryResetCount < 10)
                _rangeReset = CurrentEntity.DoResetRange();
            if (!_rangeReset)
                _tryResetCount++;
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
            float targetAmount, AIInventoryManagerBlock inventoryManager)
        {
            var oreAmount = (float)inventory.GetItemAmount(oreId);
            if (oreAmount < targetAmount && oreMap.ContainsKey(oreId))
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
                        {
                            oreMap[oreId].CargoAmount.Remove(targetCargo);
                            var targetItem = targetCargoInventory.FindItem(oreId);
                            if (targetItem.HasValue)
                            {
                                InvokeOnGameThread(() =>
                                {
                                    MyInventory.Transfer(targetCargoInventory, inventory, targetItem.Value.ItemId);
                                });
                            }
                        }
                        else
                        {
                            InvokeOnGameThread(() =>
                            {
                                MyInventory.Transfer(targetCargoInventory, inventory, oreId, MyItemFlags.None, (MyFixedPoint)transferAmoun);
                            });
                        }
                    }
                    else
                    {
                        oreMap[oreId].CargoAmount.Remove(targetCargo);
                    }
                }
                return false;
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

                // Conditional Meta
                var triggerPriority = new HashSet<string>();
                if (Settings.GetTriggers().Any())
                {
                    foreach (var triggerId in Settings.GetTriggers().Keys)
                    {
                        var targetTrigger = Settings.GetTriggers()[triggerId];
                        if (!targetTrigger.Conditions.Any())
                            continue;
                        var okToRun = false;
                        var conds = targetTrigger.Conditions.OrderBy(x => x.Index).ToArray();
                        for (int i = 0; i < conds.Length; i++)
                        {
                            var targetAmount = (float)inventoryManager.GetItemAmount(conds[i].Id);
                            var valueCheck = false;
                            switch (conds[i].OperationType)
                            {
                                case 0: /* GREATER */
                                    valueCheck = targetAmount > conds[i].Value;
                                    break;
                                case 1: /* LESS */
                                    valueCheck = targetAmount < conds[i].Value;
                                    break;
                            }
                            switch (conds[i].QueryType)
                            {
                                case 0: /* AND */
                                    okToRun = (i == 0 || okToRun) && valueCheck;
                                    break;
                                case 1: /* OR */
                                    okToRun = okToRun || valueCheck;
                                    break;
                            }
                        }
                        if (okToRun)
                        {
                            foreach (var item in targetTrigger.GetAll())
                            {
                                triggerPriority.Add(item);
                            }
                        }
                    }
                }
                var defaultPriority = Settings.DefaultOres.GetAll();
                var allStoredTypes = oreMap.Keys.Select(x => x.SubtypeName).ToArray();

                for (int i = 0; i < listaToCheck.Length; i++)
                {

                    if (!listaToCheck[i].BlockDefinition.Id.IsRefinery())
                        continue;

                    var inventory = listaToCheck[i].GetInventory(0);
                    var inInventoryPriority = inventory.GetItems().Select(x => x.Content.SubtypeName).ToArray();

                    var refineryFilter = Settings.GetDefinitions().ContainsKey(listaToCheck[i].EntityId) ? Settings.GetDefinitions()[listaToCheck[i].EntityId].Ores.GetAll() : new string[] { };
                    var finalFilter = new HashSet<string>();
                    foreach (var item in triggerPriority)
                    {
                        finalFilter.Add(item);
                    }
                    foreach (var item in refineryFilter.Where(x => !finalFilter.Contains(x)))
                    {
                        finalFilter.Add(item);
                    }
                    foreach (var item in defaultPriority.Where(x => !finalFilter.Contains(x)))
                    {
                        finalFilter.Add(item);
                    }
                    foreach (var item in inInventoryPriority.Where(x => !finalFilter.Contains(x)))
                    {
                        finalFilter.Add(item);
                    }
                    foreach (var item in allStoredTypes.Where(x => !finalFilter.Contains(x)))
                    {
                        finalFilter.Add(item);
                    }

                    bool useConveyorSystem = true;

                    // Add ore to refinery
                    var sourceOres = finalFilter.Any() ?
                        finalFilter.Where(x =>
                            inventory.GetItemAmount(new MyDefinitionId(oreType, x)) > 0 ||
                            oreMap.ContainsKey(new MyDefinitionId(oreType, x))
                        ).ToArray() :
                        new string[] { };

                    if (sourceOres.Any() && oreMap.Any())
                    {
                        int c = 0;
                        foreach (var ore in sourceOres)
                        {
                            var oreId = new MyDefinitionId(oreType, ore);
                            var push = DoPushOre(
                                oreId,
                                oreMap,
                                inventory,
                                c == 0 ? IDEAL_FIRST_AMOUNT : IDEAL_OTHERS_AMOUNT,
                                inventoryManager
                            );
                            useConveyorSystem = useConveyorSystem && push;
                            if (inventory.GetItemAmount(oreId) > 0)
                                c++;
                        }
                        var others = oreMap.Keys.Where(x => !sourceOres.Contains(x.SubtypeName)).ToArray();
                        foreach (var ore in others)
                        {
                            var push = DoPushOre(
                                ore,
                                oreMap,
                                inventory,
                                IDEAL_OTHERS_AMOUNT,
                                inventoryManager
                            );
                            useConveyorSystem = useConveyorSystem && push;
                        }
                    }

                    // Sort
                    DoSort(inventory, finalFilter.ToList());
                    (listaToCheck[i] as IMyRefinery).UseConveyorSystem = useConveyorSystem;

                }
            }
        }

        private void DoSort(MyInventory targetInventory, List<string> priority)
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
                            dif = priority.IndexOf(item.Value.Type.SubtypeId).CompareTo(priority.IndexOf(itemBefore.Value.Type.SubtypeId)) * -1;
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