using Sandbox.Game;
using Sandbox.Game.Components;
using Sandbox.Game.Entities;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Weapons;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.Utils;

namespace AILogisticsAutomation
{

    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public class AILogisticsAutomationEntityManager : BaseSessionComponent
    {

        public static AILogisticsAutomationEntityManager Instance { get; private set; }

        protected override void DoInit(MyObjectBuilder_SessionComponent sessionComponent)
        {
            Instance = this;
            if (IsServer)
            {

            }
        }

        protected override void UnloadData()
        {
            try
            {
                MyAPIGateway.Multiplayer.UnregisterSecureMessageHandler(AILogisticsAutomationSession.NETWORK_ID_ENTITYCALLS, EntityCallsMsgHandler);
            }
            catch (Exception ex)
            {
                AILogisticsAutomationLogging.Instance.LogError(GetType(), ex);
            }
            base.UnloadData();
        }

        public override void BeforeStart()
        {
            try
            {
                MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(AILogisticsAutomationSession.NETWORK_ID_ENTITYCALLS, EntityCallsMsgHandler);
            }
            catch (Exception ex)
            {
                AILogisticsAutomationLogging.Instance.LogError(GetType(), ex);
            }
            base.BeforeStart();
        }

        private void EntityCallsMsgHandler(ushort netId, byte[] data, ulong steamId, bool fromServer)
        {
            try
            {
                if (netId != AILogisticsAutomationSession.NETWORK_ID_ENTITYCALLS || (fromServer && IsServer))
                    return;
                var message = Encoding.Unicode.GetString(data);
                var mCommandData = MyAPIGateway.Utilities.SerializeFromXML<Command>(message);
                long entityId = long.Parse(mCommandData.content[0]);
                var entity = MyEntities.GetEntityById(entityId);
                if (entity != null)
                {
                    var blockLogic = entity.GameLogic;
                    if (blockLogic != null)
                    {
                        var compositeLogic = blockLogic as MyCompositeGameLogicComponent;
                        if (compositeLogic != null)
                        {
                            blockLogic = compositeLogic.GetComponents().FirstOrDefault(x => x.GetType().Name.Contains(mCommandData.content[1]));
                        }
                        if (blockLogic != null)
                        {
                            var syncComponent = blockLogic as IMySyncDataComponent;
                            if (syncComponent != null)
                            {
                                var componentParamData = Encoding.Unicode.GetString(mCommandData.data);
                                var componentParams = MyAPIGateway.Utilities.SerializeFromXML<CommandExtraParams>(componentParamData);
                                if (fromServer)
                                    syncComponent.CallFromServer(mCommandData.content[2], componentParams);
                                else
                                    syncComponent.CallFromClient(steamId, mCommandData.content[2], componentParams);
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

    }

}
