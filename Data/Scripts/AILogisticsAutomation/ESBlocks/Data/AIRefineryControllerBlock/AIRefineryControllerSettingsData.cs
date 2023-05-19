using ProtoBuf;

namespace AILogisticsAutomation
{

    [ProtoContract]
    public class AIRefineryControllerSettingsData
    {

        [ProtoMember(1)]
        public float powerConsumption;

        [ProtoMember(2)]
        public bool enabled;

        [ProtoMember(3)]
        public string[] ores = new string[] { };

        [ProtoMember(4)]
        public AIRefineryControllerRefinerySettingsData[] definitions = new AIRefineryControllerRefinerySettingsData[] { };
        
        [ProtoMember(5)]
        public long[] ignoreRefinery = new long[] { };
        
    }

}