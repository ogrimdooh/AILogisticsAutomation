using ProtoBuf;
using VRage.ObjectBuilders;

namespace AILogisticsAutomation
{
    [ProtoContract]
    public class AIAssemblerControllerStockSettingsData
    {

        [ProtoMember(1)]
        public AIAssemblerControllerStockIdSettingsData[] validIds = new AIAssemblerControllerStockIdSettingsData[] { };

        [ProtoMember(2)]
        public AIAssemblerControllerStockTypeSettingsData[] validTypes = new AIAssemblerControllerStockTypeSettingsData[] { };

        [ProtoMember(3)]
        public SerializableDefinitionId[] ignoreIds = new SerializableDefinitionId[] { };

        [ProtoMember(4)]
        public string[] ignoreTypes = new string[] { };
        
    }

}