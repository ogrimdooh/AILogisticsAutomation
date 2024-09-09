using ProtoBuf;
using VRage.ObjectBuilders;

namespace AILogisticsAutomation
{
    [ProtoContract]
    public class AIAssemblerControllerStockIdSettingsData
    {

        [ProtoMember(1)]
        public DocumentedDefinitionId id;

        [ProtoMember(2)]
        public int amount;

    }

}