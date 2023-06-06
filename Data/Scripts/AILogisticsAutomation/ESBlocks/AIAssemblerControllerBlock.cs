using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Game;
using Sandbox.Common.ObjectBuilders;
using VRage.Game.ModAPI;
using System.Linq;
using System.Collections.Generic;
using Sandbox.Game.Entities;
using System.Collections.Concurrent;
using Sandbox.Definitions;
using System;

namespace AILogisticsAutomation
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_OreDetector), false, "AIAssemblerController", "AIAssemblerControllerReskin")]
    public class AIAssemblerControllerBlock : BaseAIBlock<IMyOreDetector, AIAssemblerControllerSettings, AIAssemblerControllerSettingsData>
    {

        protected override bool GetHadWorkToDo()
        {
            return Settings.DefaultStock.ValidIds.Any() || Settings.DefaultStock.ValidTypes.Any();
        }

        protected override bool GetIsValidToWork()
        {
            return CountAIAssemblerController(Grid) == 1;
        }

        protected override void OnInit(MyObjectBuilder_EntityBase objectBuilder)
        {
            Settings = new AIAssemblerControllerSettings();
            base.OnInit(objectBuilder);
            NeedsUpdate |= MyEntityUpdateEnum.EACH_100TH_FRAME;
        }

        protected int CountAIAssemblerController(IMyCubeGrid grid)
        {
            var count = 0;
            var validSubTypes = new string[] { "AIAssemblerController", "AIAssemblerControllerReskin" };
            foreach (var item in validSubTypes)
            {
                count += grid?.CountBlocks(new MyDefinitionId(typeof(MyObjectBuilder_OreDetector), item)) ?? 0;
            }
            return count;
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
                x.BlockDefinition.Id.IsAssembler() &&
                !customIgnoreList.Contains(x.EntityId) &&
                !Settings.GetIgnoreAssembler().Contains(x.EntityId)
            );
        }

        private float CalcPowerFromBlocks(float power, IEnumerable<MyCubeBlock> blocks)
        {
            var totalInventories = blocks.Count();

            // Get Default power
            power += AILogisticsAutomationSettings.Instance.EnergyCost.DefaultBlockCost * totalInventories;

            // Get filter power
            var totalFilters = Settings.DefaultPriority.Count() + Settings.GetTriggers().Values.Sum(x => x.Actions.Count + x.Conditions.Count);
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
            var entityList = Settings.GetIgnoreAssembler().ToList();
            entityList.RemoveAll(x => CubeGrid.Inventories.Any(y => y.EntityId == x));
            foreach (var item in entityList)
            {
                Settings.GetIgnoreAssembler().Remove(item);
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
                    DoCheckAssemblerList(ValidInventories.ToArray(), inventoryManager);
                }
            }
        }

        private ConcurrentDictionary<MyDefinitionId, int> assembleMeta = new ConcurrentDictionary<MyDefinitionId, int>();
        private ConcurrentDictionary<MyDefinitionId, int> produceMeta = new ConcurrentDictionary<MyDefinitionId, int>();
        private int runCount = 0;

        private void DoBuildTargetMeta(AIInventoryManagerBlock inventoryManager)
        {
            assembleMeta.Clear();
            // Base Meta
            foreach (var targetType in Settings.DefaultStock.ValidTypes.Keys)
            {
                if (Settings.DefaultStock.IgnoreTypes.Contains(targetType))
                    continue;
                if (AIAssemblerControllerBlockTerminal.Controller.PhysicalItemTypes.ContainsKey(targetType))
                {
                    var targetTypeInfo = AIAssemblerControllerBlockTerminal.Controller.PhysicalItemTypes[targetType];
                    foreach (var targetItem in targetTypeInfo.Items.Keys.Where(x => AIAssemblerControllerBlockTerminal.Controller.ValidIds.Contains(x)))
                    {
                        if (Settings.DefaultStock.IgnoreIds.Contains(targetItem))
                            continue;
                        assembleMeta[targetItem] = Settings.DefaultStock.ValidTypes[targetType];
                    }
                }
            }
            foreach (var targetItem in Settings.DefaultStock.ValidIds.Keys)
            {
                if (Settings.DefaultStock.IgnoreTypes.Contains(targetItem.TypeId))
                    continue;
                if (Settings.DefaultStock.IgnoreIds.Contains(targetItem))
                    continue;
                assembleMeta[targetItem] = Settings.DefaultStock.ValidIds[targetItem];
            }
            // Conditional Meta
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
                        foreach (var action in targetTrigger.Actions)
                        {
                            assembleMeta[action.Id] = (int)action.Value;
                        }
                    }
                }
            }
        }

        private void DoBuildProduceList(AIInventoryManagerBlock inventoryManager)
        {
            produceMeta.Clear();
            var keys = assembleMeta.Keys.Select(x => new { Key = x, Index = Settings.DefaultPriority.GetIndex(x) }).OrderBy(x => x.Index >= 0 ? x.Index : int.MaxValue).Select(x => x.Key).ToArray();
            foreach (var key in keys)
            {
                var targetAmount = (float)inventoryManager.GetItemAmount(key);
                if (targetAmount < assembleMeta[key])
                {
                    produceMeta[key] = assembleMeta[key] - (int)targetAmount;
                }
            }
        }

        private const int CICLE_QUEUE_AMOUNT = 10;

        private void DoCheckProduceList(MyCubeBlock[] listaToCheck, AIInventoryManagerBlock inventoryManager)
        {
            foreach (var key in produceMeta.Keys)
            {
                var targetAssemblersType = AIAssemblerControllerBlockTerminal.Controller.Assemblers.Where(x => x.Value.ValidIds.ContainsKey(key)).Select(x=>x.Key).ToArray();
                var targetAssemblers = listaToCheck.Where(x => x.BlockDefinition != null && targetAssemblersType.Contains(x.BlockDefinition.Id)).ToArray();
                foreach (var block in targetAssemblers)
                {
                    var assembler = block as IMyAssembler;
                    if (assembler == null)
                        continue;
                    var assemblerDef = AIAssemblerControllerBlockTerminal.Controller.Assemblers[block.BlockDefinition.Id];
                    foreach (var blueprint in assemblerDef.ItemBlueprintToUse[key].OrderBy(x => x.DisplayNameText.Contains("Broken") ? 0 : 1))
                    {
                        if (!blueprint.Results.Any(x => x.Id == key))
                            continue; /* there are no result of the type */
                        if (blueprint.Prerequisites.Any(x => inventoryManager.GetItemAmount(x.Id) < x.Amount * CICLE_QUEUE_AMOUNT))
                            continue; /* there are no resources to assemble */
                        if (assembler.GetQueue().Any(x => x.Blueprint.Id == blueprint.Id))
                        {
                            var qItem = assembler.GetQueue().FirstOrDefault(x => x.Blueprint.Id == blueprint.Id);
                            if (qItem.Amount < CICLE_QUEUE_AMOUNT)
                            {
                                var index = assembler.GetQueue().IndexOf(qItem);
                                InvokeOnGameThread(() => {
                                    try
                                    {
                                        assembler.InsertQueueItem(index, blueprint, CICLE_QUEUE_AMOUNT - qItem.Amount);
                                    }
                                    catch (Exception ex)
                                    {
                                        AILogisticsAutomationLogging.Instance.LogWarning(GetType(), $"InsertQueueItem: Error when insert [{blueprint.Id}] to [{assembler.DisplayName}] at [{index}]");
                                        AILogisticsAutomationLogging.Instance.LogError(GetType(), ex);
                                    }
                                });
                            }
                        }
                        else
                        {
                            InvokeOnGameThread(() => {
                                try
                                {
                                    assembler.AddQueueItem(blueprint, CICLE_QUEUE_AMOUNT);
                                }
                                catch (Exception ex)
                                {
                                    AILogisticsAutomationLogging.Instance.LogWarning(GetType(), $"AddQueueItem: Error when add [{blueprint.Id}] to [{assembler.DisplayName}]");
                                    AILogisticsAutomationLogging.Instance.LogError(GetType(), ex);
                                }
                            });
                        }
                        break;
                    }
                }
            }
        }

        private void DoClearQueue(MyCubeBlock[] listaToCheck, AIInventoryManagerBlock inventoryManager)
        {
            foreach (var block in listaToCheck)
            {
                var assembler = block as IMyAssembler;
                if (assembler == null)
                    continue;
                var queueItens = assembler.GetQueue().ToArray();
                for (int i = queueItens.Length - 1; i >= 0; i--)
                {
                    var queue = queueItens[i];
                    var bluePrint = queue.Blueprint as MyBlueprintDefinitionBase;
                    if (!bluePrint.Results.Any(x=> produceMeta.ContainsKey(x.Id)))
                    {
                        InvokeOnGameThread(() => {
                            try
                            {
                                assembler.RemoveQueueItem(i, queue.Amount);
                            }
                            catch (Exception ex)
                            {
                                AILogisticsAutomationLogging.Instance.LogWarning(GetType(), $"RemoveQueueItem: Error when remove from [{assembler.DisplayName}] at [{i}]");
                                AILogisticsAutomationLogging.Instance.LogError(GetType(), ex);
                            }
                        });
                        continue;
                    }
                    if (bluePrint.Prerequisites.Any(x => inventoryManager.GetItemAmount(x.Id) < x.Amount))
                    {
                        InvokeOnGameThread(() => {
                            try
                            {
                                assembler.RemoveQueueItem(i, queue.Amount);
                            }
                            catch (Exception ex)
                            {
                                AILogisticsAutomationLogging.Instance.LogWarning(GetType(), $"RemoveQueueItem: Error when remove from [{assembler.DisplayName}] at [{i}]");
                                AILogisticsAutomationLogging.Instance.LogError(GetType(), ex);
                            }
                        });
                    }
                }
            }
        }

        private void DoCheckAssemblerList(MyCubeBlock[] listaToCheck, AIInventoryManagerBlock inventoryManager)
        {
            // Do Calc Meta
            if (runCount <= 0)
            {
                DoBuildTargetMeta(inventoryManager);
                runCount = 0;
            }
            // Do Check Meta
            if (runCount == 5)
            {
                DoBuildProduceList(inventoryManager);
            }
            // Do Clear Queue
            if (runCount == 10)
            {
                DoClearQueue(listaToCheck, inventoryManager);
            }
            runCount++;
            if (runCount > 10)
                runCount = 0;
            // Do Check Production
            DoCheckProduceList(listaToCheck, inventoryManager);
        }

    }

}