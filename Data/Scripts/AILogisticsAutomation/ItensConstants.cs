using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Definitions;
using System.Collections.Concurrent;
using System.Collections.Generic;
using VRage.Game;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.ObjectBuilders;

namespace AILogisticsAutomation
{

    public static class ItensConstants
    {

        public const string ORGANIC_SUBTYPEID = "Organic";
        public static readonly UniqueEntityId ORGANIC_ID = new UniqueEntityId(typeof(MyObjectBuilder_Ore), ORGANIC_SUBTYPEID);

        public const string ICE_SUBTYPEID = "Ice";
        public static readonly UniqueEntityId ICE_ID = new UniqueEntityId(typeof(MyObjectBuilder_Ore), ICE_SUBTYPEID);

        public const string FISH_BAIT_SUBTYPEID = "FishBait";
        public static readonly UniqueEntityId FISH_BAIT_SMALL_ID = new UniqueEntityId(typeof(MyObjectBuilder_Ingot), FISH_BAIT_SUBTYPEID);

        public const string FISH_NOBLE_BAIT_SUBTYPEID = "FishNobleBait";
        public static readonly UniqueEntityId FISH_NOBLE_BAIT_ID = new UniqueEntityId(typeof(MyObjectBuilder_Ingot), FISH_NOBLE_BAIT_SUBTYPEID);

        private static ConcurrentDictionary<UniqueEntityId, MyObjectBuilder_Base> BUILDERS_CACHE = new ConcurrentDictionary<UniqueEntityId, MyObjectBuilder_Base>();

        public static T GetBuilder<T>(UniqueEntityId id, bool cache = true) where T : MyObjectBuilder_Base
        {
            if (cache && BUILDERS_CACHE.ContainsKey(id))
                return BUILDERS_CACHE[id] as T;
            var builder = MyObjectBuilderSerializer.CreateNewObject(id.DefinitionId) as T;
            BUILDERS_CACHE[id] = builder;
            return builder as T;
        }

        public static MyObjectBuilder_PhysicalObject GetPhysicalObjectBuilder(UniqueEntityId id)
        {
            return GetBuilder<MyObjectBuilder_PhysicalObject>(id);
        }

        public static MyObjectBuilder_GasContainerObject GetGasContainerBuilder(UniqueEntityId id, float gasLevel = 0)
        {
            var builder = GetBuilder<MyObjectBuilder_GasContainerObject>(id, false);
            builder.GasLevel = gasLevel;
            return builder;
        }

    }

}