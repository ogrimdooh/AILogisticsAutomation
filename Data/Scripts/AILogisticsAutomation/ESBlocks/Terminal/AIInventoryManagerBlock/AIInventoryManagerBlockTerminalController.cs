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
    public class AIInventoryManagerBlockTerminalController : BaseTerminalController<AIInventoryManagerBlock, IMyOreDetector>
    {

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

        protected override bool CanAddControls(IMyTerminalBlock block)
        {
            return block.BlockDefinition.TypeId == typeof(MyObjectBuilder_OreDetector) && block.BlockDefinition.SubtypeId == "AIInventoryManager";
        }

        protected void LoadItensIds()
        {
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

            Func<IMyTerminalBlock, bool> isWorkingAndCargoSelected = (block) =>
            {
                var system = GetSystem(block);
                if (system != null)
                {
                    var targetGrid = system.CurrentEntity.CubeGrid as MyCubeGrid;
                    var query = targetGrid.Inventories.Where(x => x.EntityId == system.Settings.SelectedEntityId);
                    return query.Any() && isWorkingAndEnabled.Invoke(block);
                }
                return false;
            };

            Func<IMyTerminalBlock, bool> isWorkingAndCargoSelectedIsAdded = (block) =>
            {
                var system = GetSystem(block);
                if (system != null)
                {
                    var targetGrid = system.CurrentEntity.CubeGrid as MyCubeGrid;
                    var exits = targetGrid.Inventories.Any(x => x.EntityId == system.Settings.SelectedEntityId);
                    var added = system.Settings.GetDefinitions().ContainsKey(system.Settings.SelectedEntityId);
                    return exits && added && isWorkingAndEnabled.Invoke(block);
                }
                return false;
            };

            Func<IMyTerminalBlock, bool> isWorkingAndCargoSelectedIsAddedAndFilterIsSelected = (block) =>
            {
                var system = GetSystem(block);
                if (system != null)
                {
                    var targetGrid = system.CurrentEntity.CubeGrid as MyCubeGrid;
                    var exits = targetGrid.Inventories.Any(x => x.EntityId == system.Settings.SelectedEntityId);
                    var added = system.Settings.GetDefinitions().ContainsKey(system.Settings.SelectedEntityId);
                    return exits && added && !string.IsNullOrWhiteSpace(system.Settings.SelectedAddedFilterId) && isWorkingAndEnabled.Invoke(block);
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

            CreateTerminalLabel("CargosDefinition", "Cargos Definition");

            var checkboxPullFromConnectedGrids = CreateCheckbox(
                "CheckboxPullFromConnectedGrids",
                "Pull from connected grids",
                isWorkingAndEnabled,
                (block) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                    {
                        return system.Settings.GetPullFromConnectedGrids();
                    }
                    return false;
                },
                (block, value) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                    {
                        system.Settings.SetPullFromConnectedGrids(value);
                        system.SendToServer("PullFromConnectedGrids", "SET", value.ToString());
                    }
                },
                tooltip: "If enabled will pull itens from connected grids, that is not using ignored connectors.",
                supMultiple: true
            );
            CreateCheckBoxAction("PullFromConnectedGrids", checkboxPullFromConnectedGrids);

            var checkboxPullFromSubGrids = CreateCheckbox(
                "CheckboxPullFromSubGrids",
                "Pull from sub-grids",
                isWorkingAndEnabled,
                (block) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                    {
                        return system.Settings.GetPullSubGrids();
                    }
                    return false;
                },
                (block, value) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                    {
                        system.Settings.SetPullSubGrids(value);
                        system.SendToServer("PullFromSubGrids", "SET", value.ToString());
                    }
                },
                tooltip: "If enabled will pull itens from attached sub-grids.",
                supMultiple: true
            );
            CreateCheckBoxAction("PullFromSubGrids", checkboxPullFromSubGrids);

            var comboBoxSortItensType = CreateCombobox(
                "SorterType",
                "Sorter Type",
                isWorkingAndEnabled,
                (block) =>
                {
                    var system = GetSystem(block);
                    if (system == null) return 0;
                    else return system.Settings.GetSortItensType();
                },
                (block, value) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                    {
                        system.Settings.SetSortItensType(value);
                        system.SendToServer("SortItensType", "SET", value.ToString());
                    }
                },
                (list) =>
                {
                    list.Add(new MyTerminalControlComboBoxItem() { Key = 0, Value = MyStringId.GetOrCompute("None") });
                    list.Add(new MyTerminalControlComboBoxItem() { Key = 1, Value = MyStringId.GetOrCompute("Name") });
                    list.Add(new MyTerminalControlComboBoxItem() { Key = 2, Value = MyStringId.GetOrCompute("Mass") });
                    list.Add(new MyTerminalControlComboBoxItem() { Key = 3, Value = MyStringId.GetOrCompute("Type Name [Item Name]") });
                    list.Add(new MyTerminalControlComboBoxItem() { Key = 4, Value = MyStringId.GetOrCompute("Type Name [Item Mass]") });
                },
                tooltip: "Select a sorter type to do with the itens.",
                supMultiple: true
            );
            CreateComboBoxAction("SortItensType", comboBoxSortItensType);

            var checkboxStackIfPossible = CreateCheckbox(
                "CheckboxStackIfPossible",
                "Stack Items",
                isWorkingAndEnabled,
                (block) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                    {
                        return system.Settings.GetStackIfPossible();
                    }
                    return false;
                },
                (block, value) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                    {
                        system.Settings.SetStackIfPossible(value);
                        system.SendToServer("StackIfPossible", "SET", value.ToString());
                    }
                },
                tooltip: "If enabled will stack itens slots if possible.",
                supMultiple: true
            );
            CreateCheckBoxAction("StackIfPossible", checkboxStackIfPossible);

            CreateTerminalLabel("PullCargosDefinition", "Pull Cargos Definition");

            CreateListbox(
                "ListCargoContainers",
                "Cargo Container List",
                isWorkingAndEnabled,
                (block, list, selectedList) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                    {
                        var targetGrid = system.CurrentEntity.CubeGrid as MyCubeGrid;
                        foreach (var inventory in targetGrid.Inventories.Where(x => x.BlockDefinition.Id.TypeId == typeof(MyObjectBuilder_CargoContainer)))
                        {
                            var added = system.Settings.GetDefinitions().ContainsKey(inventory.EntityId);

                            var name = string.Format("[{0}] {2} - ({1})", added ? "X" : " ", inventory.BlockDefinition.DisplayNameText, inventory.DisplayNameText);
                            var item = new MyTerminalControlListBoxItem(MyStringId.GetOrCompute(name), MyStringId.GetOrCompute(name), inventory.EntityId);

                            list.Add(item);

                            if (system.Settings.SelectedEntityId == inventory.EntityId)
                            {
                                selectedList.Add(item);
                                system.Settings.SelectedEntityId = inventory.EntityId;
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
                            system.Settings.SelectedEntityId = query.FirstOrDefault().EntityId;
                            UpdateVisual(block);
                        }
                    }
                },
                tooltip: "Select a cargo container to set pull settings."
            );

            CreateCheckbox(
                "CheckboxAddContainer",
                "Added selected cargo to pull list",
                isWorkingAndEnabled,
                (block) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                    {
                        var targetGrid = system.CurrentEntity.CubeGrid as MyCubeGrid;
                        var exists = targetGrid.Inventories.Any(x => x.EntityId == system.Settings.SelectedEntityId);
                        if (exists)
                        {
                            return system.Settings.GetDefinitions().ContainsKey(system.Settings.SelectedEntityId);
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
                        var exists = targetGrid.Inventories.Any(x => x.EntityId == system.Settings.SelectedEntityId);
                        if (exists)
                        {
                            var added = system.Settings.GetDefinitions().ContainsKey(system.Settings.SelectedEntityId);
                            if (value)
                            {
                                if (!added)
                                {
                                    system.Settings.GetDefinitions()[system.Settings.SelectedEntityId] = new AIInventoryManagerCargoDefinition()
                                    {
                                        EntityId = system.Settings.SelectedEntityId
                                    };
                                    system.SendToServer("Definitions", "ADD", system.Settings.SelectedEntityId.ToString());
                                    if (system.Settings.GetIgnoreCargos().Contains(system.Settings.SelectedEntityId))
                                    {
                                        system.Settings.GetIgnoreCargos().Remove(system.Settings.SelectedEntityId);
                                        system.SendToServer("IgnoreCargos", "DEL", system.Settings.SelectedEntityId.ToString());
                                    }
                                    UpdateVisual(block);
                                }
                            }
                            else
                            {
                                if (added)
                                {
                                    var dataToRemove = system.Settings.GetDefinitions().ContainsKey(system.Settings.SelectedEntityId);
                                    if (dataToRemove)
                                    {
                                        system.Settings.GetDefinitions().Remove(system.Settings.SelectedEntityId);
                                        system.SendToServer("Definitions", "ADD", system.Settings.SelectedEntityId.ToString());
                                    }
                                    UpdateVisual(block);
                                }
                            }
                        }
                    }
                },
                tooltip: "Cargos added to list will be used as store to all containers in grid."
            );

            CreateTerminalSeparator("CargoOptionsSeparator");

            CreateTerminalLabel("CargoOptionsSeparatorLable", "Selected Cargo Filter");

            CreateCombobox(
                "FilterType",
                "Filter Type",
                isWorkingAndCargoSelectedIsAdded,
                (block) =>
                {
                    var system = GetSystem(block);
                    if (system == null) return 0;
                    else return selectedFilterType;
                },
                (block, value) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                    {
                        selectedFilterType = value;
                    }
                },
                (list) =>
                {
                    list.Add(new MyTerminalControlComboBoxItem() { Key = 0, Value = MyStringId.GetOrCompute("Pull") });
                    list.Add(new MyTerminalControlComboBoxItem() { Key = 1, Value = MyStringId.GetOrCompute("Ignore") });
                },
                tooltip: "Select a filter type."
            );

            CreateCombobox(
                "FilterGroup",
                "Filter Group",
                isWorkingAndCargoSelectedIsAdded,
                (block) =>
                {
                    var system = GetSystem(block);
                    if (system == null) return 0;
                    else return selectedFilterGroup;
                },
                (block, value) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                    {
                        selectedFilterGroup = value;
                        UpdateVisual(block);
                    }
                },
                (list) =>
                {
                    list.Add(new MyTerminalControlComboBoxItem() { Key = 0, Value = MyStringId.GetOrCompute("Item Id") });
                    list.Add(new MyTerminalControlComboBoxItem() { Key = 1, Value = MyStringId.GetOrCompute("Item Type") });
                },
                tooltip: "Select a filter group."
            );

            CreateCombobox(
                "FilterItemType",
                "Filter Item Type",
                isWorkingAndCargoSelectedIsAdded,
                (block) =>
                {
                    var system = GetSystem(block);
                    if (system == null) return 0;
                    else return selectedFilterItemType;
                },
                (block, value) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                    {
                        selectedFilterItemType = (int)value;
                        var typeToUse = validTypes[selectedFilterItemType];
                        if (validIdsByTypeUI.ContainsKey(typeToUse))
                            selectedFilterItemId = (int)validIdsByTypeUI[typeToUse].FirstOrDefault().Key;
                        else
                            selectedFilterItemId = 0;
                        UpdateVisual(block);
                    }
                },
                (list) =>
                {
                    list.AddRange(validTypesUI);
                },
                tooltip: "Select a filter item Type."
            );

            CreateCombobox(
                "FilterItemId",
                "Filter Item Id",
                (block) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                        return isWorkingAndCargoSelectedIsAdded.Invoke(block) && selectedFilterGroup == 0 && selectedFilterItemType >= 0;
                    return false;
                },
                (block) =>
                {
                    var system = GetSystem(block);
                    if (system == null) return 0;
                    else return selectedFilterItemId;
                },
                (block, value) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                    {
                        selectedFilterItemId = (int)value;
                    }
                },
                (list) =>
                {
                    var typeToUse = validTypes[selectedFilterItemType];
                    if (validIdsByTypeUI.ContainsKey(typeToUse))
                        list.AddRange(validIdsByTypeUI[typeToUse]);
                },
                tooltip: "Select a filter item Id."
            );

            /* Button Add Filter */
            CreateTerminalButton(
                "AddedSelectedFilter",
                "Added Selected Filter",
                isWorkingAndCargoSelectedIsAdded,
                (block) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                    {
                        var targetGrid = system.CurrentEntity.CubeGrid as MyCubeGrid;
                        var exits = targetGrid.Inventories.Any(x => x.EntityId == system.Settings.SelectedEntityId);
                        if (exits)
                        {
                            var lista = system.Settings.GetDefinitions();
                            var def = lista.ContainsKey(system.Settings.SelectedEntityId) ? lista[system.Settings.SelectedEntityId] : null;
                            if (def != null)
                            {
                                var useId = selectedFilterGroup == 0;
                                switch (selectedFilterType)
                                {
                                    case 0:
                                        if (useId)
                                        {
                                            var idToUse = validIds[selectedFilterItemId];
                                            if (!def.ValidIds.Contains(idToUse))
                                            {
                                                def.ValidIds.Add(idToUse);
                                                system.SendToServer("ValidIds", "ADD", idToUse.ToString(), def.EntityId.ToString());
                                                UpdateVisual(block);
                                            }
                                        }
                                        else
                                        {
                                            var typeToUse = validTypes[selectedFilterItemType];
                                            if (!def.ValidTypes.Contains(typeToUse))
                                            {
                                                def.ValidTypes.Add(typeToUse);
                                                system.SendToServer("ValidTypes", "ADD", typeToUse.ToString(), def.EntityId.ToString());
                                                UpdateVisual(block);
                                            }
                                        }
                                        break;
                                    case 1:
                                        if (useId)
                                        {
                                            var idToIgnore = validIds[selectedFilterItemId];
                                            if (!def.IgnoreIds.Contains(idToIgnore))
                                            {
                                                def.IgnoreIds.Add(idToIgnore);
                                                system.SendToServer("IgnoreIds", "ADD", idToIgnore.ToString(), def.EntityId.ToString());
                                                UpdateVisual(block);
                                            }
                                        }
                                        else
                                        {
                                            var typeToIgnore = validTypes[selectedFilterItemType];
                                            if (!def.IgnoreTypes.Contains(typeToIgnore))
                                            {
                                                def.IgnoreTypes.Add(typeToIgnore);
                                                system.SendToServer("IgnoreTypes", "ADD", typeToIgnore.ToString(), def.EntityId.ToString());
                                                UpdateVisual(block);
                                            }
                                        }
                                        break;
                                }
                            }
                        }
                    }
                }
            );

            /* Filter List */

            CreateListbox(
                "ListCargoContainerFilters",
                "Cargo Container Filters List",
                isWorkingAndCargoSelectedIsAdded,
                (block, list, selectedList) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                    {
                        var targetGrid = system.CurrentEntity.CubeGrid as MyCubeGrid;
                        var exits = targetGrid.Inventories.Any(x => x.EntityId == system.Settings.SelectedEntityId);
                        if (exits)
                        {
                            var lista = system.Settings.GetDefinitions();
                            var def = lista.ContainsKey(system.Settings.SelectedEntityId) ? lista[system.Settings.SelectedEntityId] : null;
                            if (def != null)
                            {
                                foreach (var validType in def.ValidTypes)
                                {
                                    var typeIndex = validTypes.IndexOf(validType);
                                    var typeName = validTypesUI[typeIndex].Value.String;
                                    var name = string.Format("[PULL] (TYPE) {0}", typeName);
                                    var item = new MyTerminalControlListBoxItem(MyStringId.GetOrCompute(name), MyStringId.GetOrCompute(name), string.Format("VT_{0}", typeIndex));
                                    list.Add(item);
                                }
                                foreach (var validId in def.ValidIds)
                                {
                                    var typeIndex = validIds.IndexOf(validId);
                                    var typeName = validIdsUI[typeIndex].Value.String;
                                    var name = string.Format("[PULL] (ID) {0}", typeName);
                                    var item = new MyTerminalControlListBoxItem(MyStringId.GetOrCompute(name), MyStringId.GetOrCompute(name), string.Format("VI_{0}", typeIndex));
                                    list.Add(item);
                                }
                                foreach (var ignoreType in def.IgnoreTypes)
                                {
                                    var typeIndex = validTypes.IndexOf(ignoreType);
                                    var typeName = validTypesUI[typeIndex].Value.String;
                                    var name = string.Format("[IGNORE] (TYPE) {0}", typeName);
                                    var item = new MyTerminalControlListBoxItem(MyStringId.GetOrCompute(name), MyStringId.GetOrCompute(name), string.Format("IT_{0}", typeIndex));
                                    list.Add(item);
                                }
                                foreach (var ignoreId in def.IgnoreIds)
                                {
                                    var typeIndex = validIds.IndexOf(ignoreId);
                                    var typeName = validIdsUI[typeIndex].Value.String;
                                    var name = string.Format("[IGNORE] (ID) {0}", typeName);
                                    var item = new MyTerminalControlListBoxItem(MyStringId.GetOrCompute(name), MyStringId.GetOrCompute(name), string.Format("II_{0}", typeIndex));
                                    list.Add(item);
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
                        system.Settings.SelectedAddedFilterId = selectedList[0].UserData.ToString();
                        UpdateVisual(block);
                    }
                },
                tooltip: "Select a a filter to remove."
            );

            CreateTerminalButton(
                "RemoveSelectedFilter",
                "Remove Selected Filter",
                isWorkingAndCargoSelectedIsAddedAndFilterIsSelected,
                (block) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                    {
                        var targetGrid = system.CurrentEntity.CubeGrid as MyCubeGrid;
                        var exits = targetGrid.Inventories.Any(x => x.EntityId == system.Settings.SelectedEntityId);
                        if (exits)
                        {
                            var lista = system.Settings.GetDefinitions();
                            var def = lista.ContainsKey(system.Settings.SelectedEntityId) ? lista[system.Settings.SelectedEntityId] : null;
                            if (def != null)
                            {
                                var parts = system.Settings.SelectedAddedFilterId.Split('_');
                                if (parts.Length == 2)
                                {
                                    var index = int.Parse(parts[1]);
                                    switch (parts[0])
                                    {
                                        case "VT":
                                            var itemVT = validTypes[index];
                                            if (def.ValidTypes.Contains(itemVT))
                                            {
                                                def.ValidTypes.Remove(itemVT);
                                                system.SendToServer("validTypes", "DEL", itemVT.ToString(), def.EntityId.ToString());
                                                UpdateVisual(block);
                                            }
                                            break;
                                        case "VI":
                                            var itemVI = validIds[index];
                                            if (def.ValidIds.Contains(itemVI))
                                            {
                                                def.ValidIds.Remove(itemVI);
                                                system.SendToServer("ValidIds", "DEL", itemVI.ToString(), def.EntityId.ToString());
                                                UpdateVisual(block);
                                            }
                                            break;
                                        case "IT":
                                            var itemIT = validTypes[index];
                                            if (def.IgnoreTypes.Contains(itemIT))
                                            {
                                                def.IgnoreTypes.Remove(itemIT);
                                                system.SendToServer("IgnoreTypes", "DEL", itemIT.ToString(), def.EntityId.ToString());
                                                UpdateVisual(block);
                                            }
                                            break;
                                        case "II":
                                            var itemII = validIds[index];
                                            if (def.IgnoreIds.Contains(itemII))
                                            {
                                                def.IgnoreIds.Remove(itemII);
                                                system.SendToServer("IgnoreIds", "DEL", itemII.ToString(), def.EntityId.ToString());
                                                UpdateVisual(block);
                                            }
                                            break;
                                    }
                                }
                            }
                        }
                    }
                }
            );

            CreateTerminalSeparator("IgnoreBlocksSeparator");

            CreateTerminalLabel("IgnoreBlocksSeparatorLable", "Selected the Ignored Blocks");

            CreateCombobox(
                "FilterBlockType",
                "Filter Block Type",
                isWorkingAndEnabled,
                (block) =>
                {
                    var system = GetSystem(block);
                    if (system == null) return 0;
                    else return selectedFilterBlockType;
                },
                 (block, value) =>
                 {
                     var system = GetSystem(block);
                     if (system != null)
                     {
                         selectedFilterBlockType = value;
                         UpdateVisual(block);
                     }
                 },
                 (list) =>
                 {
                     list.Add(new MyTerminalControlComboBoxItem() { Key = 0, Value = MyStringId.GetOrCompute("Cargo Container") });
                     list.Add(new MyTerminalControlComboBoxItem() { Key = 1, Value = MyStringId.GetOrCompute("Functional Blocks") });
                     list.Add(new MyTerminalControlComboBoxItem() { Key = 2, Value = MyStringId.GetOrCompute("Connector") });
                 },
                 tooltip: "Select a filter to the block type."
            );

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

                        MyObjectBuilderType[] targetFilter = new MyObjectBuilderType[] { };
                        IEnumerable<long> ignoreBlocks = new List<long>();
                        switch (selectedFilterBlockType)
                        {
                            case 0:
                                targetFilter = new MyObjectBuilderType[] { typeof(MyObjectBuilder_CargoContainer) };
                                ignoreBlocks = system.Settings.GetIgnoreCargos();
                                break;
                            case 1:
                                targetFilter = new MyObjectBuilderType[] { typeof(MyObjectBuilder_Assembler), typeof(MyObjectBuilder_Refinery), typeof(MyObjectBuilder_Reactor), typeof(MyObjectBuilder_HydrogenEngine), typeof(MyObjectBuilder_OxygenGenerator), typeof(MyObjectBuilder_OxygenTank), typeof(MyObjectBuilder_GasTank) };
                                ignoreBlocks = system.Settings.GetIgnoreFunctionalBlocks();
                                break;
                            case 2:
                                targetFilter = new MyObjectBuilderType[] { typeof(MyObjectBuilder_ShipConnector) };
                                ignoreBlocks = system.Settings.GetIgnoreConnectors();
                                break;
                        }

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

                            var targetCargoContainerFilter = new MyObjectBuilderType[] { typeof(MyObjectBuilder_CargoContainer) };
                            var targetFunctionalFilter = new MyObjectBuilderType[] { typeof(MyObjectBuilder_Assembler), typeof(MyObjectBuilder_Refinery), typeof(MyObjectBuilder_Reactor), typeof(MyObjectBuilder_HydrogenEngine), typeof(MyObjectBuilder_OxygenGenerator), typeof(MyObjectBuilder_OxygenTank), typeof(MyObjectBuilder_GasTank) };
                            var targetConnectorFilter = new MyObjectBuilderType[] { typeof(MyObjectBuilder_ShipConnector) };

                            if (targetCargoContainerFilter.Contains(inventory.BlockDefinition.Id.TypeId))
                            {
                                if (!system.Settings.GetIgnoreCargos().Contains(system.Settings.SelectedIgnoreEntityId))
                                {
                                    system.Settings.GetIgnoreCargos().Add(system.Settings.SelectedIgnoreEntityId);
                                    system.SendToServer("IgnoreCargos", "ADD", system.Settings.SelectedIgnoreEntityId.ToString());
                                    UpdateVisual(block);
                                }
                            }
                            else if (targetFunctionalFilter.Contains(inventory.BlockDefinition.Id.TypeId))
                            {
                                if (!system.Settings.GetIgnoreFunctionalBlocks().Contains(system.Settings.SelectedIgnoreEntityId))
                                {
                                    system.Settings.GetIgnoreFunctionalBlocks().Add(system.Settings.SelectedIgnoreEntityId);
                                    system.SendToServer("IgnoreFunctionalBlocks", "ADD", system.Settings.SelectedIgnoreEntityId.ToString());
                                    UpdateVisual(block);
                                }
                            }
                            else if (targetConnectorFilter.Contains(inventory.BlockDefinition.Id.TypeId))
                            {
                                if (!system.Settings.GetIgnoreConnectors().Contains(system.Settings.SelectedIgnoreEntityId))
                                {
                                    system.Settings.GetIgnoreConnectors().Add(system.Settings.SelectedIgnoreEntityId);
                                    system.SendToServer("IgnoreConnectors", "ADD", system.Settings.SelectedIgnoreEntityId.ToString());
                                    UpdateVisual(block);
                                }
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

                        List<long> addedBlocks = new List<long>();
                        addedBlocks.AddRange(system.Settings.GetIgnoreCargos());
                        addedBlocks.AddRange(system.Settings.GetIgnoreFunctionalBlocks());
                        addedBlocks.AddRange(system.Settings.GetIgnoreConnectors());

                        var targetCargoContainerFilter = new MyObjectBuilderType[] { typeof(MyObjectBuilder_CargoContainer) };
                        var targetFunctionalFilter = new MyObjectBuilderType[] { typeof(MyObjectBuilder_Assembler), typeof(MyObjectBuilder_Refinery), typeof(MyObjectBuilder_Reactor), typeof(MyObjectBuilder_HydrogenEngine), typeof(MyObjectBuilder_OxygenGenerator), typeof(MyObjectBuilder_OxygenTank), typeof(MyObjectBuilder_GasTank) };
                        var targetConnectorFilter = new MyObjectBuilderType[] { typeof(MyObjectBuilder_ShipConnector) };

                        foreach (var inventory in targetGrid.Inventories.Where(x => addedBlocks.Contains(x.EntityId)))
                        {

                            var group = "";
                            if (targetCargoContainerFilter.Contains(inventory.BlockDefinition.Id.TypeId))
                                group = "CARGO";
                            else if (targetFunctionalFilter.Contains(inventory.BlockDefinition.Id.TypeId))
                                group = "FUNCTIONAL";
                            else if (targetConnectorFilter.Contains(inventory.BlockDefinition.Id.TypeId))
                                group = "CONNECTOR";

                            var name = string.Format("[{2}] {1} - ({0})", inventory.BlockDefinition.DisplayNameText, inventory.DisplayNameText, group);
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
                },
                tooltip: "Select one or more blocks to be ignored by the AI Block."
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
                        if (system.Settings.GetIgnoreCargos().Contains(system.Settings.SelectedAddedIgnoreEntityId))
                        {
                            system.Settings.GetIgnoreCargos().Remove(system.Settings.SelectedAddedIgnoreEntityId);
                            system.SendToServer("IgnoreCargos", "DEL", system.Settings.SelectedAddedIgnoreEntityId.ToString());
                            UpdateVisual(block);
                        }
                        else if (system.Settings.GetIgnoreFunctionalBlocks().Contains(system.Settings.SelectedAddedIgnoreEntityId))
                        {
                            system.Settings.GetIgnoreFunctionalBlocks().Remove(system.Settings.SelectedAddedIgnoreEntityId);
                            system.SendToServer("IgnoreFunctionalBlocks", "DEL", system.Settings.SelectedAddedIgnoreEntityId.ToString());
                            UpdateVisual(block);
                        }
                        else if (system.Settings.GetIgnoreConnectors().Contains(system.Settings.SelectedAddedIgnoreEntityId))
                        {
                            system.Settings.GetIgnoreConnectors().Remove(system.Settings.SelectedAddedIgnoreEntityId);
                            system.SendToServer("IgnoreConnectors", "DEL", system.Settings.SelectedAddedIgnoreEntityId.ToString());
                            UpdateVisual(block);
                        }
                        system.Settings.SelectedAddedIgnoreEntityId = 0;
                    }
                }
            );

            CreateTerminalSeparator("FuncionalBlockOptionsSeparator");

            CreateTerminalLabel("FuncionalBlockOptionsLable", "Funcional Blocks Options");

            var checkboxPullFromAssembler = CreateCheckbox(
                "CheckboxPullFromAssembler",
                "Pull from assemblers.",
                isWorkingAndEnabled,
                (block) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                    {
                        return system.Settings.GetPullFromAssembler();
                    }
                    return false;
                },
                (block, value) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                    {
                        system.Settings.SetPullFromAssembler(value);
                        system.SendToServer("PullFromAssembler", "SET", value.ToString());
                    }
                },
                tooltip: "If enabled will pull itens from assemblers result inventory and not used from queue inventory.",
                supMultiple: true
            );
            CreateCheckBoxAction("PullFromAssembler", checkboxPullFromAssembler);

            var checkboxPullFromRefinery = CreateCheckbox(
                "CheckboxPullFromRefinery",
                "Pull from refineries.",
                isWorkingAndEnabled,
                (block) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                    {
                        return system.Settings.GetPullFromRefinary();
                    }
                    return false;
                },
                (block, value) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                    {
                        system.Settings.SetPullFromRefinary(value);
                        system.SendToServer("PullFromRefinary", "SET", value.ToString());
                    }
                },
                tooltip: "If enabled will pull itens from refineries result inventory.",
                supMultiple: true
            );
            CreateCheckBoxAction("PullFromRefinery", checkboxPullFromRefinery);

            var checkboxPullFromReactor = CreateCheckbox(
                "CheckboxPullFromReactor",
                "Pull from reactors/engines.",
                isWorkingAndEnabled,
                (block) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                    {
                        return system.Settings.GetPullFromReactor();
                    }
                    return false;
                },
                (block, value) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                    {
                        system.Settings.SetPullFromReactor(value);
                        system.SendToServer("PullFromReactor", "SET", value.ToString());
                        UpdateVisual(block);
                    }
                },
                tooltip: "If enabled will pull not fuel itens from reactors or engines.",
                supMultiple: true
            );
            CreateCheckBoxAction("PullFromReactor", checkboxPullFromReactor);

            var checkboxFillReactor = CreateCheckbox(
                "CheckboxFillReactor",
                "Fill reactors/engines with fuel.",
                (block) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                        return isWorkingAndEnabled.Invoke(block) && system.Settings.GetPullFromReactor();
                    return false;
                },
                (block) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                    {
                        return system.Settings.GetFillReactor();
                    }
                    return false;
                },
                (block, value) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                    {
                        system.Settings.SetFillReactor(value);
                        system.SendToServer("FillReactor", "SET", value.ToString());
                        UpdateVisual(block);
                    }
                },
                tooltip: "If enabled will fill reactors or engines with fuel.",
                supMultiple: true
            );
            CreateCheckBoxAction("FillReactor", checkboxFillReactor);

            var sliderFillSmallReactor = CreateSlider(
                "SliderFillSmallReactor",
                "Small reactors/engines fuel",
                (block) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                        return isWorkingAndEnabled.Invoke(block) && system.Settings.GetPullFromReactor() && system.Settings.GetFillReactor();
                    return false;
                },
                (block) =>
                {
                    var system = GetSystem(block);
                    return system != null ? system.Settings.GetSmallReactorFuelAmount() : 0;
                },
                (block, value) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                    {
                        system.Settings.SetSmallReactorFuelAmount(value);
                        system.SendToServer("SmallReactorFuelAmount", "SET", value.ToString());
                    }
                },
                (block, val) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                    {
                        val.Append(Math.Round(system.Settings.GetSmallReactorFuelAmount(), 2, MidpointRounding.AwayFromZero));
                    }
                },
                new VRageMath.Vector2(1, 25),
                tooltip: "Set the base amount to fill the small reactors/engines, the value will be multiply by the size of the block.",
                supMultiple: true
            );
            CreateSliderActions("FillSmallReactor", sliderFillSmallReactor);

            var sliderFillLargeReactor = CreateSlider(
                "SliderFillLargeReactor",
                "Large reactors/engines fuel",
                (block) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                        return isWorkingAndEnabled.Invoke(block) && system.Settings.GetPullFromReactor() && system.Settings.GetFillReactor();
                    return false;
                },
                (block) =>
                {
                    var system = GetSystem(block);
                    return system != null ? system.Settings.GetLargeReactorFuelAmount() : 0;
                },
                (block, value) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                    {
                        system.Settings.SetLargeReactorFuelAmount(value);
                        system.SendToServer("LargeReactorFuelAmount", "SET", value.ToString());
                    }
                },
                (block, val) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                    {
                        val.Append(Math.Round(system.Settings.GetLargeReactorFuelAmount(), 2, MidpointRounding.AwayFromZero));
                    }
                },
                new VRageMath.Vector2(10, 250),
                tooltip: "Set the base amount to fill the large reactors/engines, the value will be multiply by the size of the block.",
                supMultiple: true
            );
            CreateSliderActions("FillLargeReactor", sliderFillLargeReactor);

            var checkboxPullFromGasGenerator = CreateCheckbox(
                "CheckboxPullFromGasGenerator",
                "Pull from Gas Generators.",
                isWorkingAndEnabled,
                (block) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                    {
                        return system.Settings.GetPullFromGasGenerator();
                    }
                    return false;
                },
                (block, value) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                    {
                        system.Settings.SetPullFromGasGenerator(value);
                        system.SendToServer("PullFromGasGenerator", "SET", value.ToString());
                        UpdateVisual(block);
                    }
                },
                tooltip: "If enabled will pull not ice from Gas Generators.",
                supMultiple: true
            );
            CreateCheckBoxAction("PullFromGasGenerator", checkboxPullFromGasGenerator);

            var checkboxFillGasGenerator = CreateCheckbox(
                "CheckboxFillGasGenerator",
                "Fill Gas Generators with ice.",
                (block) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                        return isWorkingAndEnabled.Invoke(block) && system.Settings.GetPullFromGasGenerator();
                    return false;
                },
                (block) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                    {
                        return system.Settings.GetFillGasGenerator();
                    }
                    return false;
                },
                (block, value) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                    {
                        system.Settings.SetFillGasGenerator(value);
                        system.SendToServer("FillGasGenerator", "SET", value.ToString());
                        UpdateVisual(block);
                    }
                },
                tooltip: "If enabled will fill Gas Generators with ice.",
                supMultiple: true
            );
            CreateCheckBoxAction("FillGasGenerator", checkboxFillGasGenerator);

            var sliderFillSmallGasGenerator = CreateSlider(
                "SliderFillSmallGasGenerator",
                "Small Gas Generators ice",
                (block) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                        return isWorkingAndEnabled.Invoke(block) && system.Settings.GetPullFromGasGenerator() && system.Settings.GetFillGasGenerator();
                    return false;
                },
                (block) =>
                {
                    var system = GetSystem(block);
                    return system != null ? system.Settings.GetSmallGasGeneratorAmount() : 0;
                },
                (block, value) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                    {
                        system.Settings.SetSmallGasGeneratorAmount(value);
                        system.SendToServer("SmallGasGeneratorAmount", "SET", value.ToString());
                    }
                },
                (block, val) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                    {
                        val.Append(Math.Round(system.Settings.GetSmallGasGeneratorAmount(), 2, MidpointRounding.AwayFromZero));
                    }
                },
                new VRageMath.Vector2(16, 64),
                tooltip: "Set the base amount to fill the small Gas Generators, the value will be multiply by the size of the block.",
                supMultiple: true
            );
            CreateSliderActions("FillSmallGasGenerator", sliderFillSmallGasGenerator);

            var sliderFillLargeGasGenerator = CreateSlider(
                "SliderFillLargeGasGenerator",
                "Large Gas Generators ice",
                (block) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                        return isWorkingAndEnabled.Invoke(block) && system.Settings.GetPullFromGasGenerator() && system.Settings.GetFillGasGenerator();
                    return false;
                },
                (block) =>
                {
                    var system = GetSystem(block);
                    return system != null ? system.Settings.GetLargeGasGeneratorAmount() : 0;
                },
                (block, value) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                    {
                        system.Settings.SetLargeGasGeneratorAmount(value);
                        system.SendToServer("LargeGasGeneratorAmount", "SET", value.ToString());
                    }
                },
                (block, val) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                    {
                        val.Append(Math.Round(system.Settings.GetLargeGasGeneratorAmount(), 2, MidpointRounding.AwayFromZero));
                    }
                },
                new VRageMath.Vector2(100, 2000),
                tooltip: "Set the base amount to fill the large Gas Generators, the value will be multiply by the size of the block.",
                supMultiple: true
            );
            CreateSliderActions("FillLargeGasGenerator", sliderFillLargeGasGenerator);

            var checkboxPullFromGasTank = CreateCheckbox(
                "CheckboxPullFromGasTank",
                "Pull from Gas Tanks.",
                isWorkingAndEnabled,
                (block) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                    {
                        return system.Settings.GetPullFromGasTank();
                    }
                    return false;
                },
                (block, value) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                    {
                        system.Settings.SetPullFromGasTank(value);
                        system.SendToServer("PullFromGasTank", "SET", value.ToString());
                        UpdateVisual(block);
                    }
                },
                tooltip: "If enabled will pull from Gas Tanks.",
                supMultiple: true
            );
            CreateCheckBoxAction("PullFromGasTank", checkboxPullFromGasTank);

            var checkboxFillBottles = CreateCheckbox(
                "CheckboxFillBottles",
                "Fill bottles with gas.",
                (block) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                        return isWorkingAndEnabled.Invoke(block) && (system.Settings.GetPullFromGasGenerator() || system.Settings.GetPullFromGasTank());
                    return false;
                },
                (block) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                    {
                        return system.Settings.GetFillBottles();
                    }
                    return false;
                },
                (block, value) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                    {
                        system.Settings.SetFillBottles(value);
                        system.SendToServer("FillBottles", "SET", value.ToString());
                    }
                },
                tooltip: "If enabled will try to fill bottles in tanks or generators.",
                supMultiple: true
            );

            if (AILogisticsAutomationSession.IsUsingStatsAndEffects())
            {

                CreateTerminalLabel("LabeESStats", "Stats & Effects Blocks");

                var checkboxPullFromComposter = CreateCheckbox(
                    "CheckboxPullFromComposter",
                    "Pull from Composter.",
                    isWorkingAndEnabled,
                    (block) =>
                    {
                        var system = GetSystem(block);
                        if (system != null)
                        {
                            return system.Settings.GetPullFromComposter();
                        }
                        return false;
                    },
                    (block, value) =>
                    {
                        var system = GetSystem(block);
                        if (system != null)
                        {
                            system.Settings.SetPullFromComposter(value);
                            system.SendToServer("PullFromComposter", "SET", value.ToString());
                            UpdateVisual(block);
                        }
                    },
                    tooltip: "If enabled will pull not organic from Composter.",
                    supMultiple: true
                );
                CreateCheckBoxAction("PullFromComposter", checkboxPullFromComposter);

                var checkboxFillComposter = CreateCheckbox(
                    "CheckboxFillComposter",
                    "Fill Composter with organic.",
                    (block) =>
                    {
                        var system = GetSystem(block);
                        if (system != null)
                            return isWorkingAndEnabled.Invoke(block) && system.Settings.GetPullFromComposter();
                        return false;
                    },
                    (block) =>
                    {
                        var system = GetSystem(block);
                        if (system != null)
                        {
                            return system.Settings.GetFillComposter();
                        }
                        return false;
                    },
                    (block, value) =>
                    {
                        var system = GetSystem(block);
                        if (system != null)
                        {
                            system.Settings.SetFillComposter(value);
                            system.SendToServer("FillComposter", "SET", value.ToString());
                            UpdateVisual(block);
                        }
                    },
                    tooltip: "If enabled will fill Composter with organic.",
                    supMultiple: true
                );
                CreateCheckBoxAction("FillComposter", checkboxFillComposter);

                var checkboxPullFromFishTrap = CreateCheckbox(
                    "CheckboxPullFromFishTrap",
                    "Pull from Fish Trap.",
                    isWorkingAndEnabled,
                    (block) =>
                    {
                        var system = GetSystem(block);
                        if (system != null)
                        {
                            return system.Settings.GetPullFishTrap();
                        }
                        return false;
                    },
                    (block, value) =>
                    {
                        var system = GetSystem(block);
                        if (system != null)
                        {
                            system.Settings.SetPullFishTrap(value);
                            system.SendToServer("PullFishTrap", "SET", value.ToString());
                            UpdateVisual(block);
                        }
                    },
                    tooltip: "If enabled will pull not baits from FishTrap.",
                    supMultiple: true
                );
                CreateCheckBoxAction("PullFromFishTrap", checkboxPullFromFishTrap);

                var checkboxFillFishTrap = CreateCheckbox(
                    "CheckboxFillFishTrap",
                    "Fill FishTrap with organic.",
                    (block) =>
                    {
                        var system = GetSystem(block);
                        if (system != null)
                            return isWorkingAndEnabled.Invoke(block) && system.Settings.GetPullFishTrap();
                        return false;
                    },
                    (block) =>
                    {
                        var system = GetSystem(block);
                        if (system != null)
                        {
                            return system.Settings.GetFillFishTrap();
                        }
                        return false;
                    },
                    (block, value) =>
                    {
                        var system = GetSystem(block);
                        if (system != null)
                        {
                            system.Settings.SetFillFishTrap(value);
                            system.SendToServer("FillFishTrap", "SET", value.ToString());
                            UpdateVisual(block);
                        }
                    },
                    tooltip: "If enabled will fill Fish Traps with baits.",
                    supMultiple: true
                );
                CreateCheckBoxAction("FillFishTrap", checkboxFillFishTrap);

                var checkboxPullFromRefrigerator = CreateCheckbox(
                    "CheckboxPullFromRefrigerator",
                    "Pull from Refrigerator.",
                    isWorkingAndEnabled,
                    (block) =>
                    {
                        var system = GetSystem(block);
                        if (system != null)
                        {
                            return system.Settings.GetPullRefrigerator();
                        }
                        return false;
                    },
                    (block, value) =>
                    {
                        var system = GetSystem(block);
                        if (system != null)
                        {
                            system.Settings.SetPullRefrigerator(value);
                            system.SendToServer("PullRefrigerator", "SET", value.ToString());
                            UpdateVisual(block);
                        }
                    },
                    tooltip: "If enabled will pull not foods from Refrigerator.",
                    supMultiple: true
                );
                CreateCheckBoxAction("PullFromRefrigerator", checkboxPullFromRefrigerator);

                var checkboxFillRefrigerator = CreateCheckbox(
                    "CheckboxFillRefrigerator",
                    "Fill Refrigerator with food.",
                    (block) =>
                    {
                        var system = GetSystem(block);
                        if (system != null)
                            return isWorkingAndEnabled.Invoke(block) && system.Settings.GetPullRefrigerator();
                        return false;
                    },
                    (block) =>
                    {
                        var system = GetSystem(block);
                        if (system != null)
                        {
                            return system.Settings.GetFillRefrigerator();
                        }
                        return false;
                    },
                    (block, value) =>
                    {
                        var system = GetSystem(block);
                        if (system != null)
                        {
                            system.Settings.SetFillRefrigerator(value);
                            system.SendToServer("FillRefrigerator", "SET", value.ToString());
                            UpdateVisual(block);
                        }
                    },
                    tooltip: "If enabled will fill Refrigerator with foods.",
                    supMultiple: true
                );
                CreateCheckBoxAction("FillRefrigerator", checkboxFillRefrigerator);

            }

        }

        protected override string GetActionPrefix()
        {
            return "AIInventoryManager";
        }

    }

}