using ProtoBuf;
using System.Xml;
using System.Xml.Serialization;

namespace AILogisticsAutomation
{
    [ProtoContract(SkipConstructor = true, UseProtoMembersOnly = true)]
    public class EnergyCostSettings
    {

        [XmlElement]
        public float DefaultBlockCost { get; set; } = 0.01f;

        [XmlElement]
        public float DefaultPullCost { get; set; } = 0.025f;

        [XmlElement]
        public float FilterCost { get; set; } = 0.0025f;

        [XmlElement]
        public float SortCost { get; set; } = 0.025f;

        [XmlElement]
        public float StackCost { get; set; } = 0.025f;

        [XmlElement]
        public float FillReactorCost { get; set; } = 0.015f;

        [XmlElement]
        public float FillGasGeneratorCost { get; set; } = 0.015f;

        [XmlElement]
        public float FillBottlesCost { get; set; } = 0.015f;

        [XmlElement]
        public ExtendedSurvivalBlocksCostSettings ExtendedSurvival { get; set; } = new ExtendedSurvivalBlocksCostSettings();

    }

}
