﻿using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;

namespace AILogisticsAutomation
{

    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public class AILogisticsAutomationSession : BaseSessionComponent
    {

        public const ushort NETWORK_ID_STATSSYSTEM = 45622;
        public const ushort NETWORK_ID_COMMANDS = 45623;
        public const ushort NETWORK_ID_DEFINITIONS = 45624;
        public const ushort NETWORK_ID_ENTITYCALLS = 45625;
        public const string CALL_FOR_DEFS = "NEEDDEFS";

        public static AILogisticsAutomationSession Static { get; private set; }

        public const string ES_TECHNOLOGY_LOCALNAME = "SEExtendedSurvival-Technology";
        public const string ES_STATS_LOCALNAME = "SEExtendedSurvival-Stats";

        public const ulong ES_TECHNOLOGY_MODID = 2842844421;
        public const ulong ES_STATS_EFFECTS_MODID = 2840924715;
        public const ulong WC_MODID = 1918681825;
        public const ulong OREDETECTOR_REFORGED_MODID = 2790047923;

        private static bool? isUsingTechnology = null;
        public static bool IsUsingTechnology()
        {
            if (!isUsingTechnology.HasValue)
                isUsingTechnology = MyAPIGateway.Session.Mods.Any(x => x.PublishedFileId == ES_TECHNOLOGY_MODID || x.Name == ES_TECHNOLOGY_LOCALNAME);
            return isUsingTechnology.Value;
        }

        private static bool? isUsingStatsAndEffects = null;
        public static bool IsUsingStatsAndEffects()
        {
            if (!isUsingStatsAndEffects.HasValue)
                isUsingStatsAndEffects = MyAPIGateway.Session.Mods.Any(x => x.PublishedFileId == ES_STATS_EFFECTS_MODID || x.Name == ES_STATS_LOCALNAME);
            return isUsingStatsAndEffects.Value;
        }

        public static bool IsUsingExtendedSurvival()
        {
            return IsUsingTechnology() || IsUsingStatsAndEffects();
        }

        private static bool? isUsingWeaponCore = null;
        public static bool IsUsingWeaponCore()
        {
            if (!isUsingWeaponCore.HasValue)
                isUsingWeaponCore = MyAPIGateway.Session.Mods.Any(x => x.PublishedFileId == WC_MODID);
            return isUsingWeaponCore.Value;
        }

        private static bool? isUsingOreDetectorReforge = null;
        public static bool IsUsingOreDetectorReforge()
        {
            if (!isUsingOreDetectorReforge.HasValue)
                isUsingOreDetectorReforge = MyAPIGateway.Session.Mods.Any(x => x.PublishedFileId == OREDETECTOR_REFORGED_MODID);
            return isUsingOreDetectorReforge.Value;
        }

        public NanobotAPI NanobotAPI;
        public ExtendedSurvivalCoreAPI ESCoreAPI;
        public WcApi WeaponCore;

        protected override void DoInit(MyObjectBuilder_SessionComponent sessionComponent)
        {

            Static = this;

            if (!IsDedicated)
            {
                MyAPIGateway.Utilities.MessageEntered += OnMessageEntered;
            }

            if (IsServer)
            {

                MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(NETWORK_ID_COMMANDS, CommandsMsgHandler);
                MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(NETWORK_ID_DEFINITIONS, ClientDefinitionsUpdateServerMsgHandler);

            }
            else
            {

                MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(NETWORK_ID_DEFINITIONS, ClientDefinitionsUpdateMsgHandler);
                Command cmd = new Command(MyAPIGateway.Multiplayer.MyId, CALL_FOR_DEFS);
                string message = MyAPIGateway.Utilities.SerializeToXML<Command>(cmd);
                MyAPIGateway.Multiplayer.SendMessageToServer(
                    NETWORK_ID_DEFINITIONS,
                    Encoding.Unicode.GetBytes(message)
                );

            }
            
            if (!AIInventoryManagerBlockTerminal.Controller.CustomControlsInit)
                AIInventoryManagerBlockTerminal.InitializeControls();
                        
            if (!AIRefineryControllerBlockTerminal.Controller.CustomControlsInit)
                AIRefineryControllerBlockTerminal.InitializeControls();

            if (!AIAssemblerControllerBlockTerminal.Controller.CustomControlsInit)
                AIAssemblerControllerBlockTerminal.InitializeControls();

            if (!AIDisplayMonitorBlockTerminal.Controller.CustomControlsInit)
                AIDisplayMonitorBlockTerminal.InitializeControls();

            if (!AIIgnoreMapBlockTerminal.Controller.CustomControlsInit)
                AIIgnoreMapBlockTerminal.InitializeControls();

            if (!AIQuotaMapBlockTerminal.Controller.CustomControlsInit)
                AIQuotaMapBlockTerminal.InitializeControls();

        }

        private const string SETTINGS_COMMAND = "settings";

        private static readonly Dictionary<string, KeyValuePair<int, bool>> VALID_COMMANDS = new Dictionary<string, KeyValuePair<int, bool>>()
        {
            { SETTINGS_COMMAND, new KeyValuePair<int, bool>(3, false) }
        };

        private void CommandsMsgHandler(ushort netId, byte[] data, ulong steamId, bool fromServer)
        {
            try
            {
                var message = Encoding.Unicode.GetString(data);
                var mCommandData = MyAPIGateway.Utilities.SerializeFromXML<Command>(message);
                if (MyAPIGateway.Session.IsUserAdmin(steamId))
                {
                    if (VALID_COMMANDS.ContainsKey(mCommandData.content[0]))
                    {
                        if ((!VALID_COMMANDS[mCommandData.content[0]].Value && mCommandData.content.Length == VALID_COMMANDS[mCommandData.content[0]].Key) ||
                            (VALID_COMMANDS[mCommandData.content[0]].Value && mCommandData.content.Length >= VALID_COMMANDS[mCommandData.content[0]].Key))
                        {
                            switch (mCommandData.content[0])
                            {
                                case SETTINGS_COMMAND:
                                    if (AILogisticsAutomationSettings.Instance.SetConfigValue(mCommandData.content[1], mCommandData.content[2]))
                                    {
                                        ShowMessage($"[AILogisticsAutomation] Config {mCommandData.content[1]} set to {mCommandData.content[2]}.", MyFontEnum.White);
                                    }
                                    break;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                AILogisticsAutomationLogging.Instance.LogError(GetType(), ex);
            }
        }

        private IMyHudNotification hudMsg;
        public void ShowMessage(string text, string font = MyFontEnum.Red, int timeToLive = 5000)
        {
            if (hudMsg == null)
                hudMsg = MyAPIGateway.Utilities.CreateNotification(string.Empty);

            hudMsg.Hide();
            hudMsg.Font = font;
            hudMsg.AliveTime = timeToLive;
            hudMsg.Text = text;
            hudMsg.Show();
        }

        private void OnMessageEntered(string messageText, ref bool sendToOthers)
        {
            sendToOthers = true;
            if (!messageText.StartsWith("/")) return;
            var words = messageText.Trim().ToLower().Replace("/", "").Split(' ');
            if (words.Length > 0)
            {
                if (VALID_COMMANDS.ContainsKey(words[0]))
                {
                    if ((!VALID_COMMANDS[words[0]].Value && words.Length == VALID_COMMANDS[words[0]].Key) ||
                        (VALID_COMMANDS[words[0]].Value && words.Length >= VALID_COMMANDS[words[0]].Key))
                    {
                        sendToOthers = false;
                        Command cmd = new Command(MyAPIGateway.Multiplayer.MyId, words);
                        string message = MyAPIGateway.Utilities.SerializeToXML<Command>(cmd);
                        MyAPIGateway.Multiplayer.SendMessageToServer(
                            NETWORK_ID_COMMANDS,
                            Encoding.Unicode.GetBytes(message)
                        );
                    }
                }
            }
        }

        private void ClientDefinitionsUpdateServerMsgHandler(ushort netId, byte[] data, ulong steamId, bool fromServer)
        {
            try
            {
                if (netId != NETWORK_ID_DEFINITIONS)
                    return;

                var message = Encoding.Unicode.GetString(data);
                var mCommandData = MyAPIGateway.Utilities.SerializeFromXML<Command>(message);
                if (IsServer)
                {

                    switch (mCommandData.content[0])
                    {
                        default:
                            Command cmd = new Command(0, CALL_FOR_DEFS);
                            cmd.data = Encoding.Unicode.GetBytes(AILogisticsAutomationSettings.Instance.GetDataToClient());
                            string messageToSend = MyAPIGateway.Utilities.SerializeToXML<Command>(cmd);
                            MyAPIGateway.Multiplayer.SendMessageTo(
                                NETWORK_ID_DEFINITIONS,
                                Encoding.Unicode.GetBytes(messageToSend),
                                mCommandData.sender
                            );
                            break;
                    }

                }

            }
            catch (Exception ex)
            {
                AILogisticsAutomationLogging.Instance.LogError(GetType(), ex);
            }
        }

        private void ClientDefinitionsUpdateMsgHandler(ushort netId, byte[] data, ulong steamId, bool fromServer)
        {
            try
            {
                if (netId != NETWORK_ID_DEFINITIONS)
                    return;

                var message = Encoding.Unicode.GetString(data);
                var mCommandData = MyAPIGateway.Utilities.SerializeFromXML<Command>(message);
                if (IsClient)
                {
                    var settingsData = Encoding.Unicode.GetString(mCommandData.data);
                    AILogisticsAutomationSettings.ClientLoad(settingsData);
                    CheckDefinitions();
                }

            }
            catch (Exception ex)
            {
                AILogisticsAutomationLogging.Instance.LogError(GetType(), ex);
            }
        }

        public override void SaveData()
        {
            base.SaveData();

            if (IsServer)
            {
                AILogisticsAutomationSettings.Save();
                AILogisticsAutomationStorage.Save();
            }
        }

        private static List<Action> InvokeAfterCoreApiLoaded = new List<Action>();
        public static void AddToInvokeAfterCoreApiLoaded(Action action)
        {
            if (!ExtendedSurvivalCoreAPI.Registered)
                InvokeAfterCoreApiLoaded.Add(action);
        }

        public override void BeforeStart()
        {
            if (IsUsingWeaponCore())
            {
                WeaponCore = new WcApi();
                WeaponCore.Load(() =>
                {
                    var guns = new List<MyDefinitionId>();
                    var turrets = new List<MyDefinitionId>();
                    WeaponCore.GetAllCoreWeapons(guns);
                    WeaponCore.GetAllCoreTurrets(turrets);
                    MyDefinitionIdUtils.DoLoadWC(guns, turrets);
                }, true);
            }
            base.BeforeStart();
        }

        public override void LoadData()
        {
            NanobotAPI = new NanobotAPI();

            ESCoreAPI = new ExtendedSurvivalCoreAPI(() =>
            {
                if (IsServer)
                {
                    if (ExtendedSurvivalCoreAPI.Registered)
                    {
                        if (InvokeAfterCoreApiLoaded.Any())
                        {
                            foreach (var action in InvokeAfterCoreApiLoaded)
                            {
                                action.Invoke();
                            }
                        }
                        DoLoadDefs();
                    }
                }
                else
                {
                    if (ExtendedSurvivalCoreAPI.Registered)
                    {
                        DoLoadDefs();
                    }
                }
            });

            if (IsServer)
            {
                AILogisticsAutomationSettings.Load();
                AILogisticsAutomationStorage.Load();
                CheckDefinitions();
            }

            base.LoadData();
        }

        private void DoLoadDefs()
        {
            if (ExtendedSurvivalCoreAPI.IsMarkAsAllItensLoaded())
            {
                AIAssemblerControllerBlockTerminal.Controller.DoLoadItensIds();
                AIRefineryControllerBlockTerminal.Controller.LoadItensIds();
            }
            else
                ExtendedSurvivalCoreAPI.AddCallBackWhenMarkAsAllItensLoaded(() =>
                {
                    AIAssemblerControllerBlockTerminal.Controller.DoLoadItensIds();
                    AIRefineryControllerBlockTerminal.Controller.LoadItensIds();
                });
        }

        private bool definitionsChecked = false;
        private bool definitionsCheckedToTheEnd = false;

        private void CheckDefinitions()
        {
            AILogisticsAutomationLogging.Instance.LogInfo(GetType(), $"CheckDefinitions Called");
            if (!definitionsChecked)
            {
                definitionsChecked = true;

                definitionsCheckedToTheEnd = true;
            }
        }

        protected override void UnloadData()
        {
            NanobotAPI.Unregister();
            ESCoreAPI.Unregister();

            if (IsServer)
            {
                MyAPIGateway.Multiplayer.UnregisterSecureMessageHandler(NETWORK_ID_COMMANDS, CommandsMsgHandler);
                MyAPIGateway.Multiplayer.UnregisterSecureMessageHandler(NETWORK_ID_DEFINITIONS, ClientDefinitionsUpdateServerMsgHandler);
            }
            else
            {
                MyAPIGateway.Multiplayer.UnregisterSecureMessageHandler(NETWORK_ID_DEFINITIONS, ClientDefinitionsUpdateMsgHandler);
            }

            base.UnloadData();
        }

    }

}
