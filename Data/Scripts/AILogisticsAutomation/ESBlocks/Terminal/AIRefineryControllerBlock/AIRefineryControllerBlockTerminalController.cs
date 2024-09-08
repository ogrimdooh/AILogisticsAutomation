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
using VRage.Game.ModAPI;
using System.Text;

namespace AILogisticsAutomation
{
    public class AIRefineryControllerBlockTerminalController : BaseTerminalController<AIRefineryControllerBlock, IMyOreDetector>
    {

        public const float MIN_CONDITION_VALUE = 10;
        public const float MAX_CONDITION_VALUE = 10000;
        public const float DEFAULT_CONDITION_VALUE = 1000;

        protected List<MyDefinitionId> validIds = new List<MyDefinitionId>();
        protected List<MyRefineryDefinition> refineries = new List<MyRefineryDefinition>();
        protected List<MyTerminalControlComboBoxItem> validIdsUI = new List<MyTerminalControlComboBoxItem>();

        protected int selectedFilterItemId = 0;
        protected int selectedRefineryFilterItemId = 0;

        protected int selectedTriggerConditionQueryType = 0;
        protected int selectedTriggerConditionItemType = 0;
        protected int selectedTriggerConditionItemId = 0;
        protected int selectedTriggerConditionOperationType = 0;
        protected float selectedTriggerConditionValue = DEFAULT_CONDITION_VALUE;

        protected int selectedTriggerFilterItemId = 0;

        protected override bool CanAddControls(IMyTerminalBlock block)
        {
            var validSubTypes = new string[] { "AIRefineryController", "AIRefineryControllerReskin" };
            return block.BlockDefinition.TypeId == typeof(MyObjectBuilder_OreDetector) && validSubTypes.Contains(block.BlockDefinition.SubtypeId);
        }

        protected bool CheckHadAValidIngotInRecipes(MyDefinitionId id)
        {
            return MyDefinitionManager.Static.GetBlueprintDefinitions().Any(x =>
                x.Prerequisites.Length == 1 &&
                x.Prerequisites[0].Id == id &&
                x.Results.Any(y => y.Id.TypeId == typeof(MyObjectBuilder_Ingot)) &&
                refineries.Any(y => y.BlueprintClasses.Any(k => k.ContainsBlueprint(x)))
            );
        }

        public void LoadItensIds()
        {
            // Load base itens Ids
            DoLoadPhysicalItemIds();
            // Load Refinery Recipes
            var targetTypes = new MyObjectBuilderType[] { typeof(MyObjectBuilder_Ore) };
            validIds.Clear();
            validIdsUI.Clear();
            refineries = MyDefinitionManager.Static.GetAllDefinitions().Where(x => x.Id.TypeId == typeof(MyObjectBuilder_Refinery)).Cast<MyRefineryDefinition>().ToList();
            var list = MyDefinitionManager.Static.GetPhysicalItemDefinitions().Where(x =>
                targetTypes.Contains(x.Id.TypeId) &&
                CheckHadAValidIngotInRecipes(x.Id)
            ).OrderBy(x => x.DisplayNameText).ToArray();
            for (int i = 0; i < list.Length; i++)
            {
                var item = list[i];
                validIds.Add(item.Id);
                var newItem = new MyTerminalControlComboBoxItem() { Value = MyStringId.GetOrCompute(item.DisplayNameText), Key = i };
                validIdsUI.Add(newItem);
            }
        }

