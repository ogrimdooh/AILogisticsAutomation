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
    public class AIDisplayMonitorBlockTerminalController : BaseTerminalController<AIDisplayMonitorBlock, IMyOreDetector>
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
            return block.BlockDefinition.TypeId == typeof(MyObjectBuilder_OreDetector) && block.BlockDefinition.SubtypeId == "AIDisplayMonitor";
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

            CreateTerminalLabel("AIMIStartConfig", "NOT IMPLEMENTED YET");

        }

        protected override string GetActionPrefix()
        {
            return "AIDisplayMonitor";
        }

        private readonly string[] idsToRemove = new string[] { "Range", "BroadcastUsingAntennas" };
        protected override string[] GetIdsToRemove()
        {
            return idsToRemove;
        }

    }

}