﻿using ProtoBuf;
using VRage.ObjectBuilders;

namespace AILogisticsAutomation
{
    [ProtoContract]
    public class AIAssemblerControllerTriggerActionSettingsData
    {

        [ProtoMember(1)]
        public DocumentedDefinitionId id;

        [ProtoMember(2)]
        public float value;

        [ProtoMember(3)]
        public int index;

    }

}