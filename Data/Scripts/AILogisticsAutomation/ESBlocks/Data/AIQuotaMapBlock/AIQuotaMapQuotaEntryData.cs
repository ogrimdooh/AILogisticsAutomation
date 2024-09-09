﻿using VRage.ObjectBuilders;
using ProtoBuf;

namespace AILogisticsAutomation
{
    [ProtoContract]
    public class AIQuotaMapQuotaEntryData
    {

        [ProtoMember(1)]
        public DocumentedDefinitionId id;

        [ProtoMember(2)]
        public float value;
        
        [ProtoMember(3)]
        public int index;

    }

}