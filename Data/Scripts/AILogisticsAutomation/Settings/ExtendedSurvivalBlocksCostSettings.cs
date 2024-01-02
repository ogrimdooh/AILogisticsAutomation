using ProtoBuf;
using System.Xml;
using System.Xml.Serialization;

namespace AILogisticsAutomation
{
    [ProtoContract(SkipConstructor = true, UseProtoMembersOnly = true)]
    public class ExtendedSurvivalBlocksCostSettings
    {

        [XmlElement]
        public float FillRefrigeratorCost { get; set; } = 0.015f;

        [XmlElement]
        public float FillFishTrapCost { get; set; } = 0.015f;

        [XmlElement]
        public float FillComposterCost { get; set; } = 0.015f;

        [XmlElement]
        public float FillFarmCost { get; set; } = 0.015f;

    }

}
