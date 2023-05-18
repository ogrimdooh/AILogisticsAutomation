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

            var labelStartConfig = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlLabel, IMyOreDetector>("StartConfig");
            labelStartConfig.Label = MyStringId.GetOrCompute("AI Configuration");
            CustomControls.Add(labelStartConfig);

            var checkboxEnabled = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCheckbox, IMyOreDetector>("CheckboxEnabled");
            checkboxEnabled.Title = MyStringId.GetOrCompute("Enabled");
            checkboxEnabled.Tooltip = MyStringId.GetOrCompute("Set if the block will work or not.");
            checkboxEnabled.OnText = MyStringId.GetOrCompute("Yes");
            checkboxEnabled.OffText = MyStringId.GetOrCompute("No");
            checkboxEnabled.Enabled = isWorking;
            checkboxEnabled.Getter = (block) =>
            {
                var system = GetSystem(block);
                if (system != null)
                {
                    return system.Settings.GetEnabled();
                }
                return false;
            };
            checkboxEnabled.Setter = (block, value) =>
            {
                var system = GetSystem(block);
                if (system != null)
                {
                    system.Settings.SetEnabled(value);
                    system.SendToServer("Enabled", "SET", value.ToString());
                    UpdateVisual(block);
                }
            };
            checkboxEnabled.SupportsMultipleBlocks = true;
            CreateCheckBoxAction("Enabled", checkboxEnabled);
            CustomControls.Add(checkboxEnabled);

            var labelCargoDefs = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlLabel, IMyOreDetector>("CargosDefinition");
            labelCargoDefs.Label = MyStringId.GetOrCompute("Cargos Definition");
            CustomControls.Add(labelCargoDefs);
            {
                /* Simple Options */
                var checkboxPullFromConnectedGrids = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCheckbox, IMyOreDetector>("CheckboxPullFromConnectedGrids");
                checkboxPullFromConnectedGrids.Title = MyStringId.GetOrCompute("Pull from connected grids.");
                checkboxPullFromConnectedGrids.Tooltip = MyStringId.GetOrCompute("If enabled will pull itens from connected grids, that is not using ignored connectors.");
                checkboxPullFromConnectedGrids.OnText = MyStringId.GetOrCompute("Yes");
                checkboxPullFromConnectedGrids.OffText = MyStringId.GetOrCompute("No");
                checkboxPullFromConnectedGrids.Enabled = isWorkingAndEnabled;
                checkboxPullFromConnectedGrids.Getter = (block) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                    {
                        return system.Settings.GetPullFromConnectedGrids();
                    }
                    return false;
                };
                checkboxPullFromConnectedGrids.Setter = (block, value) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                    {
                        system.Settings.SetPullFromConnectedGrids(value);
                        system.SendToServer("PullFromConnectedGrids", "SET", value.ToString());
                    }
                };
                checkboxPullFromConnectedGrids.SupportsMultipleBlocks = true;
                CreateCheckBoxAction("PullFromConnectedGrids", checkboxPullFromConnectedGrids);
                CustomControls.Add(checkboxPullFromConnectedGrids);

                var checkboxPullFromSubGrids = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCheckbox, IMyOreDetector>("CheckboxPullFromSubGrids");
                checkboxPullFromSubGrids.Title = MyStringId.GetOrCompute("Pull from sub-grids.");
                checkboxPullFromSubGrids.Tooltip = MyStringId.GetOrCompute("If enabled will pull itens from attached sub-grids.");
                checkboxPullFromSubGrids.OnText = MyStringId.GetOrCompute("Yes");
                checkboxPullFromSubGrids.OffText = MyStringId.GetOrCompute("No");
                checkboxPullFromSubGrids.Enabled = isWorkingAndEnabled;
                checkboxPullFromSubGrids.Getter = (block) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                    {
                        return system.Settings.GetPullSubGrids();
                    }
                    return false;
                };
                checkboxPullFromSubGrids.Setter = (block, value) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                    {
                        system.Settings.SetPullSubGrids(value);
                        system.SendToServer("PullFromSubGrids", "SET", value.ToString());
                    }
                };
                checkboxPullFromSubGrids.SupportsMultipleBlocks = true;
                CreateCheckBoxAction("PullFromSubGrids", checkboxPullFromSubGrids);
                CustomControls.Add(checkboxPullFromSubGrids);

                /* Sort Type */
                var comboBoxSortItensType = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCombobox, IMyOreDetector>("FilterType");
                comboBoxSortItensType.Title = MyStringId.GetOrCompute("Sorter Type");
                comboBoxSortItensType.Tooltip = MyStringId.GetOrCompute("Select a sorter type to do with the itens.");
                comboBoxSortItensType.Enabled = isWorkingAndEnabled;
                comboBoxSortItensType.ComboBoxContent = (list) =>
                {
                    list.Add(new MyTerminalControlComboBoxItem() { Key = 0, Value = MyStringId.GetOrCompute("None") });
                    list.Add(new MyTerminalControlComboBoxItem() { Key = 1, Value = MyStringId.GetOrCompute("Name") });
                    list.Add(new MyTerminalControlComboBoxItem() { Key = 2, Value = MyStringId.GetOrCompute("Mass") });
                    list.Add(new MyTerminalControlComboBoxItem() { Key = 3, Value = MyStringId.GetOrCompute("Type Name [Item Name]") });
                    list.Add(new MyTerminalControlComboBoxItem() { Key = 4, Value = MyStringId.GetOrCompute("Type Name [Item Mass]") });
                };
                comboBoxSortItensType.Getter = (block) =>
                {
                    var system = GetSystem(block);
                    if (system == null) return 0;
                    else return system.Settings.GetSortItensType();
                };
                comboBoxSortItensType.Setter = (block, value) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                    {
                        system.Settings.SetSortItensType(value);
                        system.SendToServer("SortItensType", "SET", value.ToString());
                    }
                };
                comboBoxSortItensType.SupportsMultipleBlocks = true;
                CreateComboBoxAction("SortItensType", comboBoxSortItensType);
                CustomControls.Add(comboBoxSortItensType);

                /* stackIfPossible */

                var checkboxStackIfPossible = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCheckbox, IMyOreDetector>("CheckboxStackIfPossible");
                checkboxStackIfPossible.Title = MyStringId.GetOrCompute("Stack Items.");
                checkboxStackIfPossible.Tooltip = MyStringId.GetOrCompute("If enabled will stack itens slots if possible.");
                checkboxStackIfPossible.OnText = MyStringId.GetOrCompute("Yes");
                checkboxStackIfPossible.OffText = MyStringId.GetOrCompute("No");
                checkboxStackIfPossible.Enabled = isWorkingAndEnabled;
                checkboxStackIfPossible.Getter = (block) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                    {
                        return system.Settings.GetStackIfPossible();
                    }
                    return false;
                };
                checkboxStackIfPossible.Setter = (block, value) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                    {
                        system.Settings.SetStackIfPossible(value);
                        system.SendToServer("StackIfPossible", "SET", value.ToString());
                    }
                };
                checkboxStackIfPossible.SupportsMultipleBlocks = true;
                CreateCheckBoxAction("StackIfPossible", checkboxStackIfPossible);
                CustomControls.Add(checkboxStackIfPossible);

                /* Cargo Container List */
                var listCargoContainers = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlListbox, IMyOreDetector>("ListCargoContainers");
                listCargoContainers.Title = MyStringId.GetOrCompute("Cargo Container List");
                listCargoContainers.Tooltip = MyStringId.GetOrCompute("Select a cargo container to set pull settings.");
                listCargoContainers.Enabled = isWorkingAndEnabled;
                listCargoContainers.ListContent = (block, list, selectedList) =>
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
                };
                listCargoContainers.ItemSelected = (block, selectedList) =>
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
                };
                listCargoContainers.VisibleRowsCount = 5;
                listCargoContainers.SupportsMultipleBlocks = false;
                CustomControls.Add(listCargoContainers);

                /* Checkbox Cargo Container */
                var checkboxAddContainer = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCheckbox, IMyOreDetector>("CheckboxAddContainer");
                checkboxAddContainer.Title = MyStringId.GetOrCompute("Added selected cargo to pull list.");
                checkboxAddContainer.Tooltip = MyStringId.GetOrCompute("Cargos added to list will be used as store to all containers in grid.");
                checkboxAddContainer.OnText = MyStringId.GetOrCompute("Yes");
                checkboxAddContainer.OffText = MyStringId.GetOrCompute("No");
                checkboxAddContainer.Enabled = isWorkingAndCargoSelected;
                checkboxAddContainer.Getter = (block) =>
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
                };
                checkboxAddContainer.Setter = (block, value) =>
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
                };
                checkboxAddContainer.SupportsMultipleBlocks = false;
                CustomControls.Add(checkboxAddContainer);

                var cargoOptionsSeparator = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSeparator, IMyOreDetector>("CargoOptionsSeparator");
                CustomControls.Add(cargoOptionsSeparator);

                var cargoOptionsSeparatorLable = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlLabel, IMyOreDetector>("CargoOptionsSeparatorLable");
                cargoOptionsSeparatorLable.Label = MyStringId.GetOrCompute("Selected Cargo Filter");
                CustomControls.Add(cargoOptionsSeparatorLable);

                /* Filter Type */
                var comboBoxFilterType = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCombobox, IMyOreDetector>("FilterType");
                comboBoxFilterType.Title = MyStringId.GetOrCompute("Filter Type");
                comboBoxFilterType.Tooltip = MyStringId.GetOrCompute("Select a filter type.");
                comboBoxFilterType.Enabled = isWorkingAndCargoSelectedIsAdded;
                comboBoxFilterType.ComboBoxContent = (list) =>
                {
                    list.Add(new MyTerminalControlComboBoxItem() { Key = 0, Value = MyStringId.GetOrCompute("Pull") });
                    list.Add(new MyTerminalControlComboBoxItem() { Key = 1, Value = MyStringId.GetOrCompute("Ignore") });
                };
                comboBoxFilterType.Getter = (block) =>
                {
                    var system = GetSystem(block);
                    if (system == null) return 0;
                    else return selectedFilterType;
                };
                comboBoxFilterType.Setter = (block, value) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                    {
                        selectedFilterType = value;
                    }
                };
                comboBoxFilterType.SupportsMultipleBlocks = false;
                CustomControls.Add(comboBoxFilterType);

                /* Filter Type */
                var comboBoxFilterGroup = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCombobox, IMyOreDetector>("FilterGroup");
                comboBoxFilterGroup.Title = MyStringId.GetOrCompute("Filter Group");
                comboBoxFilterGroup.Tooltip = MyStringId.GetOrCompute("Select a filter group.");
                comboBoxFilterGroup.Enabled = isWorkingAndCargoSelectedIsAdded;
                comboBoxFilterGroup.ComboBoxContent = (list) =>
                {
                    list.Add(new MyTerminalControlComboBoxItem() { Key = 0, Value = MyStringId.GetOrCompute("Item Id") });
                    list.Add(new MyTerminalControlComboBoxItem() { Key = 1, Value = MyStringId.GetOrCompute("Item Type") });
                };
                comboBoxFilterGroup.Getter = (block) =>
                {
                    var system = GetSystem(block);
                    if (system == null) return 0;
                    else return selectedFilterGroup;
                };
                comboBoxFilterGroup.Setter = (block, value) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                    {
                        selectedFilterGroup = value;
                        UpdateVisual(block);
                    }
                };
                comboBoxFilterGroup.SupportsMultipleBlocks = false;
                CustomControls.Add(comboBoxFilterGroup);

                /* Filter IdType */
                var comboBoxFilterIdType = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCombobox, IMyOreDetector>("FilterItemType");
                comboBoxFilterIdType.Title = MyStringId.GetOrCompute("Filter Type");
                comboBoxFilterIdType.Tooltip = MyStringId.GetOrCompute("Select a filter item Type.");
                comboBoxFilterIdType.Enabled = isWorkingAndCargoSelectedIsAdded;
                comboBoxFilterIdType.ComboBoxContent = (list) =>
                {
                    list.AddRange(validTypesUI);
                };
                comboBoxFilterIdType.Getter = (block) =>
                {
                    var system = GetSystem(block);
                    if (system == null) return 0;
                    else return selectedFilterItemType;
                };
                comboBoxFilterIdType.Setter = (block, value) =>
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
                };
                comboBoxFilterIdType.SupportsMultipleBlocks = false;
                CustomControls.Add(comboBoxFilterIdType);

                /* Filter Id */
                var comboBoxFilterId = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCombobox, IMyOreDetector>("FilterItemId");
                comboBoxFilterId.Title = MyStringId.GetOrCompute("Filter Id");
                comboBoxFilterId.Tooltip = MyStringId.GetOrCompute("Select a filter item Id.");
                comboBoxFilterId.Enabled = (block) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                        return isWorkingAndCargoSelectedIsAdded.Invoke(block) && selectedFilterGroup == 0 && selectedFilterItemType >= 0;
                    return false;
                };
                comboBoxFilterId.ComboBoxContent = (list) =>
                {
                    var typeToUse = validTypes[selectedFilterItemType];
                    if (validIdsByTypeUI.ContainsKey(typeToUse))
                        list.AddRange(validIdsByTypeUI[typeToUse]);
                };
                comboBoxFilterId.Getter = (block) =>
                {
                    var system = GetSystem(block);
                    if (system == null) return 0;
                    else return selectedFilterItemId;
                };
                comboBoxFilterId.Setter = (block, value) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                    {
                        selectedFilterItemId = (int)value;
                    }
                };
                comboBoxFilterId.SupportsMultipleBlocks = false;
                CustomControls.Add(comboBoxFilterId);

                /* Button Add Filter */
                var button = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlButton, IMyOreDetector>("AddedSelectedFilter");
                button.Title = MyStringId.GetOrCompute("Added Selected Filter");
                button.Enabled = isWorkingAndCargoSelectedIsAdded;
                button.Action = (block) =>
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
                };
                button.SupportsMultipleBlocks = false;
                CustomControls.Add(button);

                /* Filter List */
                var listCargoContainerFilters = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlListbox, IMyOreDetector>("ListCargoContainerFilters");
                listCargoContainerFilters.Title = MyStringId.GetOrCompute("Cargo Container Filters List");
                listCargoContainerFilters.Tooltip = MyStringId.GetOrCompute("Select a a filter to remove.");
                listCargoContainerFilters.Enabled = isWorkingAndCargoSelectedIsAdded;
                listCargoContainerFilters.ListContent = (block, list, selectedList) =>
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
                };
                listCargoContainerFilters.ItemSelected = (block, selectedList) =>
                {
                    if (selectedList.Count == 0)
                        return;

                    var system = GetSystem(block);
                    if (system != null)
                    {
                        system.Settings.SelectedAddedFilterId = selectedList[0].UserData.ToString();
                        UpdateVisual(block);
                    }
                };
                listCargoContainerFilters.VisibleRowsCount = 5;
                listCargoContainerFilters.SupportsMultipleBlocks = false;
                CustomControls.Add(listCargoContainerFilters);

                /* Button Remove Filter */
                var buttonRemoveFilter = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlButton, IMyOreDetector>("AddedSelectedFilter");
                buttonRemoveFilter.Title = MyStringId.GetOrCompute("Added Selected Filter");
                buttonRemoveFilter.Enabled = isWorkingAndCargoSelectedIsAddedAndFilterIsSelected;
                buttonRemoveFilter.Action = (block) =>
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
                };
                buttonRemoveFilter.SupportsMultipleBlocks = false;
                CustomControls.Add(buttonRemoveFilter);

                var ignoreBlocksSeparator = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSeparator, IMyOreDetector>("IgnoreBlocksSeparator");
                CustomControls.Add(ignoreBlocksSeparator);

                var ignoreBlocksSeparatorLable = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlLabel, IMyOreDetector>("IgnoreBlocksSeparatorLable");
                ignoreBlocksSeparatorLable.Label = MyStringId.GetOrCompute("Selected the Ignored Blocks");
                CustomControls.Add(ignoreBlocksSeparatorLable);

                /* Filter Block Type */
                var comboBoxFilterBlockType = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCombobox, IMyOreDetector>("FilterBlockType");
                comboBoxFilterBlockType.Title = MyStringId.GetOrCompute("Filter Block Type");
                comboBoxFilterBlockType.Tooltip = MyStringId.GetOrCompute("Select a filter to the block type.");
                comboBoxFilterBlockType.Enabled = isWorkingAndEnabled;
                comboBoxFilterBlockType.ComboBoxContent = (list) =>
                {
                    list.Add(new MyTerminalControlComboBoxItem() { Key = 0, Value = MyStringId.GetOrCompute("Cargo Container") });
                    list.Add(new MyTerminalControlComboBoxItem() { Key = 1, Value = MyStringId.GetOrCompute("Functional Blocks") });
                    list.Add(new MyTerminalControlComboBoxItem() { Key = 2, Value = MyStringId.GetOrCompute("Connector") });
                };
                comboBoxFilterBlockType.Getter = (block) =>
                {
                    var system = GetSystem(block);
                    if (system == null) return 0;
                    else return selectedFilterBlockType;
                };
                comboBoxFilterBlockType.Setter = (block, value) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                    {
                        selectedFilterBlockType = value;
                        UpdateVisual(block);
                    }
                };
                comboBoxFilterBlockType.SupportsMultipleBlocks = false;
                CustomControls.Add(comboBoxFilterBlockType);

                /* Block type List */
                var listBlocksType = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlListbox, IMyOreDetector>("ListBlocksType");
                listBlocksType.Title = MyStringId.GetOrCompute("Blocks of selected type");
                listBlocksType.Tooltip = MyStringId.GetOrCompute("Select one or more blocks to be ignored by the AI Block.");
                listBlocksType.Enabled = isWorkingAndEnabled;
                listBlocksType.ListContent = (block, list, selectedList) =>
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
                };
                listBlocksType.ItemSelected = (block, selectedList) =>
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
                };
                listBlocksType.VisibleRowsCount = 5;
                listBlocksType.SupportsMultipleBlocks = false;
                CustomControls.Add(listBlocksType);

                /* Button Add Ignored */
                var buttonAddIgnored = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlButton, IMyOreDetector>("ButtonAddIgnored");
                buttonAddIgnored.Title = MyStringId.GetOrCompute("Add Selected To Ignored");
                buttonAddIgnored.Enabled = (block) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                    {
                        return isWorkingAndEnabled.Invoke(block) && system.Settings.SelectedIgnoreEntityId != 0;
                    }
                    return false;
                };
                buttonAddIgnored.Action = (block) =>
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
                };
                buttonAddIgnored.SupportsMultipleBlocks = false;
                CustomControls.Add(buttonAddIgnored);

                /* Block type List */
                var listBlocksIgnored = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlListbox, IMyOreDetector>("ListBlocksIgnored");
                listBlocksIgnored.Title = MyStringId.GetOrCompute("Ignored Blocks");
                listBlocksIgnored.Tooltip = MyStringId.GetOrCompute("Select one or more blocks to be ignored by the AI Block.");
                listBlocksIgnored.Enabled = isWorkingAndEnabled;
                listBlocksIgnored.Multiselect = true;
                listBlocksIgnored.ListContent = (block, list, selectedList) =>
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
                };
                listBlocksIgnored.ItemSelected = (block, selectedList) =>
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
                };
                listBlocksIgnored.VisibleRowsCount = 5;
                listBlocksIgnored.SupportsMultipleBlocks = false;
                CustomControls.Add(listBlocksIgnored);

                /* Button Remove Ignored */
                var buttonRemoveIgnored = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlButton, IMyOreDetector>("ButtonRemoveIgnored");
                buttonRemoveIgnored.Title = MyStringId.GetOrCompute("Remove Selected Ignored Block");
                buttonRemoveIgnored.Enabled = (block) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                    {
                        return isWorkingAndEnabled.Invoke(block) && system.Settings.SelectedAddedIgnoreEntityId != 0;
                    }
                    return false;
                };
                buttonRemoveIgnored.Action = (block) =>
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
                };
                buttonRemoveIgnored.SupportsMultipleBlocks = false;
                CustomControls.Add(buttonRemoveIgnored);

                var funcionalBlockOptionsSeparator = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSeparator, IMyOreDetector>("FuncionalBlockOptionsSeparator");
                CustomControls.Add(funcionalBlockOptionsSeparator);

                var funcionalBlockOptionsLable = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlLabel, IMyOreDetector>("FuncionalBlockOptionsLable");
                funcionalBlockOptionsLable.Label = MyStringId.GetOrCompute("Funcional Blocks Options");
                CustomControls.Add(funcionalBlockOptionsLable);

                var checkboxPullFromAssembler = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCheckbox, IMyOreDetector>("CheckboxPullFromAssembler");
                checkboxPullFromAssembler.Title = MyStringId.GetOrCompute("Pull from assemblers.");
                checkboxPullFromAssembler.Tooltip = MyStringId.GetOrCompute("If enabled will pull itens from assemblers result inventory and not used from queue inventory.");
                checkboxPullFromAssembler.OnText = MyStringId.GetOrCompute("Yes");
                checkboxPullFromAssembler.OffText = MyStringId.GetOrCompute("No");
                checkboxPullFromAssembler.Enabled = isWorkingAndEnabled;
                checkboxPullFromAssembler.Getter = (block) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                    {
                        return system.Settings.GetPullFromAssembler();
                    }
                    return false;
                };
                checkboxPullFromAssembler.Setter = (block, value) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                    {
                        system.Settings.SetPullFromAssembler(value);
                        system.SendToServer("PullFromAssembler", "SET", value.ToString());
                    }
                };
                checkboxPullFromAssembler.SupportsMultipleBlocks = true;
                CreateCheckBoxAction("PullFromAssembler", checkboxPullFromAssembler);
                CustomControls.Add(checkboxPullFromAssembler);

                var checkboxPullFromRefinery = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCheckbox, IMyOreDetector>("CheckboxPullFromRefinery");
                checkboxPullFromRefinery.Title = MyStringId.GetOrCompute("Pull from refineries.");
                checkboxPullFromRefinery.Tooltip = MyStringId.GetOrCompute("If enabled will pull itens from refineries result inventory.");
                checkboxPullFromRefinery.OnText = MyStringId.GetOrCompute("Yes");
                checkboxPullFromRefinery.OffText = MyStringId.GetOrCompute("No");
                checkboxPullFromRefinery.Enabled = isWorkingAndEnabled;
                checkboxPullFromRefinery.Getter = (block) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                    {
                        return system.Settings.GetPullFromRefinary();
                    }
                    return false;
                };
                checkboxPullFromRefinery.Setter = (block, value) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                    {
                        system.Settings.SetPullFromRefinary(value);
                        system.SendToServer("PullFromRefinary", "SET", value.ToString());
                    }
                };
                checkboxPullFromRefinery.SupportsMultipleBlocks = true;
                CreateCheckBoxAction("PullFromRefinery", checkboxPullFromRefinery);
                CustomControls.Add(checkboxPullFromRefinery);

                var checkboxPullFromReactor = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCheckbox, IMyOreDetector>("CheckboxPullFromReactor");
                checkboxPullFromReactor.Title = MyStringId.GetOrCompute("Pull from reactors/engines.");
                checkboxPullFromReactor.Tooltip = MyStringId.GetOrCompute("If enabled will pull not fuel itens from reactors or engines.");
                checkboxPullFromReactor.OnText = MyStringId.GetOrCompute("Yes");
                checkboxPullFromReactor.OffText = MyStringId.GetOrCompute("No");
                checkboxPullFromReactor.Enabled = isWorkingAndEnabled;
                checkboxPullFromReactor.Getter = (block) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                    {
                        return system.Settings.GetPullFromReactor();
                    }
                    return false;
                };
                checkboxPullFromReactor.Setter = (block, value) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                    {
                        system.Settings.SetPullFromReactor(value);
                        system.SendToServer("PullFromReactor", "SET", value.ToString());
                        UpdateVisual(block);
                    }
                };
                checkboxPullFromReactor.SupportsMultipleBlocks = true;
                CreateCheckBoxAction("PullFromReactor", checkboxPullFromReactor);
                CustomControls.Add(checkboxPullFromReactor);

                var checkboxFillReactor = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCheckbox, IMyOreDetector>("CheckboxFillReactor");
                checkboxFillReactor.Title = MyStringId.GetOrCompute("Fill reactors/engines with fuel.");
                checkboxFillReactor.Tooltip = MyStringId.GetOrCompute("If enabled will fill reactors or engines with fuel.");
                checkboxFillReactor.OnText = MyStringId.GetOrCompute("Yes");
                checkboxFillReactor.OffText = MyStringId.GetOrCompute("No");
                checkboxFillReactor.Enabled = (block) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                        return isWorkingAndEnabled.Invoke(block) && system.Settings.GetPullFromReactor();
                    return false;
                };
                checkboxFillReactor.Getter = (block) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                    {
                        return system.Settings.GetFillReactor();
                    }
                    return false;
                };
                checkboxFillReactor.Setter = (block, value) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                    {
                        system.Settings.SetFillReactor(value);
                        system.SendToServer("FillReactor", "SET", value.ToString());
                        UpdateVisual(block);
                    }
                };
                checkboxFillReactor.SupportsMultipleBlocks = true;
                CreateCheckBoxAction("FillReactor", checkboxFillReactor);
                CustomControls.Add(checkboxFillReactor);

                var sliderFillSmallReactor = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, IMyOreDetector>("SliderFillSmallReactor");
                sliderFillSmallReactor.Title = MyStringId.GetOrCompute("Small reactors/engines fuel");
                sliderFillSmallReactor.Tooltip = MyStringId.GetOrCompute("Set the base amount to fill the small reactors/engines, the value will be multiply by the size of the block.");
                sliderFillSmallReactor.SetLimits(1, 25);
                sliderFillSmallReactor.Enabled = (block) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                        return isWorkingAndEnabled.Invoke(block) && system.Settings.GetPullFromReactor() && system.Settings.GetFillReactor();
                    return false;
                };
                sliderFillSmallReactor.Getter = (block) =>
                {
                    var system = GetSystem(block);
                    return system != null ? system.Settings.GetSmallReactorFuelAmount() : 0;
                };
                sliderFillSmallReactor.Setter = (block, value) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                    {
                        system.Settings.SetSmallReactorFuelAmount(value);
                        system.SendToServer("SmallReactorFuelAmount", "SET", value.ToString());
                    }
                };
                sliderFillSmallReactor.Writer = (block, val) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                    {
                        val.Append(Math.Round(system.Settings.GetSmallReactorFuelAmount(), 2, MidpointRounding.AwayFromZero));
                    }
                };
                sliderFillSmallReactor.SupportsMultipleBlocks = true;
                CustomControls.Add(sliderFillSmallReactor);
                CreateSliderActions("FillSmallReactor", sliderFillSmallReactor);

                var sliderFillLargeReactor = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, IMyOreDetector>("SliderFillLargeReactor");
                sliderFillLargeReactor.Title = MyStringId.GetOrCompute("Large reactors/engines fuel");
                sliderFillLargeReactor.Tooltip = MyStringId.GetOrCompute("Set the base amount to fill the large reactors/engines, the value will be multiply by the size of the block.");
                sliderFillLargeReactor.SetLimits(10, 250);
                sliderFillLargeReactor.Enabled = (block) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                        return isWorkingAndEnabled.Invoke(block) && system.Settings.GetPullFromReactor() && system.Settings.GetFillReactor();
                    return false;
                };
                sliderFillLargeReactor.Getter = (block) =>
                {
                    var system = GetSystem(block);
                    return system != null ? system.Settings.GetLargeReactorFuelAmount() : 0;
                };
                sliderFillLargeReactor.Setter = (block, value) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                    {
                        system.Settings.SetLargeReactorFuelAmount(value);
                        system.SendToServer("LargeReactorFuelAmount", "SET", value.ToString());
                    }
                };
                sliderFillLargeReactor.Writer = (block, val) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                    {
                        val.Append(Math.Round(system.Settings.GetLargeReactorFuelAmount(), 2, MidpointRounding.AwayFromZero));
                    }
                };
                sliderFillLargeReactor.SupportsMultipleBlocks = true;
                CustomControls.Add(sliderFillLargeReactor);
                CreateSliderActions("FillLargeReactor", sliderFillLargeReactor);

                var checkboxPullFromGasGenerator = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCheckbox, IMyOreDetector>("CheckboxPullFromGasGenerator");
                checkboxPullFromGasGenerator.Title = MyStringId.GetOrCompute("Pull from Gas Generators.");
                checkboxPullFromGasGenerator.Tooltip = MyStringId.GetOrCompute("If enabled will pull not ice from Gas Generators.");
                checkboxPullFromGasGenerator.OnText = MyStringId.GetOrCompute("Yes");
                checkboxPullFromGasGenerator.OffText = MyStringId.GetOrCompute("No");
                checkboxPullFromGasGenerator.Enabled = isWorkingAndEnabled;
                checkboxPullFromGasGenerator.Getter = (block) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                    {
                        return system.Settings.GetPullFromGasGenerator();
                    }
                    return false;
                };
                checkboxPullFromGasGenerator.Setter = (block, value) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                    {
                        system.Settings.SetPullFromGasGenerator(value);
                        system.SendToServer("PullFromGasGenerator", "SET", value.ToString());
                        UpdateVisual(block);
                    }
                };
                checkboxPullFromGasGenerator.SupportsMultipleBlocks = true;
                CreateCheckBoxAction("PullFromGasGenerator", checkboxPullFromGasGenerator);
                CustomControls.Add(checkboxPullFromGasGenerator);

                var checkboxFillGasGenerator = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCheckbox, IMyOreDetector>("CheckboxFillGasGenerator");
                checkboxFillGasGenerator.Title = MyStringId.GetOrCompute("Fill Gas Generators with ice.");
                checkboxFillGasGenerator.Tooltip = MyStringId.GetOrCompute("If enabled will fill Gas Generators with ice.");
                checkboxFillGasGenerator.OnText = MyStringId.GetOrCompute("Yes");
                checkboxFillGasGenerator.OffText = MyStringId.GetOrCompute("No");
                checkboxFillGasGenerator.Enabled = (block) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                        return isWorkingAndEnabled.Invoke(block) && system.Settings.GetPullFromGasGenerator();
                    return false;
                };
                checkboxFillGasGenerator.Getter = (block) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                    {
                        return system.Settings.GetFillGasGenerator();
                    }
                    return false;
                };
                checkboxFillGasGenerator.Setter = (block, value) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                    {
                        system.Settings.SetFillGasGenerator(value);
                        system.SendToServer("FillGasGenerator", "SET", value.ToString());
                        UpdateVisual(block);
                    }
                };
                checkboxFillGasGenerator.SupportsMultipleBlocks = true;
                CreateCheckBoxAction("FillGasGenerator", checkboxFillGasGenerator);
                CustomControls.Add(checkboxFillGasGenerator);

                var sliderFillSmallGasGenerator = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, IMyOreDetector>("SliderFillSmallGasGenerator");
                sliderFillSmallGasGenerator.Title = MyStringId.GetOrCompute("Small Gas Generators ice");
                sliderFillSmallGasGenerator.Tooltip = MyStringId.GetOrCompute("Set the base amount to fill the small Gas Generators, the value will be multiply by the size of the block.");
                sliderFillSmallGasGenerator.SetLimits(16, 64);
                sliderFillSmallGasGenerator.Enabled = (block) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                        return isWorkingAndEnabled.Invoke(block) && system.Settings.GetPullFromGasGenerator() && system.Settings.GetFillGasGenerator();
                    return false;
                };
                sliderFillSmallGasGenerator.Getter = (block) =>
                {
                    var system = GetSystem(block);
                    return system != null ? system.Settings.GetSmallGasGeneratorAmount() : 0;
                };
                sliderFillSmallGasGenerator.Setter = (block, value) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                    {
                        system.Settings.SetSmallGasGeneratorAmount(value);
                        system.SendToServer("SmallGasGeneratorAmount", "SET", value.ToString());
                    }
                };
                sliderFillSmallGasGenerator.Writer = (block, val) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                    {
                        val.Append(Math.Round(system.Settings.GetSmallGasGeneratorAmount(), 2, MidpointRounding.AwayFromZero));
                    }
                };
                sliderFillSmallGasGenerator.SupportsMultipleBlocks = true;
                CustomControls.Add(sliderFillSmallGasGenerator);
                CreateSliderActions("FillSmallGasGenerator", sliderFillSmallGasGenerator);

                var sliderFillLargeGasGenerator = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, IMyOreDetector>("SliderFillLargeGasGenerator");
                sliderFillLargeGasGenerator.Title = MyStringId.GetOrCompute("Large Gas Generators ice");
                sliderFillLargeGasGenerator.Tooltip = MyStringId.GetOrCompute("Set the base amount to fill the large Gas Generators, the value will be multiply by the size of the block.");
                sliderFillLargeGasGenerator.SetLimits(100, 2000);
                sliderFillLargeGasGenerator.Enabled = (block) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                        return isWorkingAndEnabled.Invoke(block) && system.Settings.GetPullFromGasGenerator() && system.Settings.GetFillGasGenerator();
                    return false;
                };
                sliderFillLargeGasGenerator.Getter = (block) =>
                {
                    var system = GetSystem(block);
                    return system != null ? system.Settings.GetLargeGasGeneratorAmount() : 0;
                };
                sliderFillLargeGasGenerator.Setter = (block, value) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                    {
                        system.Settings.SetLargeGasGeneratorAmount(value);
                        system.SendToServer("LargeGasGeneratorAmount", "SET", value.ToString());
                    }
                };
                sliderFillLargeGasGenerator.Writer = (block, val) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                    {
                        val.Append(Math.Round(system.Settings.GetLargeGasGeneratorAmount(), 2, MidpointRounding.AwayFromZero));
                    }
                };
                sliderFillLargeGasGenerator.SupportsMultipleBlocks = true;
                CustomControls.Add(sliderFillLargeGasGenerator);
                CreateSliderActions("FillLargeGasGenerator", sliderFillLargeGasGenerator);

                var checkboxPullFromGasTank = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCheckbox, IMyOreDetector>("CheckboxPullFromGasTank");
                checkboxPullFromGasTank.Title = MyStringId.GetOrCompute("Pull from Gas Tanks.");
                checkboxPullFromGasTank.Tooltip = MyStringId.GetOrCompute("If enabled will pull from Gas Tanks.");
                checkboxPullFromGasTank.OnText = MyStringId.GetOrCompute("Yes");
                checkboxPullFromGasTank.OffText = MyStringId.GetOrCompute("No");
                checkboxPullFromGasTank.Enabled = isWorkingAndEnabled;
                checkboxPullFromGasTank.Getter = (block) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                    {
                        return system.Settings.GetPullFromGasTank();
                    }
                    return false;
                };
                checkboxPullFromGasTank.Setter = (block, value) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                    {
                        system.Settings.SetPullFromGasTank(value);
                        system.SendToServer("PullFromGasTank", "SET", value.ToString());
                        UpdateVisual(block);
                    }
                };
                checkboxPullFromGasTank.SupportsMultipleBlocks = true;
                CreateCheckBoxAction("PullFromGasTank", checkboxPullFromGasTank);
                CustomControls.Add(checkboxPullFromGasTank);

                var checkboxFillBottles = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCheckbox, IMyOreDetector>("CheckboxFillBottles");
                checkboxFillBottles.Title = MyStringId.GetOrCompute("Fill bottles with gas.");
                checkboxFillBottles.Tooltip = MyStringId.GetOrCompute("If enabled will try to fill bottles in tanks or generators.");
                checkboxFillBottles.OnText = MyStringId.GetOrCompute("Yes");
                checkboxFillBottles.OffText = MyStringId.GetOrCompute("No");
                checkboxFillBottles.Enabled = (block) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                        return isWorkingAndEnabled.Invoke(block) && (system.Settings.GetPullFromGasGenerator() || system.Settings.GetPullFromGasTank());
                    return false;
                };
                checkboxFillBottles.Getter = (block) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                    {
                        return system.Settings.GetFillBottles();
                    }
                    return false;
                };
                checkboxFillBottles.Setter = (block, value) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                    {
                        system.Settings.SetFillBottles(value);
                        system.SendToServer("FillBottles", "SET", value.ToString());
                    }
                };
                checkboxFillBottles.SupportsMultipleBlocks = true;
                CreateCheckBoxAction("FillBottles", checkboxFillBottles);
                CustomControls.Add(checkboxFillBottles);

                if (AILogisticsAutomationSession.IsUsingStatsAndEffects())
                {

                    var labeESStats = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlLabel, IMyOreDetector>("LabeESStats");
                    labeESStats.Label = MyStringId.GetOrCompute("Stats & Effects Blocks");
                    CustomControls.Add(labeESStats);

                    var checkboxPullFromComposter = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCheckbox, IMyOreDetector>("CheckboxPullFromComposter");
                    checkboxPullFromComposter.Title = MyStringId.GetOrCompute("Pull from Composter.");
                    checkboxPullFromComposter.Tooltip = MyStringId.GetOrCompute("If enabled will pull not organic from Composter.");
                    checkboxPullFromComposter.OnText = MyStringId.GetOrCompute("Yes");
                    checkboxPullFromComposter.OffText = MyStringId.GetOrCompute("No");
                    checkboxPullFromComposter.Enabled = isWorkingAndEnabled;
                    checkboxPullFromComposter.Getter = (block) =>
                    {
                        var system = GetSystem(block);
                        if (system != null)
                        {
                            return system.Settings.GetPullFromComposter();
                        }
                        return false;
                    };
                    checkboxPullFromComposter.Setter = (block, value) =>
                    {
                        var system = GetSystem(block);
                        if (system != null)
                        {
                            system.Settings.SetPullFromComposter(value);
                            system.SendToServer("PullFromComposter", "SET", value.ToString());
                            UpdateVisual(block);
                        }
                    };
                    checkboxPullFromComposter.SupportsMultipleBlocks = true;
                    CreateCheckBoxAction("PullFromComposter", checkboxPullFromComposter);
                    CustomControls.Add(checkboxPullFromComposter);

                    var checkboxFillComposter = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCheckbox, IMyOreDetector>("CheckboxFillComposter");
                    checkboxFillComposter.Title = MyStringId.GetOrCompute("Fill Composter with organic.");
                    checkboxFillComposter.Tooltip = MyStringId.GetOrCompute("If enabled will fill Composter with organic.");
                    checkboxFillComposter.OnText = MyStringId.GetOrCompute("Yes");
                    checkboxFillComposter.OffText = MyStringId.GetOrCompute("No");
                    checkboxFillComposter.Enabled = (block) =>
                    {
                        var system = GetSystem(block);
                        if (system != null)
                            return isWorkingAndEnabled.Invoke(block) && system.Settings.GetPullFromComposter();
                        return false;
                    };
                    checkboxFillComposter.Getter = (block) =>
                    {
                        var system = GetSystem(block);
                        if (system != null)
                        {
                            return system.Settings.GetFillComposter();
                        }
                        return false;
                    };
                    checkboxFillComposter.Setter = (block, value) =>
                    {
                        var system = GetSystem(block);
                        if (system != null)
                        {
                            system.Settings.SetFillComposter(value);
                            system.SendToServer("FillComposter", "SET", value.ToString());
                            UpdateVisual(block);
                        }
                    };
                    checkboxFillComposter.SupportsMultipleBlocks = true;
                    CreateCheckBoxAction("FillComposter", checkboxFillComposter);
                    CustomControls.Add(checkboxFillComposter);

                    var checkboxPullFromFishTrap = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCheckbox, IMyOreDetector>("CheckboxPullFromFishTrap");
                    checkboxPullFromFishTrap.Title = MyStringId.GetOrCompute("Pull from Fish Trap.");
                    checkboxPullFromFishTrap.Tooltip = MyStringId.GetOrCompute("If enabled will pull not baits from FishTrap.");
                    checkboxPullFromFishTrap.OnText = MyStringId.GetOrCompute("Yes");
                    checkboxPullFromFishTrap.OffText = MyStringId.GetOrCompute("No");
                    checkboxPullFromFishTrap.Enabled = isWorkingAndEnabled;
                    checkboxPullFromFishTrap.Getter = (block) =>
                    {
                        var system = GetSystem(block);
                        if (system != null)
                        {
                            return system.Settings.GetPullFishTrap();
                        }
                        return false;
                    };
                    checkboxPullFromFishTrap.Setter = (block, value) =>
                    {
                        var system = GetSystem(block);
                        if (system != null)
                        {
                            system.Settings.SetPullFishTrap(value);
                            system.SendToServer("PullFishTrap", "SET", value.ToString());
                            UpdateVisual(block);
                        }
                    };
                    checkboxPullFromFishTrap.SupportsMultipleBlocks = true;
                    CreateCheckBoxAction("PullFromFishTrap", checkboxPullFromFishTrap);
                    CustomControls.Add(checkboxPullFromFishTrap);

                    var checkboxFillFishTrap = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCheckbox, IMyOreDetector>("CheckboxFillFishTrap");
                    checkboxFillFishTrap.Title = MyStringId.GetOrCompute("Fill FishTrap with organic.");
                    checkboxFillFishTrap.Tooltip = MyStringId.GetOrCompute("If enabled will fill Fish Traps with baits.");
                    checkboxFillFishTrap.OnText = MyStringId.GetOrCompute("Yes");
                    checkboxFillFishTrap.OffText = MyStringId.GetOrCompute("No");
                    checkboxFillFishTrap.Enabled = (block) =>
                    {
                        var system = GetSystem(block);
                        if (system != null)
                            return isWorkingAndEnabled.Invoke(block) && system.Settings.GetPullFishTrap();
                        return false;
                    };
                    checkboxFillFishTrap.Getter = (block) =>
                    {
                        var system = GetSystem(block);
                        if (system != null)
                        {
                            return system.Settings.GetFillFishTrap();
                        }
                        return false;
                    };
                    checkboxFillFishTrap.Setter = (block, value) =>
                    {
                        var system = GetSystem(block);
                        if (system != null)
                        {
                            system.Settings.SetFillFishTrap(value);
                            system.SendToServer("FillFishTrap", "SET", value.ToString());
                            UpdateVisual(block);
                        }
                    };
                    checkboxFillFishTrap.SupportsMultipleBlocks = true;
                    CreateCheckBoxAction("FillFishTrap", checkboxFillFishTrap);
                    CustomControls.Add(checkboxFillFishTrap);

                    var checkboxPullFromRefrigerator = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCheckbox, IMyOreDetector>("CheckboxPullFromRefrigerator");
                    checkboxPullFromRefrigerator.Title = MyStringId.GetOrCompute("Pull from Refrigerator.");
                    checkboxPullFromRefrigerator.Tooltip = MyStringId.GetOrCompute("If enabled will pull not foods from Refrigerator.");
                    checkboxPullFromRefrigerator.OnText = MyStringId.GetOrCompute("Yes");
                    checkboxPullFromRefrigerator.OffText = MyStringId.GetOrCompute("No");
                    checkboxPullFromRefrigerator.Enabled = isWorkingAndEnabled;
                    checkboxPullFromRefrigerator.Getter = (block) =>
                    {
                        var system = GetSystem(block);
                        if (system != null)
                        {
                            return system.Settings.GetPullRefrigerator();
                        }
                        return false;
                    };
                    checkboxPullFromRefrigerator.Setter = (block, value) =>
                    {
                        var system = GetSystem(block);
                        if (system != null)
                        {
                            system.Settings.SetPullRefrigerator(value);
                            system.SendToServer("PullRefrigerator", "SET", value.ToString());
                            UpdateVisual(block);
                        }
                    };
                    checkboxPullFromRefrigerator.SupportsMultipleBlocks = true;
                    CreateCheckBoxAction("PullFromRefrigerator", checkboxPullFromRefrigerator);
                    CustomControls.Add(checkboxPullFromRefrigerator);

                    var checkboxFillRefrigerator = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCheckbox, IMyOreDetector>("CheckboxFillRefrigerator");
                    checkboxFillRefrigerator.Title = MyStringId.GetOrCompute("Fill Refrigerator with food.");
                    checkboxFillRefrigerator.Tooltip = MyStringId.GetOrCompute("If enabled will fill Refrigerator with foods.");
                    checkboxFillRefrigerator.OnText = MyStringId.GetOrCompute("Yes");
                    checkboxFillRefrigerator.OffText = MyStringId.GetOrCompute("No");
                    checkboxFillRefrigerator.Enabled = (block) =>
                    {
                        var system = GetSystem(block);
                        if (system != null)
                            return isWorkingAndEnabled.Invoke(block) && system.Settings.GetPullRefrigerator();
                        return false;
                    };
                    checkboxFillRefrigerator.Getter = (block) =>
                    {
                        var system = GetSystem(block);
                        if (system != null)
                        {
                            return system.Settings.GetFillRefrigerator();
                        }
                        return false;
                    };
                    checkboxFillRefrigerator.Setter = (block, value) =>
                    {
                        var system = GetSystem(block);
                        if (system != null)
                        {
                            system.Settings.SetFillRefrigerator(value);
                            system.SendToServer("FillRefrigerator", "SET", value.ToString());
                            UpdateVisual(block);
                        }
                    };
                    checkboxFillRefrigerator.SupportsMultipleBlocks = true;
                    CreateCheckBoxAction("FillRefrigerator", checkboxFillRefrigerator);
                    CustomControls.Add(checkboxFillRefrigerator);

                }

            }
        }

        protected override string GetActionPrefix()
        {
            return "AIInventoryManager";
        }

    }

}