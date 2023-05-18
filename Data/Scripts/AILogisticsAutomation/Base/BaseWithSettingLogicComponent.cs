using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;

namespace AILogisticsAutomation
{

    public interface IBlockSettings<T>
    {
        T GetData();
        bool UpdateData(string key, string action, string value, string owner);
        void UpdateData(T data);
        float GetPowerConsumption();
        void SetPowerConsumption(float value);
    }

    public abstract class BaseWithSettingLogicComponent<T, S, D> : BaseLogicComponent<T> where T : IMyCubeBlock where S : IBlockSettings<D>
    {

        public S Settings { get; set; }

        protected override void OnInit(MyObjectBuilder_EntityBase objectBuilder)
        {
            if (IsServer)
            {
                LoadSettings();
                CurrentEntity.OnClose += CurrentEntity_OnClose;
            }
            else
            {
                RequestSettings();
            }
        }

        protected virtual void CurrentEntity_OnClose(IMyEntity obj)
        {
            AILogisticsAutomationStorage.Instance.RemoveEntity(CurrentEntity.EntityId);
        }

        protected void ReciveFromServer(string encodeData)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(encodeData))
                {
                    var decodeData = Base64Utils.DecodeFrom64(encodeData);
                    var data = MyAPIGateway.Utilities.SerializeFromXML<D>(decodeData);
                    Settings.UpdateData(data);
                }
            }
            catch (Exception ex)
            {
                AILogisticsAutomationLogging.Instance.LogError(GetType(), ex);
            }
        }

        protected string GetEncodedData()
        {
            try
            {
                var data = Settings.GetData();
                var dataToSend = MyAPIGateway.Utilities.SerializeToXML<D>(data);
                return Base64Utils.EncodeToBase64(dataToSend);
            }
            catch (Exception ex)
            {
                AILogisticsAutomationLogging.Instance.LogError(GetType(), ex);
                return null;
            }
        }

        protected void SendToClient(params ulong[] clientIds)
        {
            try
            {
                if (AILogisticsAutomationSettings.Instance.Debug)
                    AILogisticsAutomationLogging.Instance.LogInfo(GetType(), $"SendToClient: clientId={string.Join(",", clientIds.Select(x => x.ToString()))}");
                var encodeData = GetEncodedData();
                SendCallServer(clientIds, "UpdateSettings", new Dictionary<string, string>() { { "DATA", encodeData } });
            }
            catch (Exception ex)
            {
                AILogisticsAutomationLogging.Instance.LogError(GetType(), ex);
            }
        }

        protected void SendPowerToClient(params ulong[] clientIds)
        {
            try
            {
                if (AILogisticsAutomationSettings.Instance.Debug)
                    AILogisticsAutomationLogging.Instance.LogInfo(GetType(), $"SendPowerToClient: POWER={Settings.GetPowerConsumption()}");
                SendCallServer(clientIds, "UpdatePower", new Dictionary<string, string>() { { "POWER", Settings.GetPowerConsumption().ToString() } });
            }
            catch (Exception ex)
            {
                AILogisticsAutomationLogging.Instance.LogError(GetType(), ex);
            }
        }

        protected void ReciveFromClient(ulong caller, string key, string action, string value, string owner)
        {
            try
            {
                if (AILogisticsAutomationSettings.Instance.Debug)
                    AILogisticsAutomationLogging.Instance.LogInfo(GetType(), $"ReciveFromClient: caller={caller} - key={key} - action={action} - value={value} - owner={owner}");
                if (Settings.UpdateData(key, action, value, owner))
                {
                    SaveSettings();
                    var players = new List<IMyPlayer>();
                    MyAPIGateway.Players.GetPlayers(players);
                    if (players.Any(x => x.SteamUserId != caller))
                    {
                        var ids = players.Where(x => x.SteamUserId != caller).Select(x => x.SteamUserId).ToArray();
                        if (ids.Any())
                        {
                            var changeData = new Dictionary<string, string>()
                            {
                                { "KEY", key },
                                { "ACTION", action },
                                { "VALUE", value },
                                { "OWNER", owner }
                            };
                            SendCallServer(ids, "SetSettings", changeData);
                        }
                    }
                }
                else
                {
                    AILogisticsAutomationLogging.Instance.LogWarning(GetType(), $"Failed to update key={key} : will force configs in client");
                    SendToClient(caller);
                }
            }
            catch (Exception ex)
            {
                AILogisticsAutomationLogging.Instance.LogError(GetType(), ex);
            }
        }

        protected void LoadSettings()
        {
            try
            {
                var storedData = AILogisticsAutomationStorage.Instance.GetEntityValue(CurrentEntity.EntityId, "DATA");
                if (!string.IsNullOrWhiteSpace(storedData))
                {
                    var decodeData = Base64Utils.DecodeFrom64(storedData);
                    var data = MyAPIGateway.Utilities.SerializeFromXML<D>(decodeData);
                    Settings.UpdateData(data);
                }
            }
            catch (Exception ex)
            {
                AILogisticsAutomationLogging.Instance.LogError(GetType(), ex);
            }
        }

        protected void RequestPower()
        {
            try
            {
                SendCallClient(MyAPIGateway.Session.Player.SteamUserId, "RequestPower", new Dictionary<string, string>() { });
            }
            catch (Exception ex)
            {
                AILogisticsAutomationLogging.Instance.LogError(GetType(), ex);
            }
        }

        public void RequestSettings()
        {
            try
            {
                SendCallClient(MyAPIGateway.Session.Player.SteamUserId, "RequestSettings", new Dictionary<string, string>() { });
            }
            catch (Exception ex)
            {
                AILogisticsAutomationLogging.Instance.LogError(GetType(), ex);
            }
        }

        protected void SaveSettings()
        {
            try
            {
                var encodeData = GetEncodedData();
                AILogisticsAutomationStorage.Instance.SetEntityValue(CurrentEntity.EntityId, "DATA", encodeData);
            }
            catch (Exception ex)
            {
                AILogisticsAutomationLogging.Instance.LogError(GetType(), ex);
            }
        }

        public void SendToServer(string key, string action, string value, string owner = null)
        {
            try
            {
                var changeData = new Dictionary<string, string>()
                {
                    { "KEY", key },
                    { "ACTION", action },
                    { "VALUE", value },
                    { "OWNER", owner }
                };
                if (!IsServer)
                {
                    var encodeData = GetEncodedData();
                    SendCallClient(MyAPIGateway.Session.Player.SteamUserId, "SetSettings", changeData);
                }
                else
                {
                    SaveSettings();
                    SendCallServer(new ulong[] { }, "SetSettings", changeData);
                }
            }
            catch (Exception ex)
            {
                AILogisticsAutomationLogging.Instance.LogError(GetType(), ex);
            }
        }

        public override void CallFromClient(ulong caller, string method, CommandExtraParams extraParams)
        {
            base.CallFromClient(caller, method, extraParams);
            try
            {
                if (AILogisticsAutomationSettings.Instance.Debug)
                    AILogisticsAutomationLogging.Instance.LogInfo(GetType(), $"CallFromClient: caller={caller} - method={method}");
                switch (method)
                {
                    case "RequestSettings":
                        SendToClient(caller);
                        break;
                    case "RequestPower":
                        SendPowerToClient(caller);
                        break;
                    case "SetSettings":
                        ReciveFromClient(
                            caller,
                            extraParams.extraParams.FirstOrDefault(x => x.id == "KEY")?.data,
                            extraParams.extraParams.FirstOrDefault(x => x.id == "ACTION")?.data,
                            extraParams.extraParams.FirstOrDefault(x => x.id == "VALUE")?.data,
                            extraParams.extraParams.FirstOrDefault(x => x.id == "OWNER")?.data
                        );
                        break;
                }
            }
            catch (Exception ex)
            {
                AILogisticsAutomationLogging.Instance.LogError(GetType(), ex);
            }
        }

        public override void CallFromServer(string method, CommandExtraParams extraParams)
        {
            base.CallFromServer(method, extraParams);
            try
            {
                switch (method)
                {
                    case "UpdateSettings":
                        ReciveFromServer(extraParams.extraParams.FirstOrDefault(x => x.id == "DATA")?.data);
                        break;
                    case "UpdatePower":
                        var power = float.Parse(extraParams.extraParams.FirstOrDefault(x => x.id == "POWER")?.data);
                        Settings.SetPowerConsumption(power);
                        break;
                    case "SetSettings":
                        Settings.UpdateData(
                            extraParams.extraParams.FirstOrDefault(x => x.id == "KEY")?.data,
                            extraParams.extraParams.FirstOrDefault(x => x.id == "ACTION")?.data,
                            extraParams.extraParams.FirstOrDefault(x => x.id == "VALUE")?.data,
                            extraParams.extraParams.FirstOrDefault(x => x.id == "OWNER")?.data
                        );
                        break;
                }
            }
            catch (Exception ex)
            {
                AILogisticsAutomationLogging.Instance.LogError(GetType(), ex);
            }
        }

    }

}
