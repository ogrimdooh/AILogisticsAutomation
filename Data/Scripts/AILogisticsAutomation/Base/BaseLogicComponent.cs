using Sandbox.Definitions;
using Sandbox.Game.Entities;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ObjectBuilders;

namespace AILogisticsAutomation
{

    public interface IMySyncDataComponent
    {

        void CallFromServer(string method, CommandExtraParams extraParams);
        void CallFromClient(ulong caller, string method, CommandExtraParams extraParams);

    }

    public abstract class BaseLogicComponent<T> : MyGameLogicComponent, IMySyncDataComponent where T : IMyCubeBlock
    {

        public enum EmissiveState
        {
            Working,
            Disabled,
            Warning,
            Damaged,
            Alternative,
            Locked,
            Autolock,
            Constraint
        }

        public bool IsServer
        {
            get
            {
                return MyAPIGateway.Multiplayer.IsServer;
            }
        }

        public bool IsClient
        {
            get
            {
                return !IsServer;
            }
        }

        public bool IsDedicated
        {
            get
            {
                return MyAPIGateway.Utilities.IsDedicated;
            }
        }

        private bool _IsInit;
        protected bool IsInit
        {
            get
            {
                return _IsInit;
            }
        }

        private T _Entity;
        public T CurrentEntity
        {
            get
            {
                return _Entity;
            }
        }

        public float CurrentPower
        {
            get
            {
                return CurrentEntity.ResourceSink.CurrentInputByType(MyResourceDistributorComponent.ElectricityId);
            }
        }

        public float RequiredPower
        {
            get
            {
                return CurrentEntity.ResourceSink.RequiredInputByType(MyResourceDistributorComponent.ElectricityId);
            }
        }

        public bool IsPowered
        {
            get
            {
                return CurrentPower == RequiredPower;
            }
        }

        protected IMyCubeGrid Grid
        {
            get
            {
                if (CurrentEntity != null)
                    return CurrentEntity.CubeGrid;
                return null;
            }
        }

        private MyCubeBlockDefinition _blockDefinition;
        protected MyCubeBlockDefinition BlockDefinition
        {
            get
            {
                if (_blockDefinition == null)
                    _blockDefinition = MyDefinitionManager.Static.GetCubeBlockDefinition(CurrentEntity.BlockDefinition);
                return _blockDefinition;
            }
        }

        protected abstract void OnInit(MyObjectBuilder_EntityBase objectBuilder);

        protected virtual void OnUpdateAfterSimulation100()
        {

        }

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            base.Init(objectBuilder);
            AILogisticsAutomationLogging.Instance.LogInfo(GetType(), "Init");
            _Entity = (T)Entity;
            DoInit(objectBuilder);
        }

        public override void UpdateAfterSimulation100()
        {
            base.UpdateAfterSimulation100();
            try
            {
                OnUpdateAfterSimulation100();
            }
            catch (System.Exception ex)
            {
                AILogisticsAutomationLogging.Instance.LogError(GetType(), ex);
            }
        }

        private void DoInit(MyObjectBuilder_EntityBase objectBuilder)
        {
            if (_IsInit) return;
            var terminalBlock = CurrentEntity as IMyTerminalBlock;
            if (terminalBlock != null)
            {
                terminalBlock.AppendingCustomInfo += TerminalBlock_AppendingCustomInfo;
            }
            OnInit(objectBuilder);
            _IsInit = true;
        }

        protected virtual void OnAppendingCustomInfo(StringBuilder sb)
        {

        }

        private void TerminalBlock_AppendingCustomInfo(IMyTerminalBlock block, StringBuilder sb)
        {
            sb.Append('-', 30).Append('\n');
            sb.Append("AI Logistics Automation").Append('\n');
            sb.Append('-', 30).Append('\n');
            sb.Append("Startup: ").Append(_IsInit ? "Initialized" : "Pending").Append('\n');
            OnAppendingCustomInfo(sb);
            sb.Append('-', 30).Append('\n');
        }

        protected void StoreValue(string key, string value)
        {
            Guid guidKey;
            if (Guid.TryParse(key, out guidKey))
            {
                if (CurrentEntity.Storage == null)
                    CurrentEntity.Storage = new MyModStorageComponent();
                if (!CurrentEntity.Storage.ContainsKey(guidKey))
                    CurrentEntity.Storage.Add(guidKey, value);
                else
                    CurrentEntity.Storage.SetValue(guidKey, value);
            }
        }

        protected string GetValue(string key)
        {
            Guid guidKey;
            if (Guid.TryParse(key, out guidKey))
            {
                if (CurrentEntity.Storage == null)
                    CurrentEntity.Storage = new MyModStorageComponent();
                if (CurrentEntity.Storage.ContainsKey(guidKey))
                    return CurrentEntity.Storage.GetValue(guidKey);
            }
            return "";
        }

