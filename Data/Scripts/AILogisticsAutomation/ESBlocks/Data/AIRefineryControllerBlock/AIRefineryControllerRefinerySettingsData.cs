using ProtoBuf;
using VRageMath;

namespace AILogisticsAutomation
{
    [ProtoContract]
    public class AIRefineryControllerRefinerySettingsData
    {

        [ProtoMember(1)]
        public long entityId;

        [ProtoMember(2)]
        public string[] ores = new string[] { };

        [ProtoMember(3)]
        public Vector3I position;

    }

}