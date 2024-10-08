﻿using ProtoBuf;

namespace AILogisticsAutomation
{
    [ProtoContract]
    public class AIQuotaMapSettingsData
    {

        [ProtoMember(1)]
        public float powerConsumption;

        [ProtoMember(2)]
        public bool enabled;

        [ProtoMember(3)]
        public AIQuotaMapQuotaDefinitionData[] quotas = new AIQuotaMapQuotaDefinitionData[] { };

    }

}