        protected override void DoInitializeControls()
        {

            if (!AILogisticsAutomationSession.IsUsingExtendedSurvival())
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

            Func<IMyTerminalBlock, bool> isWorkingEnabledAndDefaultOreSelected = (block) =>
            {
                var system = GetSystem(block);
                return system != null && isWorkingAndEnabled.Invoke(block) && system.Settings.DefaultOres.Contains(system.Settings.SelectedDefaultOre);
            };

            Func<IMyTerminalBlock, bool> isWorkingEnabledAndRefinerySelected = (block) =>
            {
                var system = GetSystem(block);
                if (system != null)
                {
                    List<IMySlimBlock> blocks = new List<IMySlimBlock>();
                    system.CurrentEntity.CubeGrid.GetBlocks(blocks, x => refineries.Any(b => b.Id == x.BlockDefinition.Id));
                    var exists = blocks.Any(x => x.FatBlock.EntityId == system.Settings.SelectedRefinery);
                    return isWorkingAndEnabled.Invoke(block) && exists;
                }
                return false;
            };

            Func<IMyTerminalBlock, bool> isWorkingEnabledAndRefinerySelectedAdded = (block) =>
            {
                var system = GetSystem(block);
                if (system != null)
                {
                    List<IMySlimBlock> blocks = new List<IMySlimBlock>();
                    system.CurrentEntity.CubeGrid.GetBlocks(blocks, x => refineries.Any(b => b.Id == x.BlockDefinition.Id));
                    var exists = blocks.Any(x => x.FatBlock.EntityId == system.Settings.SelectedRefinery);
                    var added = system.Settings.GetDefinitions().ContainsKey(system.Settings.SelectedRefinery);
                    return isWorkingAndEnabled.Invoke(block) && exists && added;
                }
                return false;
            };

            Func<IMyTerminalBlock, bool> isWorkingEnabledAndRefinerySelectedOreSelected = (block) =>
            {
                var system = GetSystem(block);
                if (system != null)
                {
                    List<IMySlimBlock> blocks = new List<IMySlimBlock>();
                    system.CurrentEntity.CubeGrid.GetBlocks(blocks, x => refineries.Any(b => b.Id == x.BlockDefinition.Id));
                    var exists = blocks.Any(x => x.FatBlock.EntityId == system.Settings.SelectedRefinery);
                    var added = system.Settings.GetDefinitions().ContainsKey(system.Settings.SelectedRefinery);
                    return isWorkingAndEnabled.Invoke(block) && exists && added && system.Settings.GetDefinitions()[system.Settings.SelectedRefinery].Ores.Contains(system.Settings.SelectedRefineryOre);
                }
                return false;
            };

            Func<IMyTerminalBlock, bool> isWorkingEnabledAndTriggerSelected = (block) =>
            {
                var system = GetSystem(block);
                return system != null && isWorkingAndEnabled.Invoke(block) && system.Settings.GetTriggers().ContainsKey(system.Settings.SelectedTriggerId);
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

            CreateTerminalLabel("DefaultOresLabel", "Default Ore Priority");

            CreateCombobox(
                "FilterItemId",
                "Filter Ore Id",
                isWorkingAndEnabled,
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
                    list.AddRange(validIdsUI);
                },
                tooltip: "Select a filter ore Id."
            );

            /* Button Add Filter */
            CreateTerminalButton(
                "AddedSelectedDefaultPriority",
                "Added Selected Ore",
                isWorkingAndEnabled,
                (block) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                    {
                        var idToUse = validIds[selectedFilterItemId];
                        if (!system.Settings.DefaultOres.Contains(idToUse.SubtypeName))
                        {
                            system.Settings.DefaultOres.AddPriority(idToUse.SubtypeName);
                            system.SendToServer("DefaultOres", "ADD", idToUse.SubtypeName, null);
                            UpdateVisual(block);
                        }
                    }
                }
            );

            CreateListbox(
                "DefaultOresList",
                "Ore Priority",
                isWorkingAndEnabled,
                (block, list, selectedList) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                    {
                        foreach (var ore in system.Settings.DefaultOres.GetAll())
                        {
                            var item = new MyTerminalControlListBoxItem(MyStringId.GetOrCompute(ore), MyStringId.GetOrCompute(ore), ore);
                            list.Add(item);
                            if (ore == system.Settings.SelectedDefaultOre)
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
                        system.Settings.SelectedDefaultOre = selectedList[0].UserData.ToString();
                        UpdateVisual(block);
                    }
                },
                tooltip: "List of the ore priority to all refineries."
            );

            /* Button Move Up */
            CreateTerminalButton(
                "MoveUpSelectedDefaultPriority",
                "Move Up Selected Ore",
                isWorkingEnabledAndDefaultOreSelected,
                (block) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                    {
                        if (system.Settings.DefaultOres.Contains(system.Settings.SelectedDefaultOre))
                        {
                            system.Settings.DefaultOres.MoveUp(system.Settings.SelectedDefaultOre);
                            system.SendToServer("DefaultOres", "UP", system.Settings.SelectedDefaultOre, null);
                            UpdateVisual(block);
                        }
                    }
                }
            );

