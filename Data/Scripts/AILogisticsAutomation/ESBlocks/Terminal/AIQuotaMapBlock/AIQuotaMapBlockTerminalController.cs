using Sandbox.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Game;
using Sandbox.ModAPI.Interfaces.Terminal;
using System.Collections.Generic;
using System;
using VRage.Utils;
using Sandbox.Game.Entities;
using System.Linq;
using System.Collections.Concurrent;
using Sandbox.Definitions;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;

namespace AILogisticsAutomation
{
    public class AIQuotaMapBlockTerminalController : BaseTerminalController<AIQuotaMapBlock, IMyOreDetector>
    {

        public const float MIN_QUOTA_VALUE = 1;
        public const float MAX_QUOTA_VALUE = 1000000;
        public const float DEFAULT_QUOTA_VALUE = 100;

        protected List<MyDefinitionId> validIds = new List<MyDefinitionId>();
        protected List<MyObjectBuilderType> validTypes = new List<MyObjectBuilderType>();
        protected List<MyTerminalControlComboBoxItem> validIdsUI = new List<MyTerminalControlComboBoxItem>();
        protected List<MyTerminalControlComboBoxItem> validTypesUI = new List<MyTerminalControlComboBoxItem>();
        protected ConcurrentDictionary<MyObjectBuilderType, List<MyTerminalControlComboBoxItem>> validIdsByTypeUI = new ConcurrentDictionary<MyObjectBuilderType, List<MyTerminalControlComboBoxItem>>();

        protected long selectedFilterType = 0;
        protected long selectedFilterGroup = 0;
        protected int selectedFilterItemType = 0;
        protected int selectedFilterItemId = 0;
        protected long selectedFilterBlockType = 0;

        protected int selectedQuotaItemType = 0;
        protected int selectedQuotaItemId = 0;
        protected float selectedQuotaValue = DEFAULT_QUOTA_VALUE;

        protected override bool CanAddControls(IMyTerminalBlock block)
        {
            var validSubTypes = new string[] { "AIQuotaMap", "AIQuotaMapSmall", "AIQuotaMapReskin", "AIQuotaMapReskinSmall" };
            return block.BlockDefinition.TypeId == typeof(MyObjectBuilder_OreDetector) && validSubTypes.Contains(block.BlockDefinition.SubtypeId);
        }

        protected void LoadItensIds()
        {
            // Load base itens Ids
            DoLoadPhysicalItemIds();
            // Load others configs
            var ignoredTypes = new MyObjectBuilderType[] { typeof(MyObjectBuilder_TreeObject), typeof(MyObjectBuilder_Package) };
            validIds.Clear();
            validTypes.Clear();
            validIdsUI.Clear();
            validTypesUI.Clear();
            validIdsByTypeUI.Clear();
            var list = MyDefinitionManager.Static.GetPhysicalItemDefinitions().Where(x => !ignoredTypes.Contains(x.Id.TypeId)).OrderBy(x => x.DisplayNameText).ToArray();
            int c = 0;
            for (int i = 0; i < list.Length; i++)
            {
                var item = list[i];
                validIds.Add(item.Id);
                var newItem = new MyTerminalControlComboBoxItem() { Value = MyStringId.GetOrCompute(item.DisplayNameText), Key = i };
                validIdsUI.Add(newItem);
                if (!validIdsByTypeUI.ContainsKey(item.Id.TypeId))
                    validIdsByTypeUI[item.Id.TypeId] = new List<MyTerminalControlComboBoxItem>();
                validIdsByTypeUI[item.Id.TypeId].Add(newItem);
                if (!validTypes.Contains(item.Id.TypeId))
                {
                    validTypes.Add(item.Id.TypeId);
                    validTypesUI.Add(new MyTerminalControlComboBoxItem() { Value = MyStringId.GetOrCompute(item.Id.TypeId.ToString().Replace(MyObjectBuilderType.LEGACY_TYPE_PREFIX, "")), Key = c });
                    c++;
                }
            }
        }

