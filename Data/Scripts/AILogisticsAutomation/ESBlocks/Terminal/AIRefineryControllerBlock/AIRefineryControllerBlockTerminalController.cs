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

namespace AILogisticsAutomation
{
    public class AIRefineryControllerBlockTerminalController : BaseTerminalController<AIRefineryControllerBlock, IMyOreDetector>
    {

        protected List<MyDefinitionId> validIds = new List<MyDefinitionId>();
        protected List<MyRefineryDefinition> refineries = new List<MyRefineryDefinition>();
        protected List<MyTerminalControlComboBoxItem> validIdsUI = new List<MyTerminalControlComboBoxItem>();

        protected int selectedFilterItemId = 0;
        protected int selectedRefineryFilterItemId = 0;

        protected override bool CanAddControls(IMyTerminalBlock block)
        {
            return block.BlockDefinition.TypeId == typeof(MyObjectBuilder_OreDetector) && block.BlockDefinition.SubtypeId == "AIRefineryController";
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

        protected void LoadItensIds()
        {
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
                            system.Settings.DefaultOres.AddOrePriority(idToUse.SubtypeName);
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
                        foreach (var ore in system.Settings.DefaultOres.GetOres())
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
                            system.Settings.DefaultOres.RemoveOrePriority(system.Settings.SelectedDefaultOre);
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
                                def.Ores.AddOrePriority(idToUse.SubtypeName);
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
                            foreach (var ore in def.Ores.GetOres())
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
                                def.Ores.RemoveOrePriority(system.Settings.SelectedRefineryOre);
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

        }

        protected override string GetActionPrefix()
        {
            return "AIRefineryController";
        }

    }

}