            /* Button Move Down */
            CreateTerminalButton(
                "MoveDownSelectedDefaultPriority",
                "Move Down Selected Ore",
                isWorkingEnabledAndDefaultOreSelected,
                (block) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                    {
                        if (system.Settings.DefaultOres.Contains(system.Settings.SelectedDefaultOre))
                        {
                            system.Settings.DefaultOres.MoveDown(system.Settings.SelectedDefaultOre);
                            system.SendToServer("DefaultOres", "DOWN", system.Settings.SelectedDefaultOre, null);
                            UpdateVisual(block);
                        }
                    }
                }
            );

            /* Button Remove */
            CreateTerminalButton(
                "RemoveSelectedDefaultPriority",
                "Remove Selected Ore",
                isWorkingEnabledAndDefaultOreSelected,
                (block) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                    {
                        if (system.Settings.DefaultOres.Contains(system.Settings.SelectedDefaultOre))
                        {
                            system.Settings.DefaultOres.RemovePriority(system.Settings.SelectedDefaultOre);
                            system.SendToServer("DefaultOres", "DEL", system.Settings.SelectedDefaultOre, null);
                            UpdateVisual(block);
                        }
                    }
                }
            );

            CreateTerminalSeparator("EspecificOresSeparetor");

            CreateTerminalLabel("EspecificOresLabel", "Single Refinery Ore Priority");

            CreateListbox(
                "EspecificRefineryList",
                "Grid Refinery Blocks",
                isWorkingAndEnabled,
                (block, list, selectedList) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                    {
                        List<IMySlimBlock> blocks = new List<IMySlimBlock>();
                        system.CurrentEntity.CubeGrid.GetBlocks(blocks, x => refineries.Any(b => b.Id == x.BlockDefinition.Id));
                        foreach (var blk in blocks)
                        {
                            var added = system.Settings.GetDefinitions().ContainsKey(blk.FatBlock.EntityId);

                            var name = string.Format("[{0}] {2} - ({1})", added ? "X" : " ", blk.BlockDefinition.DisplayNameText, blk.FatBlock.DisplayNameText);

                            var item = new MyTerminalControlListBoxItem(MyStringId.GetOrCompute(name), MyStringId.GetOrCompute(name), blk.FatBlock.EntityId);
                            list.Add(item);
                            if (blk.FatBlock.EntityId == system.Settings.SelectedRefinery)
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
                        system.Settings.SelectedRefinery = (long)selectedList[0].UserData;
                        UpdateVisual(block);
                    }
                },
                tooltip: "List of the ore priority to all refineries."
            );

            CreateCheckbox(
                "CheckboxAddContainer",
                "Added custom priority",
                isWorkingEnabledAndRefinerySelected,
                (block) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                    {
                        List<IMySlimBlock> blocks = new List<IMySlimBlock>();
                        system.CurrentEntity.CubeGrid.GetBlocks(blocks, x => refineries.Any(b => b.Id == x.BlockDefinition.Id));
                        var exists = blocks.Any(x => x.FatBlock.EntityId == system.Settings.SelectedRefinery);
                        if (exists)
                        {
                            return system.Settings.GetDefinitions().ContainsKey(system.Settings.SelectedRefinery);
                        }
                    }
                    return false;
                },
                (block, value) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                    {
                        List<IMySlimBlock> blocks = new List<IMySlimBlock>();
                        system.CurrentEntity.CubeGrid.GetBlocks(blocks, x => refineries.Any(b => b.Id == x.BlockDefinition.Id));
                        var exists = blocks.Any(x => x.FatBlock.EntityId == system.Settings.SelectedRefinery);
                        if (exists)
                        {
                            var added = system.Settings.GetDefinitions().ContainsKey(system.Settings.SelectedRefinery);
                            if (value)
                            {
                                if (!added)
                                {
                                    system.Settings.GetDefinitions()[system.Settings.SelectedRefinery] = new AIRefineryControllerRefineryPrioritySettings()
                                    {
                                        EntityId = system.Settings.SelectedRefinery
                                    };
                                    system.SendToServer("Definitions", "ADD", system.Settings.SelectedRefinery.ToString());
                                    if (system.Settings.GetIgnoreRefinery().Contains(system.Settings.SelectedRefinery))
                                    {
                                        system.Settings.GetIgnoreRefinery().Remove(system.Settings.SelectedRefinery);
                                        system.SendToServer("IgnoreCargos", "DEL", system.Settings.SelectedRefinery.ToString());
                                    }
                                    UpdateVisual(block);
                                }
                            }
                            else
                            {
                                if (added)
                                {
                                    var dataToRemove = system.Settings.GetDefinitions().ContainsKey(system.Settings.SelectedRefinery);
                                    if (dataToRemove)
                                    {
                                        system.Settings.GetDefinitions().Remove(system.Settings.SelectedRefinery);
                                        system.SendToServer("Definitions", "DEL", system.Settings.SelectedRefinery.ToString());
                                    }
                                    UpdateVisual(block);
                                }
                            }
                        }
                    }
                },
                tooltip: "Refineries added to list will use a exclusive ore priority."
            );

            CreateTerminalSeparator("RefineryOptionsSeparator");

            CreateTerminalLabel("RefineryOptionsSeparatorLable", "Selected Refinery Priority");

            CreateCombobox(
                "FilterRefineryItemId",
                "Filter Ore Id",
                isWorkingEnabledAndRefinerySelectedAdded,
                (block) =>
                {
                    var system = GetSystem(block);
                    if (system == null) return 0;
                    else return selectedRefineryFilterItemId;
                },
                (block, value) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                    {
                        selectedRefineryFilterItemId = (int)value;
                    }
                },
                (list) =>
                {
                    list.AddRange(validIdsUI);
                },
                tooltip: "Select a filter ore Id."
            );

            /* Button Add Filter */
            CreateTerminalButton(
                "AddedSelectedRefineryPriority",
                "Added Selected Ore",
                isWorkingEnabledAndRefinerySelectedAdded,
                (block) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                    {
                        var idToUse = validIds[selectedRefineryFilterItemId];
                        if (system.Settings.GetDefinitions().ContainsKey(system.Settings.SelectedRefinery))
                        {
                            var def = system.Settings.GetDefinitions()[system.Settings.SelectedRefinery];
                            if (!def.Ores.Contains(idToUse.SubtypeName))
                            {
                                def.Ores.AddPriority(idToUse.SubtypeName);
                                system.SendToServer("Ores", "ADD", idToUse.SubtypeName, system.Settings.SelectedRefinery.ToString());
                                UpdateVisual(block);
                            }
                        }
                    }
                }
            );

            CreateListbox(
                "RefineryOresList",
                "Refinery Ore Priority",
                isWorkingEnabledAndRefinerySelectedAdded,
                (block, list, selectedList) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                    {
                        if (system.Settings.GetDefinitions().ContainsKey(system.Settings.SelectedRefinery))
                        {
                            var def = system.Settings.GetDefinitions()[system.Settings.SelectedRefinery];
                            foreach (var ore in def.Ores.GetAll())
                            {
                                var item = new MyTerminalControlListBoxItem(MyStringId.GetOrCompute(ore), MyStringId.GetOrCompute(ore), ore);
                                list.Add(item);
                                if (ore == system.Settings.SelectedRefineryOre)
                                    selectedList.Add(item);
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
                        system.Settings.SelectedRefineryOre = selectedList[0].UserData.ToString();
                        UpdateVisual(block);
                    }
                },
                tooltip: "List of the ore priority to selected refinery."
            );

            /* Button Move Up */
            CreateTerminalButton(
                "MoveUpSelectedRefineryPriority",
                "Move Up Selected Ore",
                isWorkingEnabledAndRefinerySelectedOreSelected,
                (block) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                    {
                        if (system.Settings.GetDefinitions().ContainsKey(system.Settings.SelectedRefinery))
                        {
                            var def = system.Settings.GetDefinitions()[system.Settings.SelectedRefinery];
                            if (def.Ores.Contains(system.Settings.SelectedRefineryOre))
                            {
                                def.Ores.MoveUp(system.Settings.SelectedRefineryOre);
                                system.SendToServer("Ores", "UP", system.Settings.SelectedRefineryOre, system.Settings.SelectedRefinery.ToString());
                                UpdateVisual(block);
                            }
                        }
                    }
                }
            );

            /* Button Move Down */
            CreateTerminalButton(
                "MoveDownSelectedRefineryPriority",
                "Move Down Selected Ore",
                isWorkingEnabledAndRefinerySelectedOreSelected,
                (block) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                    {
                        if (system.Settings.GetDefinitions().ContainsKey(system.Settings.SelectedRefinery))
                        {
                            var def = system.Settings.GetDefinitions()[system.Settings.SelectedRefinery];
                            if (def.Ores.Contains(system.Settings.SelectedRefineryOre))
                            {
                                def.Ores.MoveDown(system.Settings.SelectedRefineryOre);
                                system.SendToServer("Ores", "DOWN", system.Settings.SelectedRefineryOre, system.Settings.SelectedRefinery.ToString());
                                UpdateVisual(block);
                            }
                        }
                    }
                }
            );

            /* Button Remove */
            CreateTerminalButton(
                "RemoveSelectedRefineryPriority",
                "Remove Selected Ore",
                isWorkingEnabledAndRefinerySelectedOreSelected,
                (block) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                    {
                        if (system.Settings.GetDefinitions().ContainsKey(system.Settings.SelectedRefinery))
                        {
                            var def = system.Settings.GetDefinitions()[system.Settings.SelectedRefinery];
                            if (def.Ores.Contains(system.Settings.SelectedRefineryOre))
                            {
                                def.Ores.RemovePriority(system.Settings.SelectedRefineryOre);
                                system.SendToServer("Ores", "DEL", system.Settings.SelectedRefineryOre, system.Settings.SelectedRefinery.ToString());
                                UpdateVisual(block);
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

                        MyObjectBuilderType[] targetFilter = new MyObjectBuilderType[] { typeof(MyObjectBuilder_Refinery) };
                        IEnumerable<long> ignoreBlocks = system.Settings.GetIgnoreRefinery();

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

                            var targetFunctionalFilter = new MyObjectBuilderType[] { typeof(MyObjectBuilder_Refinery) };
                            
                            if (targetFunctionalFilter.Contains(inventory.BlockDefinition.Id.TypeId))
                            {
                                if (!system.Settings.GetIgnoreRefinery().Contains(system.Settings.SelectedIgnoreEntityId))
                                {
                                    system.Settings.GetIgnoreRefinery().Add(system.Settings.SelectedIgnoreEntityId);
                                    system.SendToServer("IgnoreRefinery", "ADD", system.Settings.SelectedIgnoreEntityId.ToString());
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
                        addedBlocks.AddRange(system.Settings.GetIgnoreRefinery());

                        var targetFunctionalFilter = new MyObjectBuilderType[] { typeof(MyObjectBuilder_Refinery) };
                        
                        foreach (var inventory in targetGrid.Inventories.Where(x => addedBlocks.Contains(x.EntityId)))
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
                        if (system.Settings.GetIgnoreRefinery().Contains(system.Settings.SelectedAddedIgnoreEntityId))
                        {
                            system.Settings.GetIgnoreRefinery().Remove(system.Settings.SelectedAddedIgnoreEntityId);
                            system.SendToServer("IgnoreRefinery", "DEL", system.Settings.SelectedAddedIgnoreEntityId.ToString());
                            UpdateVisual(block);
                        }
                        system.Settings.SelectedAddedIgnoreEntityId = 0;
                    }
                }
            );

            CreateTerminalSeparator("PriorityTriggerSeparator");

            CreateTerminalLabel("DefaultCompsLabel", "Priority Triggers");

            /* Button Add Trigger */
            CreateTerminalButton(
                "AddPriorityTrigger",
                "Add Priority Trigger",
                isWorkingAndEnabled,
                (block) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                    {
                        var triggerId = MyUtils.GetRandomLong();
                        system.Settings.GetTriggers()[triggerId] = new AIRefineryControllerTriggerSettings()
                        {
                            TriggerId = triggerId
                        };
                        system.SendToServer("triggers", "ADD", triggerId.ToString(), null);
                        UpdateVisual(block);
                    }
                }
            );

            CreateListbox(
                "PriorityTriggerList",
                "Priority Triggers",
                isWorkingAndEnabled,
                (block, list, selectedList) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                    {
                        int i = 1;
                        foreach (var trigger in system.Settings.GetTriggers().Values)
                        {
                            var desc = $"Trigger {i} [{trigger.Name}]";
                            var item = new MyTerminalControlListBoxItem(MyStringId.GetOrCompute(desc), MyStringId.GetOrCompute(desc), (object)trigger.TriggerId);
                            list.Add(item);
                            if (trigger.TriggerId == system.Settings.SelectedTriggerId)
                                selectedList.Add(item);
                            i++;
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
                        system.Settings.SelectedTriggerId = (long)selectedList[0].UserData;
                        UpdateVisual(block);
                    }
                },
                tooltip: "List of the Priority triggers to all assemblers."
            );

            /* Button Add Trigger */
            CreateTerminalButton(
                "DelPriorityTrigger",
                "Remove Selected Trigger",
                isWorkingEnabledAndTriggerSelected,
                (block) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                    {
                        if (system.Settings.GetTriggers().ContainsKey(system.Settings.SelectedTriggerId))
                        {
                            system.Settings.GetTriggers().Remove(system.Settings.SelectedTriggerId);
                            system.SendToServer("triggers", "DEL", system.Settings.SelectedTriggerId.ToString(), null);
                            UpdateVisual(block);
                        }
                    }
                }
            );

            CreateTextbox(
                "TriggerNameTextBox",
                "Trigger Name",
                isWorkingEnabledAndTriggerSelected,
                (block) =>
                {
                    var value = new StringBuilder();
                    var system = GetSystem(block);
                    if (system != null)
                    {
                        if (system.Settings.GetTriggers().ContainsKey(system.Settings.SelectedTriggerId))
                        {
                            var targetTrigger = system.Settings.GetTriggers()[system.Settings.SelectedTriggerId];
                            value.Append(targetTrigger.Name);
                        }
                    }
                    return value;
                },
                (block, value) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                    {
                        if (system.Settings.GetTriggers().ContainsKey(system.Settings.SelectedTriggerId))
                        {
                            var targetTrigger = system.Settings.GetTriggers()[system.Settings.SelectedTriggerId];
                            targetTrigger.Name = value.ToString();
                            system.SendToServer("Name", "SET", targetTrigger.Name, system.Settings.SelectedTriggerId.ToString());
                            UpdateVisual(block);
                        }
                    }
                },
                tooltip: "The name of the selected trigger."
            );

            CreateTerminalLabel("SelectedTriggerConditionLabel", "Selected Trigger Condition");

            CreateCombobox(
                "TriggerConditionQueryType",
                "Query Type",
                isWorkingEnabledAndTriggerSelected,
                (block) =>
                {
                    var system = GetSystem(block);
                    if (system == null) return 0;
                    else return selectedTriggerConditionQueryType;
                },
                (block, value) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                    {
                        selectedTriggerConditionQueryType = (int)value;
                        UpdateVisual(block);
                    }
                },
                (list) =>
                {
                    list.Add(new MyTerminalControlComboBoxItem() { Key = 0, Value = MyStringId.GetOrCompute("AND") });
                    list.Add(new MyTerminalControlComboBoxItem() { Key = 1, Value = MyStringId.GetOrCompute("OR") });
                },
                tooltip: "Select a trigger condition query type."
            );

            CreateCombobox(
                "TriggerConditionItemType",
                "Condition Item Type",
                isWorkingEnabledAndTriggerSelected,
                (block) =>
                {
                    var system = GetSystem(block);
                    if (system == null) return 0;
                    else return selectedTriggerConditionItemType;
                },
                (block, value) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                    {
                        selectedTriggerConditionItemType = (int)value;
                        if (PhysicalItemTypes.Values.Any(x => x.Index == selectedTriggerConditionItemType))
                        {
                            var typeToUse = PhysicalItemTypes.Values.FirstOrDefault(x => x.Index == selectedTriggerConditionItemType);
                            selectedTriggerConditionItemId = typeToUse.Items.Min(x => x.Value.Index);
                        }
                        else
                            selectedTriggerConditionItemId = 0;
                        UpdateVisual(block);
                    }
                },
                (list) =>
                {
                    list.AddRange(PhysicalItemTypes.Values.OrderBy(x => x.Index).Select(x => x.ComboBoxItem));
                },
                tooltip: "Select a trigger condition item Type."
            );

            CreateCombobox(
                "TriggerConditionItemId",
                "Condition Item Id",
                (block) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                        return isWorkingEnabledAndTriggerSelected.Invoke(block) && selectedTriggerConditionItemType >= 0;
                    return false;
                },
                (block) =>
                {
                    var system = GetSystem(block);
                    if (system == null) return 0;
                    else return selectedTriggerConditionItemId;
                },
                (block, value) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                    {
                        selectedTriggerConditionItemId = (int)value;
                    }
                },
                (list) =>
                {
                    if (PhysicalItemTypes.Values.Any(x => x.Index == selectedTriggerConditionItemType))
                    {
                        var typeToUse = PhysicalItemTypes.Values.FirstOrDefault(x => x.Index == selectedTriggerConditionItemType);
                        list.AddRange(typeToUse.Items.Values.OrderBy(x => x.Index).Select(x => x.ComboBoxItem));
                    }
                },
                tooltip: "Select a trigger condition item Id."
            );

            CreateCombobox(
                "TriggerConditionOperationType",
                "Operation Type",
                isWorkingEnabledAndTriggerSelected,
                (block) =>
                {
                    var system = GetSystem(block);
                    if (system == null) return 0;
                    else return selectedTriggerConditionOperationType;
                },
                (block, value) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                    {
                        selectedTriggerConditionOperationType = (int)value;
                        UpdateVisual(block);
                    }
                },
                (list) =>
                {
                    list.Add(new MyTerminalControlComboBoxItem() { Key = 0, Value = MyStringId.GetOrCompute("Greater") });
                    list.Add(new MyTerminalControlComboBoxItem() { Key = 1, Value = MyStringId.GetOrCompute("Less") });
                },
                tooltip: "Select a trigger condition operation type."
            );

            CreateSlider(
                "SliderTriggerConditionValue",
                "Condition Amount",
                isWorkingEnabledAndTriggerSelected,
                (block) =>
                {
                    var system = GetSystem(block);
                    return system != null ? selectedTriggerConditionValue : MIN_CONDITION_VALUE;
                },
                (block, value) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                    {
                        selectedTriggerConditionValue = value;
                    }
                },
                (block, val) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                    {
                        val.Append((int)selectedTriggerConditionValue);
                    }
                },
                new VRageMath.Vector2(MIN_CONDITION_VALUE, MAX_CONDITION_VALUE),
                tooltip: "Set a trigger condition amount value."
            );

            /* Button Add Trigger */
            CreateTerminalButton(
                "AddTriggerCondition",
                "Add Condition",
                isWorkingEnabledAndTriggerSelected,
                (block) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                    {
                        if (system.Settings.GetTriggers().ContainsKey(system.Settings.SelectedTriggerId))
                        {
                            var typeToUse = PhysicalItemTypes.Values.FirstOrDefault(x => x.Index == selectedTriggerConditionItemType);
                            if (typeToUse != null)
                            {
                                var itemToUse = typeToUse.Items.Values.FirstOrDefault(x => x.Index == selectedTriggerConditionItemId);
                                if (itemToUse != null)
                                {
                                    var targetTrigger = system.Settings.GetTriggers()[system.Settings.SelectedTriggerId];
                                    var cond = new AIRefineryControllerTriggerConditionSettings()
                                    {
                                        QueryType = selectedTriggerConditionQueryType,
                                        Id = itemToUse.Id,
                                        OperationType = selectedTriggerConditionOperationType,
                                        Value = (int)selectedTriggerConditionValue,
                                        Index = targetTrigger.Conditions.Any() ? targetTrigger.Conditions.Max(x => x.Index) + 1 : 1
                                    };
                                    targetTrigger.Conditions.Add(cond);
                                    var data = $"{cond.QueryType};{cond.Id};{cond.OperationType};{cond.Value};{cond.Index}";
                                    system.SendToServer("Conditions", "ADD", data, system.Settings.SelectedTriggerId.ToString());
                                    UpdateVisual(block);
                                }
                            }
                        }
                    }
                }
            );

            CreateListbox(
                "TriggerConditionList",
                "Selected Trigger Conditions",
                isWorkingEnabledAndTriggerSelected,
                (block, list, selectedList) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                    {
                        if (system.Settings.GetTriggers().ContainsKey(system.Settings.SelectedTriggerId))
                        {
                            var targetTrigger = system.Settings.GetTriggers()[system.Settings.SelectedTriggerId];
                            foreach (var condition in targetTrigger.Conditions)
                            {
                                if (PhysicalItemIds.ContainsKey(condition.Id))
                                {
                                    var itemToUse = PhysicalItemIds[condition.Id];
                                    var query = condition.QueryType == 0 ? "AND" : "OR";
                                    var operation = condition.OperationType == 0 ? ">" : "<";
                                    var desc = $"{query} [{itemToUse.DisplayText}] {operation} [{condition.Value}]";
                                    var item = new MyTerminalControlListBoxItem(MyStringId.GetOrCompute(desc), MyStringId.GetOrCompute(desc), (object)condition.Index);
                                    list.Add(item);
                                    if (condition.Index == system.Settings.SelectedTriggerConditionIndex)
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
                        system.Settings.SelectedTriggerConditionIndex = (int)selectedList[0].UserData;
                        UpdateVisual(block);
                    }
                },
                tooltip: "List of the conditions of selected trigger."
            );

            /* Button Add Trigger */
            CreateTerminalButton(
                "DelConditionTrigger",
                "Remove Selected Condition",
                (block) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                    {
                        if (isWorkingEnabledAndTriggerSelected.Invoke(block))
                        {
                            var targetTrigger = system.Settings.GetTriggers()[system.Settings.SelectedTriggerId];
                            return targetTrigger.Conditions.Any(x => x.Index == system.Settings.SelectedTriggerConditionIndex);
                        }
                    }
                    return false;
                },
                (block) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                    {
                        if (system.Settings.GetTriggers().ContainsKey(system.Settings.SelectedTriggerId))
                        {
                            var targetTrigger = system.Settings.GetTriggers()[system.Settings.SelectedTriggerId];
                            var condition = targetTrigger.Conditions.FirstOrDefault(x => x.Index == system.Settings.SelectedTriggerConditionIndex);
                            if (condition != null)
                            {
                                targetTrigger.Conditions.Remove(condition);
                                system.SendToServer("Conditions", "DEL", system.Settings.SelectedTriggerConditionIndex.ToString(), system.Settings.SelectedTriggerId.ToString());
                                UpdateVisual(block);
                            }
                        }
                    }
                }
            );

            CreateTerminalLabel("SelectedTriggerActionsLabel", "Selected Trigger Priority");

            CreateCombobox(
                "FilterTriggerItemId",
                "Filter Ore Id",
                isWorkingEnabledAndTriggerSelected,
                (block) =>
                {
                    var system = GetSystem(block);
                    if (system == null) return 0;
                    else return selectedTriggerFilterItemId;
                },
                (block, value) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                    {
                        selectedTriggerFilterItemId = (int)value;
                    }
                },
                (list) =>
                {
                    list.AddRange(validIdsUI);
                },
                tooltip: "Select a filter ore Id."
            );

            /* Button Add Filter */
            CreateTerminalButton(
                "AddedSelectedTriggerPriority",
                "Added Selected Ore",
                isWorkingEnabledAndTriggerSelected,
                (block) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                    {
                        if (system.Settings.GetTriggers().ContainsKey(system.Settings.SelectedTriggerId))
                        {
                            var targetTrigger = system.Settings.GetTriggers()[system.Settings.SelectedTriggerId];
                            var idToUse = validIds[selectedTriggerFilterItemId];
                            if (!targetTrigger.Contains(idToUse.SubtypeName))
                            {
                                targetTrigger.AddPriority(idToUse.SubtypeName);
                                system.SendToServer("TRIGGERORES", "ADD", idToUse.SubtypeName, system.Settings.SelectedTriggerId.ToString());
                                UpdateVisual(block);
                            }
                        }
                    }
                }
            );

            CreateListbox(
                "TriggerOresList",
                "Trigger Ore Priority",
                isWorkingEnabledAndTriggerSelected,
                (block, list, selectedList) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                    {
                        if (system.Settings.GetTriggers().ContainsKey(system.Settings.SelectedTriggerId))
                        {
                            var targetTrigger = system.Settings.GetTriggers()[system.Settings.SelectedTriggerId];
                            foreach (var ore in targetTrigger.GetAll())
                            {
                                var item = new MyTerminalControlListBoxItem(MyStringId.GetOrCompute(ore), MyStringId.GetOrCompute(ore), ore);
                                list.Add(item);
                                if (ore == system.Settings.SelectedTriggerOre)
                                    selectedList.Add(item);
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
                        system.Settings.SelectedTriggerOre = selectedList[0].UserData.ToString();
                        UpdateVisual(block);
                    }
                },
                tooltip: "List of the ore priority to selected trigger."
            );

            /* Button Move Up */
            CreateTerminalButton(
                "MoveUpSelectedTriggerPriority",
                "Move Up Selected Ore",
                isWorkingEnabledAndTriggerSelected,
                (block) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                    {
                        if (system.Settings.GetTriggers().ContainsKey(system.Settings.SelectedTriggerId))
                        {
                            var targetTrigger = system.Settings.GetTriggers()[system.Settings.SelectedTriggerId];
                            if (targetTrigger.Contains(system.Settings.SelectedTriggerOre))
                            {
                                targetTrigger.MoveUp(system.Settings.SelectedTriggerOre);
                                system.SendToServer("TRIGGERORES", "UP", system.Settings.SelectedTriggerOre, system.Settings.SelectedTriggerId.ToString());
                                UpdateVisual(block);
                            }
                        }
                    }
                }
            );

            /* Button Move Down */
            CreateTerminalButton(
                "MoveDownSelectedTriggerPriority",
                "Move Down Selected Ore",
                isWorkingEnabledAndTriggerSelected,
                (block) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                    {
                        if (system.Settings.GetTriggers().ContainsKey(system.Settings.SelectedTriggerId))
                        {
                            var targetTrigger = system.Settings.GetTriggers()[system.Settings.SelectedTriggerId];
                            if (targetTrigger.Contains(system.Settings.SelectedTriggerOre))
                            {
                                targetTrigger.MoveDown(system.Settings.SelectedTriggerOre);
                                system.SendToServer("TRIGGERORES", "DOWN", system.Settings.SelectedTriggerOre, system.Settings.SelectedTriggerId.ToString());
                                UpdateVisual(block);
                            }
                        }
                    }
                }
            );

            /* Button Remove */
            CreateTerminalButton(
                "RemoveSelectedTriggerPriority",
                "Remove Selected Ore",
                isWorkingEnabledAndTriggerSelected,
                (block) =>
                {
                    var system = GetSystem(block);
                    if (system != null)
                    {
                        if (system.Settings.GetTriggers().ContainsKey(system.Settings.SelectedTriggerId))
                        {
                            var targetTrigger = system.Settings.GetTriggers()[system.Settings.SelectedTriggerId];
                            if (targetTrigger.Contains(system.Settings.SelectedTriggerOre))
                            {
                                targetTrigger.RemovePriority(system.Settings.SelectedTriggerOre);
                                system.SendToServer("TRIGGERORES", "DEL", system.Settings.SelectedTriggerOre, system.Settings.SelectedTriggerId.ToString());
                                UpdateVisual(block);
                            }
                        }
                    }
                }
            );

        }

        protected override string GetActionPrefix()
        {
            return "AIRefineryController";
        }

        private readonly string[] idsToRemove = new string[] { "Range", "BroadcastUsingAntennas", "CustomData" };
        protected override string[] GetIdsToRemove()
        {
            return idsToRemove;
        }

    }

}