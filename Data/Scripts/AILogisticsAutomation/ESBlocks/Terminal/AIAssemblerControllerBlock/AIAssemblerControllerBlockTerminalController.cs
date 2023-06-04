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
    public class AIAssemblerControllerBlockTerminalController : BaseTerminalController<AIAssemblerControllerBlock, IMyOreDetector>
    {

        public class AssemblerItemInfo : PhysicalItemInfo
        {

        }

        public class AssemblerTypeInfo : PhysicalItemTypeInfo
        {

        }

        public class AssemblerDefinitionInfo
        {

            public MyAssemblerDefinition Definition { get; set; }
            public ConcurrentDictionary<MyDefinitionId, AssemblerItemInfo> ValidIds { get; set; } = new ConcurrentDictionary<MyDefinitionId, AssemblerItemInfo>();            
            public ConcurrentDictionary<MyObjectBuilderType, AssemblerTypeInfo> ValidTypes { get; set; } = new ConcurrentDictionary<MyObjectBuilderType, AssemblerTypeInfo>();
            public ConcurrentDictionary<MyDefinitionId, List<MyBlueprintDefinitionBase>> ItemBlueprintToUse { get; set; } = new ConcurrentDictionary<MyDefinitionId, List<MyBlueprintDefinitionBase>>();
            
            public AssemblerDefinitionInfo(MyAssemblerDefinition definition)
            {
                Definition = definition;
                DoLoadAll();
            }

            private void DoLoadAll()
            {
                if (Definition != null)
                {
                    foreach (var blueprintClass in Definition.BlueprintClasses)
                    {
                        foreach (var blueprint in blueprintClass)
                        {
                            foreach (var result in blueprint.Results)
                            {
                                DoLoadItem(result.Id, blueprint);
                            }
                        }
                    }
                    DoSortAndSetIndex();
                    CreateComboBoxItens();
                }
            }

            private void DoSortAndSetIndex()
            {
                // Sort Types
                var ordenedTypesList = ValidTypes.OrderBy(x => x.Value.DisplayText).Select(x => x.Key).ToArray();
                for (int i = 0; i < ordenedTypesList.Length; i++)
                {
                    ValidTypes[ordenedTypesList[i]].Index = i;
                }
                // Sort Items
                var ordenedIdList = ValidIds.OrderBy(x => x.Value.DisplayText).Select(x => x.Key).ToArray();
                for (int i = 0; i < ordenedIdList.Length; i++)
                {
                    ValidIds[ordenedIdList[i]].Index = i;
                }
            }

            private void CreateComboBoxItens()
            {
                // Create Types Combo Box
                foreach (var id in ValidTypes.Keys)
                {
                    ValidTypes[id].ComboBoxItem = new MyTerminalControlComboBoxItem()
                    {
                        Key = ValidTypes[id].Index,
                        Value = MyStringId.GetOrCompute(ValidTypes[id].DisplayText)
                    };
                }
                // Create Itens Combo Box
                foreach (var id in ValidIds.Keys)
                {
                    ValidIds[id].ComboBoxItem = new MyTerminalControlComboBoxItem()
                    {
                        Key = ValidIds[id].Index,
                        Value = MyStringId.GetOrCompute(ValidIds[id].DisplayText)
                    };
                }
            }

            private void DoLoadItem(MyDefinitionId itemId, MyBlueprintDefinitionBase blueprint)
            {
                if (!ValidIds.ContainsKey(itemId))
                {
                    var itemDef = MyDefinitionManager.Static.GetPhysicalItemDefinition(itemId);
                    if (itemDef != null)
                    {
                        ValidIds[itemId] = new AssemblerItemInfo() 
                        { 
                            Id = itemId,
                            DisplayText = itemDef.DisplayNameText,
                            ItemDefinition = itemDef
                        };
                        ItemBlueprintToUse[itemId] = new List<MyBlueprintDefinitionBase>();
                        if (!ValidTypes.ContainsKey(itemId.TypeId))
                            ValidTypes[itemId.TypeId] = new AssemblerTypeInfo() 
                            { 
                                Type = itemId.TypeId,
                                DisplayText = itemId.TypeId.ToString().Replace(MyObjectBuilderType.LEGACY_TYPE_PREFIX, "")
                            };
                    }
                }
                if (ItemBlueprintToUse.ContainsKey(itemId))
                {
                    ItemBlueprintToUse[itemId].Add(blueprint);
                }
            }

        }

        public const float MIN_META = 10;
        public const float MAX_META = 100000;
        public const float DEFAULT_META = 1000;

        protected ConcurrentDictionary<MyDefinitionId, AssemblerDefinitionInfo> Assemblers { get; set; } = new ConcurrentDictionary<MyDefinitionId, AssemblerDefinitionInfo>();
        protected HashSet<MyDefinitionId> ValidIds { get; set; } = new HashSet<MyDefinitionId>();
        protected HashSet<MyObjectBuilderType> ValidTypes { get; set; } = new HashSet<MyObjectBuilderType>();
                
        protected int selectedMetaType = 0;
        protected int selectedMetaGroup = 0;
        protected int selectedMetaItemType = 0;
        protected int selectedMetaItemId = 0;
        protected float metaValue = DEFAULT_META;

        protected int selectedMetaBlockType = 0;

        protected override bool CanAddControls(IMyTerminalBlock block)
        {
            var validSubTypes = new string[] { "AIAssemblerController", "AIAssemblerControllerReskin" };
            return block.BlockDefinition.TypeId == typeof(MyObjectBuilder_OreDetector) && validSubTypes.Contains(block.BlockDefinition.SubtypeId);
        }

        public void DoLoadItensIds()
        {
            // Load base itens Ids
            DoLoadPhysicalItemIds();
            // Clear All
            Assemblers.Clear();
            ValidIds.Clear();
            ValidTypes.Clear();
            // Load all assemblers types
            var assemblers = MyDefinitionManager.Static.GetAllDefinitions().Where(x => x.Id.TypeId == typeof(MyObjectBuilder_Assembler)).Cast<MyAssemblerDefinition>().ToList();
            foreach (var assembler in assemblers)
            {
                Assemblers[assembler.Id] = new AssemblerDefinitionInfo(assembler);
                foreach (var id in Assemblers[assembler.Id].ValidIds.Keys)
                {
                    if (!ValidIds.Contains(id))
                        ValidIds.Add(id);
                }
                foreach (var id in Assemblers[assembler.Id].ValidTypes.Keys)
                {
                    if (!ValidTypes.Contains(id))
                        ValidTypes.Add(id);
                }
            }
        }

        protected override void DoInitializeControls()
        {

            if (!AILogisticsAutomationSession.IsUsingExtendedSurvival())
                DoLoadItensIds();

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
                    tooltip: "Sometimes the client desync the configs from the server, of you detect that just click in this button, close the terminal, wait some seconds and open again."
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

            CreateTerminalLabel("SetAssemblerMetaLabel", "Set Assembler Meta");

            CreateCombobox(
                "AssemblerMetaType",
                "Meta Type",
                isWorking,
                (block) =>
                {
                    var system = GetSystem(block);
                    if (system == null) return 0;
                    else return selectedMetaType;
                },
                (block, value) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                    {
                        selectedMetaType = (int)value;
                        UpdateVisual(block);
                    }
                },
                (list) =>
                {
                    list.Add(new MyTerminalControlComboBoxItem() { Key = 0, Value = MyStringId.GetOrCompute("Produce") });
                    list.Add(new MyTerminalControlComboBoxItem() { Key = 1, Value = MyStringId.GetOrCompute("Ignore") });
                },
                tooltip: "Select a meta type."
            );

            CreateCombobox(
                "AssemblerMetaGroup",
                "Meta Group",
                isWorking,
                (block) =>
                {
                    var system = GetSystem(block);
                    if (system == null) return 0;
                    else return selectedMetaGroup;
                },
                (block, value) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                    {
                        selectedMetaGroup = (int)value;
                        UpdateVisual(block);
                    }
                },
                (list) =>
                {
                    list.Add(new MyTerminalControlComboBoxItem() { Key = 0, Value = MyStringId.GetOrCompute("Item Id") });
                    list.Add(new MyTerminalControlComboBoxItem() { Key = 1, Value = MyStringId.GetOrCompute("Item Type") });
                },
                tooltip: "Select a meta group."
            );

            CreateCombobox(
                "AssemblerMetaType",
                "Meta Item Type",
                isWorking,
                (block) =>
                {
                    var system = GetSystem(block);
                    if (system == null) return 0;
                    else return selectedMetaItemType;
                },
                (block, value) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                    {
                        selectedMetaItemType = (int)value;
                        if (PhysicalItemTypes.Values.Any(x => x.Index == selectedMetaItemType))
                        {
                            var typeToUse = PhysicalItemTypes.Values.FirstOrDefault(x => x.Index == selectedMetaItemType);
                            selectedMetaItemId = typeToUse.Items.Min(x => x.Value.Index);
                        }
                        else
                            selectedMetaItemId = 0;
                        UpdateVisual(block);
                    }
                },
                (list) =>
                {
                    list.AddRange(PhysicalItemTypes.Values.Where(x => ValidTypes.Contains(x.Type)).OrderBy(x => x.Index).Select(x => x.ComboBoxItem));
                },
                tooltip: "Select a filter item Type."
            );

            CreateCombobox(
                "AssemblerMetaItemId",
                "Meta Item Id",
                (block) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                        return isWorking.Invoke(block) && selectedMetaGroup == 0 && selectedMetaItemType >= 0;
                    return false;
                },
                (block) =>
                {
                    var system = GetSystem(block);
                    if (system == null) return 0;
                    else return selectedMetaItemId;
                },
                (block, value) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                    {
                        selectedMetaItemId = (int)value;
                    }
                },
                (list) =>
                {
                    if (PhysicalItemTypes.Values.Any(x => x.Index == selectedMetaItemType))
                    {
                        var typeToUse = PhysicalItemTypes.Values.FirstOrDefault(x => x.Index == selectedMetaItemType);
                        list.AddRange(typeToUse.Items.Values.Where(x => ValidIds.Contains(x.Id)).OrderBy(x => x.Index).Select(x => x.ComboBoxItem));
                    }
                },
                tooltip: "Select a meta item Id."
            );

            CreateSlider(
                "SliderMetaValue",
                "Meta Amount",
                (block) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                        return isWorking.Invoke(block) && selectedMetaType == 0;
                    return false;
                },
                (block) =>
                {
                    var system = GetSystem(block);
                    return system != null ? metaValue : MIN_META;
                },
                (block, value) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                    {
                        metaValue = value;
                    }
                },
                (block, val) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                    {
                        val.Append((int)metaValue);
                    }
                },
                new VRageMath.Vector2(MIN_META, MAX_META),
                tooltip: "Set the base amount to be a meta to assemblers stock."
            );

            /* Button Add Meta */
            CreateTerminalButton(
                "AddedSelectedMeta",
                "Add Stock Meta",
                isWorking,
                (block) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                    {
                        var useId = selectedMetaGroup == 0;
                        var ignore = selectedMetaType == 1;
                        if (PhysicalItemTypes.Values.Any(x => x.Index == selectedMetaItemType))
                        {
                            var typeToUse = PhysicalItemTypes.Values.FirstOrDefault(x => x.Index == selectedMetaItemType);
                            if (useId)
                            {
                                if (typeToUse.Items.Values.Any(x => x.Index == selectedMetaItemId))
                                {
                                    var itemToUse = typeToUse.Items.Values.FirstOrDefault(x => x.Index == selectedMetaItemId);
                                    if (ignore)
                                    {
                                        system.Settings.DefaultStock.IgnoreIds.Add(itemToUse.Id);
                                        system.SendToServer("IgnoreIds", "ADD", itemToUse.Id.ToString(), null);
                                        UpdateVisual(block);
                                    }
                                    else
                                    {
                                        system.Settings.DefaultStock.ValidIds[itemToUse.Id] = (int)metaValue;
                                        system.SendToServer("ValidIds", "ADD", itemToUse.Id.ToString(), null);
                                        UpdateVisual(block);
                                    }
                                }
                            }
                            else
                            {
                                if (ignore)
                                {
                                    system.Settings.DefaultStock.IgnoreTypes.Add(typeToUse.Type);
                                    system.SendToServer("IgnoreTypes", "ADD", typeToUse.Type.ToString(), null);
                                    UpdateVisual(block);
                                }
                                else
                                {
                                    system.Settings.DefaultStock.ValidTypes[typeToUse.Type] = (int)metaValue;
                                    system.SendToServer("ValidTypes", "ADD", typeToUse.Type.ToString(), null);
                                    UpdateVisual(block);
                                }
                            }
                        }
                    }
                }
            );

            /* Filter List */

            CreateListbox(
                "ListStockMetas",
                "Stock Metas List",
                isWorking,
                (block, list, selectedList) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                    {
                        foreach (var validType in system.Settings.DefaultStock.ValidTypes)
                        {
                            var typeIndex = PhysicalItemTypes[validType.Key].Index;
                            var typeName = PhysicalItemTypes[validType.Key].DisplayText;
                            var name = string.Format("[{1}] (TYPE) {0}", typeName, validType.Value);
                            var key = string.Format("VT_{0}", typeIndex);
                            var item = new MyTerminalControlListBoxItem(MyStringId.GetOrCompute(name), MyStringId.GetOrCompute(name), key);
                            list.Add(item);
                            if (key == system.Settings.SelectedAddedMetaId)
                                selectedList.Add(item);
                        }
                        foreach (var validId in system.Settings.DefaultStock.ValidIds)
                        {
                            var typeIndex = PhysicalItemIds[validId.Key].Index;
                            var typeName = PhysicalItemIds[validId.Key].DisplayText;
                            var name = string.Format("[{1}] (ID) {0}", typeName, validId.Value);
                            var key = string.Format("VI_{0}", typeIndex);
                            var item = new MyTerminalControlListBoxItem(MyStringId.GetOrCompute(name), MyStringId.GetOrCompute(name), key);
                            list.Add(item);
                            if (key == system.Settings.SelectedAddedMetaId)
                                selectedList.Add(item);
                        }
                        foreach (var ignoreType in system.Settings.DefaultStock.IgnoreTypes)
                        {
                            var typeIndex = PhysicalItemTypes[ignoreType].Index;
                            var typeName = PhysicalItemTypes[ignoreType].DisplayText;
                            var name = string.Format("[IGNORE] (TYPE) {0}", typeName);
                            var key = string.Format("IT_{0}", typeIndex);
                            var item = new MyTerminalControlListBoxItem(MyStringId.GetOrCompute(name), MyStringId.GetOrCompute(name), key);
                            list.Add(item);
                            if (key == system.Settings.SelectedAddedMetaId)
                                selectedList.Add(item);
                        }
                        foreach (var ignoreId in system.Settings.DefaultStock.IgnoreIds)
                        {
                            var typeIndex = PhysicalItemIds[ignoreId].Index;
                            var typeName = PhysicalItemIds[ignoreId].DisplayText;
                            var name = string.Format("[IGNORE] (ID) {0}", typeName);
                            var key = string.Format("II_{0}", typeIndex);
                            var item = new MyTerminalControlListBoxItem(MyStringId.GetOrCompute(name), MyStringId.GetOrCompute(name), key);
                            list.Add(item);
                            if (key == system.Settings.SelectedAddedMetaId)
                                selectedList.Add(item);
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
                        system.Settings.SelectedAddedMetaId = selectedList[0].UserData.ToString();
                        UpdateVisual(block);
                    }
                },
                tooltip: "Select a stock meta to remove."
            );

            CreateTerminalButton(
                "RemoveSelectedFilter",
                "Remove Selected Filter",
                (block) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                        return isWorking.Invoke(block) && !string.IsNullOrWhiteSpace(system.Settings.SelectedAddedMetaId);
                    return false;
                },
                (block) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                    {
                        var parts = system.Settings.SelectedAddedMetaId.Split('_');
                        if (parts.Length == 2)
                        {
                            var index = int.Parse(parts[1]);
                            switch (parts[0])
                            {
                                case "VT":
                                    var itemVT = PhysicalItemTypes.Values.FirstOrDefault(x => x.Index == index);
                                    if (system.Settings.DefaultStock.ValidTypes.ContainsKey(itemVT.Type))
                                    {
                                        system.Settings.DefaultStock.ValidTypes.Remove(itemVT.Type);
                                        system.SendToServer("validTypes", "DEL", itemVT.Type.ToString(), null);
                                        UpdateVisual(block);
                                    }
                                    break;
                                case "VI":
                                    var itemVI = PhysicalItemIds.Values.FirstOrDefault(x => x.Index == index);
                                    if (system.Settings.DefaultStock.ValidIds.ContainsKey(itemVI.Id))
                                    {
                                        system.Settings.DefaultStock.ValidIds.Remove(itemVI.Id);
                                        system.SendToServer("ValidIds", "DEL", itemVI.Id.ToString(), null);
                                        UpdateVisual(block);
                                    }
                                    break;
                                case "IT":
                                    var itemIT = PhysicalItemTypes.Values.FirstOrDefault(x => x.Index == index);
                                    if (system.Settings.DefaultStock.IgnoreTypes.Contains(itemIT.Type))
                                    {
                                        system.Settings.DefaultStock.IgnoreTypes.Remove(itemIT.Type);
                                        system.SendToServer("IgnoreTypes", "DEL", itemIT.Type.ToString(), null);
                                        UpdateVisual(block);
                                    }
                                    break;
                                case "II":
                                    var itemII = PhysicalItemIds.Values.FirstOrDefault(x => x.Index == index);
                                    if (system.Settings.DefaultStock.IgnoreIds.Contains(itemII.Id))
                                    {
                                        system.Settings.DefaultStock.IgnoreIds.Remove(itemII.Id);
                                        system.SendToServer("IgnoreIds", "DEL", itemII.Id.ToString(), null);
                                        UpdateVisual(block);
                                    }
                                    break;
                            }
                        }
                    }
                }
            );

            CreateTerminalSeparator("IgnoreBlocksSeparator");

            CreateTerminalLabel("IgnoreBlocksSeparatorLable", "Selected the Ignored Blocks");

            CreateListbox(
                "ListBlocksType",
                "Blocks of selected type",
                isWorkingAndEnabled,
                (block, list, selectedList) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                    {
                        var targetGrid = system.CurrentEntity.CubeGrid as MyCubeGrid;

                        MyObjectBuilderType[] targetFilter = new MyObjectBuilderType[] { typeof(MyObjectBuilder_Assembler) };
                        IEnumerable<long> ignoreBlocks = system.Settings.GetIgnoreAssembler();

                        foreach (var inventory in targetGrid.Inventories.Where(x => targetFilter.Contains(x.BlockDefinition.Id.TypeId)))
                        {
                            var added = system.Settings.GetDefinitions().ContainsKey(inventory.EntityId);
                            if (!added)
                            {
                                if (!ignoreBlocks.Contains(inventory.EntityId))
                                {

                                    var name = string.Format("{1} - ({0})", inventory.BlockDefinition.DisplayNameText, inventory.DisplayNameText);
                                    var item = new MyTerminalControlListBoxItem(MyStringId.GetOrCompute(name), MyStringId.GetOrCompute(name), inventory.EntityId);

                                    list.Add(item);

                                    if (system.Settings.SelectedIgnoreEntityId == inventory.EntityId)
                                    {
                                        selectedList.Add(item);
                                        system.Settings.SelectedIgnoreEntityId = inventory.EntityId;
                                    }

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
                        var targetGrid = system.CurrentEntity.CubeGrid as MyCubeGrid;
                        var query = targetGrid.Inventories.Where(x => x.EntityId == (long)selectedList[0].UserData);
                        if (query.Any())
                        {
                            system.Settings.SelectedIgnoreEntityId = query.FirstOrDefault().EntityId;
                            UpdateVisual(block);
                        }
                    }
                },
                tooltip: "Select one or more blocks to be ignored by the AI Block."
            );

            CreateTerminalButton(
                "ButtonAddIgnored",
                "Add Selected To Ignored",
                (block) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                    {
                        return isWorkingAndEnabled.Invoke(block) && system.Settings.SelectedIgnoreEntityId != 0;
                    }
                    return false;
                },
                (block) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                    {
                        var targetGrid = system.CurrentEntity.CubeGrid as MyCubeGrid;
                        var query = targetGrid.Inventories.Where(x => x.EntityId == system.Settings.SelectedIgnoreEntityId);
                        if (query.Any())
                        {
                            var inventory = query.FirstOrDefault();

                            if (!system.Settings.GetIgnoreAssembler().Contains(system.Settings.SelectedIgnoreEntityId))
                            {
                                system.Settings.GetIgnoreAssembler().Add(system.Settings.SelectedIgnoreEntityId);
                                system.SendToServer("IgnoreAssembler", "ADD", system.Settings.SelectedIgnoreEntityId.ToString());
                                UpdateVisual(block);
                            }
                        }
                        system.Settings.SelectedIgnoreEntityId = 0;
                    }
                }
            );

            CreateListbox(
                "ListBlocksIgnored",
                "Ignored Blocks",
                isWorkingAndEnabled,
                (block, list, selectedList) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                    {
                        var targetGrid = system.CurrentEntity.CubeGrid as MyCubeGrid;

                        foreach (var inventory in targetGrid.Inventories.Where(x => system.Settings.GetIgnoreAssembler().Contains(x.EntityId)))
                        {

                            var name = string.Format("{1} - ({0})", inventory.BlockDefinition.DisplayNameText, inventory.DisplayNameText);
                            var item = new MyTerminalControlListBoxItem(MyStringId.GetOrCompute(name), MyStringId.GetOrCompute(name), inventory.EntityId);

                            list.Add(item);

                            if (system.Settings.SelectedAddedIgnoreEntityId == inventory.EntityId)
                            {
                                selectedList.Add(item);
                                system.Settings.SelectedAddedIgnoreEntityId = inventory.EntityId;
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
                            system.Settings.SelectedAddedIgnoreEntityId = query.FirstOrDefault().EntityId;
                            UpdateVisual(block);
                        }
                    }
                }
            );

            CreateTerminalButton(
                "ButtonRemoveIgnored",
                "Remove Selected Ignored Block",
                (block) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                    {
                        return isWorkingAndEnabled.Invoke(block) && system.Settings.SelectedAddedIgnoreEntityId != 0;
                    }
                    return false;
                },
                (block) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                    {
                        if (system.Settings.GetIgnoreAssembler().Contains(system.Settings.SelectedAddedIgnoreEntityId))
                        {
                            system.Settings.GetIgnoreAssembler().Remove(system.Settings.SelectedAddedIgnoreEntityId);
                            system.SendToServer("IgnoreAssembler", "DEL", system.Settings.SelectedAddedIgnoreEntityId.ToString());
                            UpdateVisual(block);
                        }
                        system.Settings.SelectedAddedIgnoreEntityId = 0;
                    }
                }
            );

        }

        protected override string GetActionPrefix()
        {
            return "AIAssemblerController";
        }

        private readonly string[] idsToRemove = new string[] { "Range", "BroadcastUsingAntennas" };
        protected override string[] GetIdsToRemove()
        {
            return idsToRemove;
        }

    }

}