using ProtoBuf;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using VRage;
using VRage.Game;
using VRage.Game.Components;
using VRage.Utils;
using VRageMath;

namespace AILogisticsAutomation
{

    public class NanobotAPI
    {

        [ProtoContract(SkipConstructor = true, UseProtoMembersOnly = true)]
        public class NanobotTargetState
        {

            [ProtoMember(1)]
            public long GridId { get; set; }

            [ProtoMember(2)]
            public Vector3I BlockPosition { get; set; }

            [ProtoMember(3)]
            public long? BlockId { get; set; }

        }

        [ProtoContract(SkipConstructor = true, UseProtoMembersOnly = true)]
        public class NanobotState
        {

            [ProtoMember(1)]
            public bool Ready { get; set; }

            [ProtoMember(2)]
            public bool Welding { get; set; }

            [ProtoMember(3)]
            public bool NeedWelding { get; set; }

            [ProtoMember(4)]
            public bool Grinding { get; set; }

            [ProtoMember(5)]
            public bool NeedGrinding { get; set; }

            [ProtoMember(6)]
            public bool Transporting { get; set; }

            [ProtoMember(7)]
            public NanobotTargetState CurrentWeldingBlock { get; set; }

            [ProtoMember(8)]
            public NanobotTargetState CurrentGrindingBlock { get; set; }

        }

        private static NanobotAPI instance;

        public static string ModName = "";
        public const ushort ModHandlerID = 60122;
        public const int ModAPIVersion = 1;

        public static bool Registered { get; private set; } = false;

        private static Dictionary<string, Delegate> ModAPIMethods;

        private static Func<int, string, bool> _VerifyVersion;
        private static Func<long, string> _GetBlockState;

        /// <summary>
        /// Returns true if the version is compatibile with the API Backend, this is automatically called
        /// </summary>
        public static bool VerifyVersion(int Version, string ModName) => _VerifyVersion?.Invoke(Version, ModName) ?? false;

        /// <summary>
        /// Returns the state by Block EntityId
        /// </summary>
        public static NanobotState GetBlockState(long entityId)
        {
            var stateMessage = _GetBlockState?.Invoke(entityId);
            if (!string.IsNullOrEmpty(stateMessage))
            {
                try
                {
                    var mCommandData = MyAPIGateway.Utilities.SerializeFromXML<NanobotState>(stateMessage);
                    return mCommandData;
                }
                catch (Exception e)
                {
                    MyLog.Default.WriteLine("NanobotAPI: " + e);
                }
            }
            return null;
        }

        /// <summary>
        /// Unregisters the mod
        /// </summary>
        public void Unregister()
        {
            if (instance != null)
            {
                instance.DoUnregister();
            }
        }

        /// <summary>
        /// Unregisters the mod
        /// </summary>
        public void DoUnregister()
        {
            MyAPIGateway.Utilities.UnregisterMessageHandler(ModHandlerID, ModHandler);
            Registered = false;
            instance = null;
            m_onRegisteredAction = null;
        }

        private Action m_onRegisteredAction;

        /// <summary>
        /// Create a HudAPI Instance. Please only create one per mod. 
        /// </summary>
        /// <param name="onRegisteredAction">Callback once the HudAPI is active. You can Instantiate HudAPI objects in this Action</param>
        public NanobotAPI(Action onRegisteredAction = null)
        {
            if (instance != null)
            {
                return;
            }
            instance = this;
            m_onRegisteredAction = onRegisteredAction;
            MyAPIGateway.Utilities.RegisterMessageHandler(ModHandlerID, ModHandler);
            if (ModName == "")
            {
                if (MyAPIGateway.Utilities.GamePaths.ModScopeName.Contains("_"))
                    ModName = MyAPIGateway.Utilities.GamePaths.ModScopeName.Split('_')[1];
                else
                    ModName = MyAPIGateway.Utilities.GamePaths.ModScopeName;
            }
        }

        private void ModHandler(object obj)
        {
            if (obj == null)
            {
                return;
            }

            if (obj is Dictionary<string, Delegate>)
            {
                ModAPIMethods = (Dictionary<string, Delegate>)obj;
                _VerifyVersion = (Func<int, string, bool>)ModAPIMethods["VerifyVersion"];

                Registered = VerifyVersion(ModAPIVersion, ModName);

                MyLog.Default.WriteLine("Registering NanobotAPI for Mod '" + ModName + "'");

                if (Registered)
                {
                    try
                    {
                        _GetBlockState = (Func<long, string>)ModAPIMethods["GetBlockState"];

                        if (m_onRegisteredAction != null)
                            m_onRegisteredAction();
                    }
                    catch (Exception e)
                    {
                        MyAPIGateway.Utilities.ShowMessage("NanobotAPI", "Mod '" + ModName + "' encountered an error when registering the Nanobot Mod API, see log for more info.");
                        MyLog.Default.WriteLine("NanobotAPI: " + e);
                    }
                }
            }
        }

    }

}