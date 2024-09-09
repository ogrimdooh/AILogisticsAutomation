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
    public class AIIgnoreMapBlockTerminalController : BaseTerminalController<AIIgnoreMapBlock, IMyOreDetector>
    {

        protected long selectedFilterBlockType = 0;

        protected override bool CanAddControls(IMyTerminalBlock block)
        {
            var validSubTypes = new string[] { "AIIgnoreMap", "AIIgnoreMapSmall", "AIIgnoreMapReskin", "AIIgnoreMapReskinSmall" };
            return block.BlockDefinition.TypeId == typeof(MyObjectBuilder_OreDetector) && validSubTypes.Contains(block.BlockDefinition.SubtypeId);
        }

        protected override void DoInitializeControls()
        {

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
                                targetFilter = new MyObjectBuilderType[] { typeof(MyObjectBuilder_CargoContainer), typeof(MyObjectBuilder_Cockpit), typeof(MyObjectBuilder_CryoChamber) };
                                ignoreBlocks = system.Settings.GetIgnoreCargos();
                                break;
                            case 1:
                                targetFilter = new MyObjectBuilderType[] { typeof(MyObjectBuilder_Assembler), typeof(MyObjectBuilder_Refinery), typeof(MyObjectBuilder_Reactor), typeof(MyObjectBuilder_HydrogenEngine), typeof(MyObjectBuilder_OxygenGenerator), typeof(MyObjectBuilder_OxygenTank), typeof(MyObjectBuilder_GasTank), typeof(MyObjectBuilder_Drill), typeof(MyObjectBuilder_ShipGrinder), typeof(MyObjectBuilder_ShipWelder) };
                                ignoreBlocks = system.Settings.GetIgnoreFunctionalBlocks();
                                break;
                            case 2:
                                targetFilter = new MyObjectBuilderType[] { typeof(MyObjectBuilder_ShipConnector) };
                                ignoreBlocks = system.Settings.GetIgnoreConnectors();
                                break;
                        }

                        foreach (var inventory in targetGrid.Inventories.Where(x => targetFilter.Contains(x.BlockDefinition.Id.TypeId)))
                        {
                            if (!ignoreBlocks.Contains(inventory.EntityId))
                            {
                                var ignored = false;
                                var quotaBlock = system.GetAIQuotaMap();
                                if (quotaBlock != null)
                                {
                                    ignored = quotaBlock.Settings.GetQuotas().ContainsKey(inventory.EntityId);
                                }

                                if (ignored)
                                    continue;

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

                            var targetCargoContainerFilter = new MyObjectBuilderType[] { typeof(MyObjectBuilder_CargoContainer), typeof(MyObjectBuilder_Cockpit), typeof(MyObjectBuilder_CryoChamber) };
                            var targetFunctionalFilter = new MyObjectBuilderType[] { typeof(MyObjectBuilder_Assembler), typeof(MyObjectBuilder_Refinery), typeof(MyObjectBuilder_Reactor), typeof(MyObjectBuilder_HydrogenEngine), typeof(MyObjectBuilder_OxygenGenerator), typeof(MyObjectBuilder_OxygenTank), typeof(MyObjectBuilder_GasTank), typeof(MyObjectBuilder_Drill), typeof(MyObjectBuilder_ShipGrinder), typeof(MyObjectBuilder_ShipWelder) };
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

                        var targetCargoContainerFilter = new MyObjectBuilderType[] { typeof(MyObjectBuilder_CargoContainer), typeof(MyObjectBuilder_Cockpit), typeof(MyObjectBuilder_CryoChamber) };
                        var targetFunctionalFilter = new MyObjectBuilderType[] { typeof(MyObjectBuilder_Assembler), typeof(MyObjectBuilder_Refinery), typeof(MyObjectBuilder_Reactor), typeof(MyObjectBuilder_HydrogenEngine), typeof(MyObjectBuilder_OxygenGenerator), typeof(MyObjectBuilder_OxygenTank), typeof(MyObjectBuilder_GasTank), typeof(MyObjectBuilder_Drill), typeof(MyObjectBuilder_ShipGrinder), typeof(MyObjectBuilder_ShipWelder) };
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

        }

        protected override string GetActionPrefix()
        {
            return "AIIgnoreMap";
        }

        private readonly string[] idsToRemove = new string[] { "Range", "BroadcastUsingAntennas", "CustomData" };
        protected override string[] GetIdsToRemove()
        {
            return idsToRemove;
        }

    }

}