        protected void SendCallClient(ulong caller, string method, Dictionary<string, string> extraParams)
        {
            var cmd = new Command(caller, CurrentEntity.EntityId.ToString(), GetType().Name, method);
            var extraData = new CommandExtraParams() { extraParams = extraParams.Select(x => new CommandExtraParam() { id = x.Key, data = x.Value }).ToArray() };
            string extraDataToSend = MyAPIGateway.Utilities.SerializeToXML<CommandExtraParams>(extraData);
            cmd.data = Encoding.Unicode.GetBytes(extraDataToSend);
            string messageToSend = MyAPIGateway.Utilities.SerializeToXML<Command>(cmd);
            MyAPIGateway.Multiplayer.SendMessageToServer(
                AILogisticsAutomationSession.NETWORK_ID_ENTITYCALLS,
                Encoding.Unicode.GetBytes(messageToSend)
            );
        }

        protected void SendCallServer(ulong[] target, string method, Dictionary<string, string> extraParams)
        {
            if (IsDedicated && !target.Any())
            {
                var players = new List<IMyPlayer>();
                MyAPIGateway.Players.GetPlayers(players);
                if (players.Any())
                    target = players.Select(x => x.SteamUserId).ToArray();
                else
                    return;
            }
            var cmd = new Command(0, CurrentEntity.EntityId.ToString(), GetType().Name, method);
            var extraData = new CommandExtraParams() { extraParams = extraParams.Select(x => new CommandExtraParam() { id = x.Key, data = x.Value }).ToArray() };
            string extraDataToSend = MyAPIGateway.Utilities.SerializeToXML<CommandExtraParams>(extraData);
            cmd.data = Encoding.Unicode.GetBytes(extraDataToSend);
            string messageToSend = MyAPIGateway.Utilities.SerializeToXML<Command>(cmd);
            if (!target.Any())
            {
                MyAPIGateway.Multiplayer.SendMessageToOthers(
                    AILogisticsAutomationSession.NETWORK_ID_ENTITYCALLS,
                    Encoding.Unicode.GetBytes(messageToSend)
                );
            }
            else
            {
                foreach (var item in target)
                {
                    MyAPIGateway.Multiplayer.SendMessageTo(
                        AILogisticsAutomationSession.NETWORK_ID_ENTITYCALLS,
                        Encoding.Unicode.GetBytes(messageToSend),
                        item
                    );
                }
            }
        }

        public virtual void CallFromClient(ulong caller, string method, CommandExtraParams extraParams)
        {

        }

        public virtual void CallFromServer(string method, CommandExtraParams extraParams)
        {

        }

        protected void InvokeOnGameThread(Action action, bool wait = true)
        {
            bool isExecuting = true;
            MyAPIGateway.Utilities.InvokeOnGameThread(() =>
            {
                try
                {
                    action.Invoke();
                }
                finally
                {
                    isExecuting = false;
                }
            });
            while (wait && isExecuting)
            {
                if (MyAPIGateway.Parallel != null)
                    MyAPIGateway.Parallel.Sleep(25);
            }
        }

        protected bool SetEmissiveState(EmissiveState state)
        {
            if (CurrentEntity.Render.RenderObjectIDs[0] != uint.MaxValue)
            {
                switch (state)
                {
                    case EmissiveState.Working:
                        (CurrentEntity as MyCubeBlock).SetEmissiveState(MyCubeBlock.m_emissiveNames.Working, CurrentEntity.Render.RenderObjectIDs[0]);
                        return true;
                    case EmissiveState.Disabled:
                        (CurrentEntity as MyCubeBlock).SetEmissiveState(MyCubeBlock.m_emissiveNames.Disabled, CurrentEntity.Render.RenderObjectIDs[0]);
                        return true;
                    case EmissiveState.Warning:
                        (CurrentEntity as MyCubeBlock).SetEmissiveState(MyCubeBlock.m_emissiveNames.Warning, CurrentEntity.Render.RenderObjectIDs[0]);
                        return true;
                    case EmissiveState.Damaged:
                        (CurrentEntity as MyCubeBlock).SetEmissiveState(MyCubeBlock.m_emissiveNames.Damaged, CurrentEntity.Render.RenderObjectIDs[0]);
                        return true;
                    case EmissiveState.Alternative:
                        (CurrentEntity as MyCubeBlock).SetEmissiveState(MyCubeBlock.m_emissiveNames.Alternative, CurrentEntity.Render.RenderObjectIDs[0]);
                        return true;
                    case EmissiveState.Locked:
                        (CurrentEntity as MyCubeBlock).SetEmissiveState(MyCubeBlock.m_emissiveNames.Locked, CurrentEntity.Render.RenderObjectIDs[0]);
                        return true;
                    case EmissiveState.Autolock:
                        (CurrentEntity as MyCubeBlock).SetEmissiveState(MyCubeBlock.m_emissiveNames.Autolock, CurrentEntity.Render.RenderObjectIDs[0]);
                        return true;
                    case EmissiveState.Constraint:
                        (CurrentEntity as MyCubeBlock).SetEmissiveState(MyCubeBlock.m_emissiveNames.Constraint, CurrentEntity.Render.RenderObjectIDs[0]);
                        return true;
                }
            }
            return false;
        }

    }

}