        protected override void DoInitializeControls()
        {

            LoadItensIds();

            Func<IMyTerminalBlock, bool> isWorking = (block) =>
            {
                var system = GetSystem(block);
                return system != null && system.IsPowered;
            };

            Func<IMyTerminalBlock, bool> isWorkingAndEnabled = (block) =>
            {
                var system = GetSystem(block);
                return system != null && isWorking.Invoke(block) && system.Settings.GetEnabled();
            };

            Func<IMyTerminalBlock, bool> isWorkingAndQuotaCargoSelected = (block) =>
            {
                var system = GetSystem(block);
                if (system != null)
                {
                    var targetGrid = system.CurrentEntity.CubeGrid as MyCubeGrid;
                    var query = targetGrid.Inventories.Where(x => x.EntityId == system.Settings.SelectedQuotaEntityId);
                    return query.Any() && isWorkingAndEnabled.Invoke(block);
                }
                return false;
            };

            Func<IMyTerminalBlock, bool> isWorkingAndQuotaCargoSelectedIsAdded = (block) =>
            {
                var system = GetSystem(block);
                if (system != null)
                {
                    var targetGrid = system.CurrentEntity.CubeGrid as MyCubeGrid;
                    var exits = targetGrid.Inventories.Any(x => x.EntityId == system.Settings.SelectedQuotaEntityId);
                    var added = system.Settings.GetQuotas().ContainsKey(system.Settings.SelectedQuotaEntityId);
                    return exits && added && isWorkingAndEnabled.Invoke(block);
                }
                return false;
            };

            if (!MyAPIGateway.Session.IsServer)
            {

                CreateTerminalLabel("AIMIClientConfig", "Client Configuration");

                /* Button Add Ignored */
                CreateTerminalButton(
                    "RequestConfigInfo", 
                    "Request Configuration", 
                    isWorking,
                    (block) =>
                    {
                        var system = GetSystem(block);
                        if (system != null)
                        {
                            system.RequestSettings();
                        }
                    },
                    tooltip: "Server and client sometimes get out of sync. Click this button to resync to server (Need to reload terminal to take effect)."
                );

            }

            CreateTerminalLabel("AIMIStartConfig", "AI Configuration");

            var checkboxEnabled = CreateOnOffSwitch(
                "CheckboxEnabled",
                "Enabled",
                isWorking,
                (block) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                    {
                        return system.Settings.GetEnabled();
                    }
                    return false;
                },
                (block, value) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                    {
                        system.Settings.SetEnabled(value);
                        system.SendToServer("Enabled", "SET", value.ToString());
                        UpdateVisual(block);
                    }
                },
                tooltip: "Set if the block will work or not.",
                supMultiple: true
            );
            CreateOnOffSwitchAction("AIEnabled", checkboxEnabled);

            CreateTerminalLabel("AIMIStartConfig", "AI Configuration");

            CreateTerminalSeparator("QuotaOptionsSeparator");

            CreateTerminalLabel("QuotaOptionsLable", "Quota Options");

