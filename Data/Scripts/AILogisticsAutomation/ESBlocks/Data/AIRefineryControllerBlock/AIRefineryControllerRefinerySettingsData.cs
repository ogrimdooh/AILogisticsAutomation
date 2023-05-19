using ProtoBuf;

namespace AILogisticsAutomation
{
    [ProtoContract]
    public class AIRefineryControllerRefinerySettingsData
    {

        [ProtoMember(1)]
        public long entityId;

        [ProtoMember(2)]
        public string[] ores = new string[] { };

    }

}