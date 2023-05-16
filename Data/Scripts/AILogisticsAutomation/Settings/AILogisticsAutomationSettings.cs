using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;
using VRage.Utils;
using VRageMath;

namespace AILogisticsAutomation
{

    [ProtoContract(SkipConstructor = true, UseProtoMembersOnly = true)]
    public class AILogisticsAutomationSettings : BaseSettings
    {

        private const int CURRENT_VERSION = 1;
        private const string FILE_NAME = "AILogisticsAutomation.Settings.xml";

        private static AILogisticsAutomationSettings _instance;
        public static AILogisticsAutomationSettings Instance
        {
            get
            {
                if (_instance == null)
                    _instance = Load();
                return _instance;
            }
        }

        private static bool Validate(AILogisticsAutomationSettings settings)
        {
            var res = true;
            return res;
        }

        public static AILogisticsAutomationSettings Load()
        {
            _instance = Load(FILE_NAME, CURRENT_VERSION, Validate, () => { return new AILogisticsAutomationSettings(); });
            return _instance;
        }

        public static void ClientLoad(string data)
        {
            _instance = GetData<AILogisticsAutomationSettings>(data);
        }

        public string GetDataToClient()
        {
            return GetData(this);
        }

        public static void Save()
        {
            try
            {
                Save(Instance, FILE_NAME, true);
            }
            catch (Exception ex)
            {
                AILogisticsAutomationLogging.Instance.LogError(typeof(AILogisticsAutomationSettings), ex);
            }
        }

        [XmlElement]
        public bool Debug { get; set; } = false;

        public AILogisticsAutomationSettings()
        {

        }

        protected override void OnAfterLoad()
        {
            base.OnAfterLoad();

        }

        public bool SetConfigValue(string name, string value)
        {
            switch (name)
            {
                case "debug":
                    bool debug;
                    if (bool.TryParse(value, out debug))
                    {
                        Debug = debug;
                        return true;
                    }
                    break;
            }
            return false;
        }

    }

}
