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

        public class AssemblerItemInfo
        {

            public MyDefinitionId Id { get; set; }
            public string DisplayText { get; set; }
            public int Index { get; set; }
            public MyPhysicalItemDefinition ItemDefinition { get; set; }
            public MyTerminalControlComboBoxItem ComboBoxItem { get; set; }

        }

        public class AssemblerTypeInfo
        {

            public MyObjectBuilderType Type { get; set; }
            public string DisplayText { get; set; }
            public int Index { get; set; }
            public MyTerminalControlComboBoxItem ComboBoxItem { get; set; }

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

        protected ConcurrentDictionary<MyDefinitionId, AssemblerDefinitionInfo> Assemblers { get; set; } = new ConcurrentDictionary<MyDefinitionId, AssemblerDefinitionInfo>();
        protected ConcurrentDictionary<MyDefinitionId, AssemblerItemInfo> ValidIds { get; set; } = new ConcurrentDictionary<MyDefinitionId, AssemblerItemInfo>();
        protected ConcurrentDictionary<MyObjectBuilderType, AssemblerTypeInfo> ValidTypes { get; set; } = new ConcurrentDictionary<MyObjectBuilderType, AssemblerTypeInfo>();

        protected long selectedFilterType = 0;
        protected long selectedFilterGroup = 0;
        protected int selectedFilterItemType = 0;
        protected int selectedFilterItemId = 0;
        protected long selectedFilterBlockType = 0;

        protected override bool CanAddControls(IMyTerminalBlock block)
        {
            var validSubTypes = new string[] { "AIAssemblerController", "AIAssemblerControllerReskin" };
            return block.BlockDefinition.TypeId == typeof(MyObjectBuilder_OreDetector) && validSubTypes.Contains(block.BlockDefinition.SubtypeId);
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

        public void DoLoadItensIds()
        {
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
                    if (!ValidIds.ContainsKey(id))
                        ValidIds[id] = new AssemblerItemInfo() 
                        { 
                            Id = id,
                            DisplayText = Assemblers[assembler.Id].ValidIds[id].DisplayText,
                            ItemDefinition = Assemblers[assembler.Id].ValidIds[id].ItemDefinition
                        };
                }
                foreach (var id in Assemblers[assembler.Id].ValidTypes.Keys)
                {
                    if (!ValidTypes.ContainsKey(id))
                        ValidTypes[id] = new AssemblerTypeInfo()
                        {
                            Type = id,
                            DisplayText = Assemblers[assembler.Id].ValidTypes[id].DisplayText
                        };
                }
            }
            DoSortAndSetIndex();
            CreateComboBoxItens();
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