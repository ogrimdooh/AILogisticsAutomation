using ProtoBuf;

namespace AILogisticsAutomation
{
    [ProtoContract]
    public class AIAssemblerControllerStockTypeSettingsData
    {

        [ProtoMember(1)]
        public string type;

        [ProtoMember(2)]
        public int amount;

    }

}