            CreateListbox(
                "ListCargoContainersForQuota",
                "Quota Cargo Container List",
                isWorkingAndEnabled,
                (block, list, selectedList) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                    {
                        var targetGrid = system.CurrentEntity.CubeGrid as MyCubeGrid;
                        var targetFilter = new MyObjectBuilderType[] { typeof(MyObjectBuilder_CargoContainer), typeof(MyObjectBuilder_Cockpit), typeof(MyObjectBuilder_CryoChamber) };
                        foreach (var inventory in targetGrid.Inventories.Where(x => targetFilter.Contains(x.BlockDefinition.Id.TypeId) && !x.BlockDefinition.Id.IsCage()))
                        {
                            var ignored = false;
                            var ignoredBlock = system.GetAIIgnoreMap();
                            if (ignoredBlock != null)
                            {
                                ignored = ignoredBlock.Settings.GetIgnoreBlocks().Contains(inventory.EntityId);
                            }

                            if (ignored)
                                continue;

                            var added = system.Settings.GetQuotas().ContainsKey(inventory.EntityId);

                            var name = string.Format("[{0}] {2} - ({1})", added ? "X" : " ", inventory.BlockDefinition.DisplayNameText, inventory.DisplayNameText);
                            var item = new MyTerminalControlListBoxItem(MyStringId.GetOrCompute(name), MyStringId.GetOrCompute(name), inventory.EntityId);

                            list.Add(item);

                            if (system.Settings.SelectedQuotaEntityId == inventory.EntityId)
                            {
                                selectedList.Add(item);
                                system.Settings.SelectedQuotaEntityId = inventory.EntityId;
                            }
                        }
                    }
                },
                (block, selectedList) =>
                {
                    if (selectedList.Count == 0)
                        return;

                    var system = GetSystem(block);
                    if (system != null)
                    {
                        var targetGrid = system.CurrentEntity.CubeGrid as MyCubeGrid;
                        var query = targetGrid.Inventories.Where(x => x.EntityId == (long)selectedList[0].UserData);
                        if (query.Any())
                        {
                            system.Settings.SelectedQuotaEntityId = query.FirstOrDefault().EntityId;
                            UpdateVisual(block);
                        }
                    }
                },
                tooltip: "Select a cargo container to set quota settings."
            );

            CreateCheckbox(
                "CheckboxAddContainerQuota",
                "Added selected cargo to quota list",
                isWorkingAndQuotaCargoSelected,
                (block) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                    {
                        var targetGrid = system.CurrentEntity.CubeGrid as MyCubeGrid;
                        var exists = targetGrid.Inventories.Any(x => x.EntityId == system.Settings.SelectedQuotaEntityId);
                        if (exists)
                        {
                            return system.Settings.GetQuotas().ContainsKey(system.Settings.SelectedQuotaEntityId);
                        }
                    }
                    return false;
                },
                (block, value) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                    {
                        var targetGrid = system.CurrentEntity.CubeGrid as MyCubeGrid;
                        var exists = targetGrid.Inventories.Any(x => x.EntityId == system.Settings.SelectedQuotaEntityId);
                        if (exists)
                        {
                            var added = system.Settings.GetQuotas().ContainsKey(system.Settings.SelectedQuotaEntityId);
                            if (value)
                            {
                                if (!added)
                                {
                                    system.Settings.GetQuotas()[system.Settings.SelectedQuotaEntityId] = new AIQuotaMapQuotaDefinition()
                                    {
                                        EntityId = system.Settings.SelectedQuotaEntityId
                                    };
                                    system.SendToServer("Quotas", "ADD", system.Settings.SelectedQuotaEntityId.ToString());
                                    UpdateVisual(block);
                                }
                            }
                            else
                            {
                                if (added)
                                {
                                    var dataToRemove = system.Settings.GetQuotas().ContainsKey(system.Settings.SelectedQuotaEntityId);
                                    if (dataToRemove)
                                    {
                                        system.Settings.GetQuotas().Remove(system.Settings.SelectedQuotaEntityId);
                                        system.SendToServer("Quotas", "DEL", system.Settings.SelectedQuotaEntityId.ToString());
                                    }
                                    UpdateVisual(block);
                                }
                            }
                        }
                    }
                },
                tooltip: "Cargos added to list will be used as quota stock."
            );

            CreateTerminalSeparator("CargoQuotaOptionsSeparator");

            CreateTerminalLabel("CargoOptionsSeparatorQuotaLable", "Selected Item to Quota");

            CreateCombobox(
                "QuotaItemType",
                "Quota Item Type",
                isWorkingAndQuotaCargoSelectedIsAdded,
                (block) =>
                {
                    var system = GetSystem(block);
                    if (system == null) return 0;
                    else return selectedQuotaItemType;
                },
                (block, value) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                    {
                        selectedQuotaItemType = (int)value;
                        if (PhysicalItemTypes.Values.Any(x => x.Index == selectedQuotaItemType))
                        {
                            var typeToUse = PhysicalItemTypes.Values.FirstOrDefault(x => x.Index == selectedQuotaItemType);
                            selectedQuotaItemId = typeToUse.Items.Min(x => x.Value.Index);
                        }
                        else
                            selectedQuotaItemId = 0;
                        UpdateVisual(block);
                    }
                },
                (list) =>
                {
                    list.AddRange(PhysicalItemTypes.Values.OrderBy(x => x.Index).Select(x => x.ComboBoxItem));
                },
                tooltip: "Select a quota item Type."
            );

            CreateCombobox(
                "QuotaItemId",
                "Quota Item Id",
                (block) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                        return isWorkingAndQuotaCargoSelectedIsAdded.Invoke(block) && selectedQuotaItemType >= 0;
                    return false;
                },
                (block) =>
                {
                    var system = GetSystem(block);
                    if (system == null) return 0;
                    else return selectedQuotaItemId;
                },
                (block, value) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                    {
                        selectedQuotaItemId = (int)value;
                    }
                },
                (list) =>
                {
                    if (PhysicalItemTypes.Values.Any(x => x.Index == selectedQuotaItemType))
                    {
                        var typeToUse = PhysicalItemTypes.Values.FirstOrDefault(x => x.Index == selectedQuotaItemType);
                        list.AddRange(typeToUse.Items.Values.OrderBy(x => x.Index).Select(x => x.ComboBoxItem));
                    }
                },
                tooltip: "Select a quota item Id."
            );

            CreateSlider(
                "SliderQuotaValue",
                "Quota Amount",
                isWorkingAndQuotaCargoSelectedIsAdded,
                (block) =>
                {
                    var system = GetSystem(block);
                    return system != null ? selectedQuotaValue : MIN_QUOTA_VALUE;
                },
                (block, value) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                    {
                        selectedQuotaValue = value;
                    }
                },
                (block, val) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                    {
                        val.Append((int)selectedQuotaValue);
                    }
                },
                new VRageMath.Vector2(MIN_QUOTA_VALUE, MAX_QUOTA_VALUE),
                tooltip: "Set a quota amount value."
            );

            /* Button Add Trigger */
            CreateTerminalButton(
                "AddQuotaItem",
                "Add Quota",
                (block) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                        return isWorkingAndQuotaCargoSelectedIsAdded.Invoke(block) && selectedQuotaItemType >= 0;
                    return false;
                },
                (block) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                    {
                        if (system.Settings.GetQuotas().ContainsKey(system.Settings.SelectedQuotaEntityId))
                        {
                            var typeToUse = PhysicalItemTypes.Values.FirstOrDefault(x => x.Index == selectedQuotaItemType);
                            if (typeToUse != null)
                            {
                                var itemToUse = typeToUse.Items.Values.FirstOrDefault(x => x.Index == selectedQuotaItemId);
                                if (itemToUse != null)
                                {
                                    var targetQuota = system.Settings.GetQuotas()[system.Settings.SelectedQuotaEntityId];
                                    var entry = new AIQuotaMapQuotaEntry()
                                    {
                                        Id = itemToUse.Id,
                                        Value = (int)selectedQuotaValue,
                                        Index = targetQuota.Entries.Any() ? targetQuota.Entries.Max(x => x.Index) + 1 : 1
                                    };
                                    targetQuota.Entries.Add(entry);
                                    var data = $"{entry.Id};{entry.Value};{entry.Index}";
                                    system.SendToServer("Entries", "ADD", data, system.Settings.SelectedQuotaEntityId.ToString());
                                    UpdateVisual(block);
                                }
                            }
                        }
                    }
                }
            );

            CreateListbox(
                "SelectedQuotaList",
                "Selected Quota Items",
                isWorkingAndQuotaCargoSelectedIsAdded,
                (block, list, selectedList) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                    {
                        if (system.Settings.GetQuotas().ContainsKey(system.Settings.SelectedQuotaEntityId))
                        {
                            var targetQuota = system.Settings.GetQuotas()[system.Settings.SelectedQuotaEntityId];
                            foreach (var condition in targetQuota.Entries)
                            {
                                if (PhysicalItemIds.ContainsKey(condition.Id))
                                {
                                    var itemToUse = PhysicalItemIds[condition.Id];
                                    var desc = $"{itemToUse.DisplayText} [{condition.Value}]";
                                    var item = new MyTerminalControlListBoxItem(MyStringId.GetOrCompute(desc), MyStringId.GetOrCompute(desc), (object)condition.Index);
                                    list.Add(item);
                                    if (condition.Index == system.Settings.SelectedQuotaEntryIndex)
                                        selectedList.Add(item);
                                }
                            }
                        }
                    }
                },
                (block, selectedList) =>
                {
                    if (selectedList.Count == 0)
                        return;

                    var system = GetSystem(block);
                    if (system != null)
                    {
                        system.Settings.SelectedQuotaEntryIndex = (int)selectedList[0].UserData;
                        UpdateVisual(block);
                    }
                },
                tooltip: "List of the items of selected quota."
            );

            CreateTerminalButton(
                "DelQuotaItem",
                "Remove Selected Item",
                (block) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                    {
                        if (isWorkingAndQuotaCargoSelectedIsAdded.Invoke(block))
                        {
                            var targetTrigger = system.Settings.GetQuotas()[system.Settings.SelectedQuotaEntityId];
                            return targetTrigger.Entries.Any(x => x.Index == system.Settings.SelectedQuotaEntryIndex);
                        }
                    }
                    return false;
                },
                (block) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                    {
                        if (system.Settings.GetQuotas().ContainsKey(system.Settings.SelectedQuotaEntityId))
                        {
                            var targetQuota = system.Settings.GetQuotas()[system.Settings.SelectedQuotaEntityId];
                            var entry = targetQuota.Entries.FirstOrDefault(x => x.Index == system.Settings.SelectedQuotaEntryIndex);
                            if (entry != null)
                            {
                                targetQuota.Entries.Remove(entry);
                                system.SendToServer("Entries", "DEL", system.Settings.SelectedQuotaEntryIndex.ToString(), system.Settings.SelectedQuotaEntityId.ToString());
                                UpdateVisual(block);
                            }
                        }
                    }
                }
            );

        }

        protected override string GetActionPrefix()
        {
            return "AIQuotaMap";
        }

        private readonly string[] idsToRemove = new string[] { "Range", "BroadcastUsingAntennas", "CustomData" };
        protected override string[] GetIdsToRemove()
        {
            return idsToRemove;
        }

    }

}