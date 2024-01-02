using ProtoBuf;

namespace AILogisticsAutomation
{
    [ProtoContract]
    public class AIRefineryControllerTriggerSettingsData
    {

        [ProtoMember(1)]
        public long triggerId;

        [ProtoMember(2)]
        public string name;

        [ProtoMember(3)]
        public AIAssemblerControllerTriggerConditionSettingsData[] conditions = new AIAssemblerControllerTriggerConditionSettingsData[] { };

        [ProtoMember(4)]
        public string[] ores = new string[] { };

    }

}