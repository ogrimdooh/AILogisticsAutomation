using ProtoBuf;
using System.Xml;
using System.Xml.Serialization;
using VRage.Game;
using VRage.ObjectBuilders;

namespace AILogisticsAutomation
{
    [ProtoContract(SkipConstructor = true, UseProtoMembersOnly = true)]
    public class DocumentedDefinitionId
    {

        [XmlElement]
        public string Type { get; set; }
        [XmlElement]
        public string Subtype { get; set; }

        public DocumentedDefinitionId()
        {

        }

        public DocumentedDefinitionId(MyDefinitionId id)
        {
            Type = id.TypeId.ToString();
            Subtype = id.SubtypeName;
        }

        public MyDefinitionId? GetId()
        {
            MyObjectBuilderType type;
            if (MyObjectBuilderType.TryParse(Type, out type))
            {
                return new MyDefinitionId(type, Subtype);
            }
            return null;
        }

        public override int GetHashCode()
        {
            return (Type + Subtype).GetHashCode();
        }

    }

}
