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

        public static readonly MyObjectBuilderType[] GAS_TYPES = new MyObjectBuilderType[] 
        {
            typeof(MyObjectBuilder_GasContainerObject),
            typeof(MyObjectBuilder_OxygenContainerObject)
        };

        public const string ORGANIC_SUBTYPEID = "Organic";
        public static readonly UniqueEntityId ORGANIC_ID = new UniqueEntityId(typeof(MyObjectBuilder_Ore), ORGANIC_SUBTYPEID);

        public const string ICE_SUBTYPEID = "Ice";
        public static readonly UniqueEntityId ICE_ID = new UniqueEntityId(typeof(MyObjectBuilder_Ore), ICE_SUBTYPEID);

        public const string FISH_BAIT_SUBTYPEID = "FishBait";
        public static readonly UniqueEntityId FISH_BAIT_SMALL_ID = new UniqueEntityId(typeof(MyObjectBuilder_Ingot), FISH_BAIT_SUBTYPEID);

        public const string FISH_NOBLE_BAIT_SUBTYPEID = "FishNobleBait";
        public static readonly UniqueEntityId FISH_NOBLE_BAIT_ID = new UniqueEntityId(typeof(MyObjectBuilder_Ingot), FISH_NOBLE_BAIT_SUBTYPEID);

        public const string HYDROGENBOTTLE_SUBTYPEID = "HydrogenBottle";
        public static readonly UniqueEntityId HYDROGENBOTTLE_ID = new UniqueEntityId(typeof(MyObjectBuilder_GasContainerObject), HYDROGENBOTTLE_SUBTYPEID);

        public const string OXYGENBOTTLE_SUBTYPEID = "OxygenBottle";
        public static readonly UniqueEntityId OXYGENBOTTLE_ID = new UniqueEntityId(typeof(MyObjectBuilder_OxygenContainerObject), OXYGENBOTTLE_SUBTYPEID);

        public const string OXYGEN_SUBTYPEID = "Oxygen";
        public static readonly UniqueEntityId OXYGEN_ID = new UniqueEntityId(typeof(MyObjectBuilder_GasProperties), OXYGEN_SUBTYPEID);

        public const string HYDROGEN_SUBTYPEID = "Hydrogen";
        public static readonly UniqueEntityId HYDROGEN_ID = new UniqueEntityId(typeof(MyObjectBuilder_GasProperties), HYDROGEN_SUBTYPEID);

        private static ConcurrentDictionary<UniqueEntityId, MyObjectBuilder_Base> BUILDERS_CACHE = new ConcurrentDictionary<UniqueEntityId, MyObjectBuilder_Base>();

        public const string FERTILIZER_SUBTYPEID = "Fertilizer";
        public static readonly UniqueEntityId FERTILIZER_ID = new UniqueEntityId(typeof(MyObjectBuilder_Ore), FERTILIZER_SUBTYPEID);

        public const string MINERALFERTILIZER_SUBTYPEID = "MineralFertilizer";
        public static readonly UniqueEntityId MINERALFERTILIZER_ID = new UniqueEntityId(typeof(MyObjectBuilder_Ore), MINERALFERTILIZER_SUBTYPEID);

        public const string SUPERFERTILIZER_SUBTYPEID = "SuperFertilizer";
        public static readonly UniqueEntityId SUPERFERTILIZER_ID = new UniqueEntityId(typeof(MyObjectBuilder_Ore), SUPERFERTILIZER_SUBTYPEID);

        public static readonly List<UniqueEntityId> FERTILIZERS = new List<UniqueEntityId>()
        {
            FERTILIZER_ID,
            MINERALFERTILIZER_ID,
            SUPERFERTILIZER_ID
        };

        public const string ARNICA_SEEDS_SUBTYPEID = "ArnicaSeeds";
        public static readonly UniqueEntityId ARNICA_SEEDS_ID = new UniqueEntityId(typeof(MyObjectBuilder_Ore), ARNICA_SEEDS_SUBTYPEID);

        public const string BEETROOT_SEEDS_SUBTYPEID = "BeetrootSeeds";
        public static readonly UniqueEntityId BEETROOT_SEEDS_ID = new UniqueEntityId(typeof(MyObjectBuilder_Ore), BEETROOT_SEEDS_SUBTYPEID);

        public const string BROCCOLI_SEEDS_SUBTYPEID = "BroccoliSeeds";
        public static readonly UniqueEntityId BROCCOLI_SEEDS_ID = new UniqueEntityId(typeof(MyObjectBuilder_Ore), BROCCOLI_SEEDS_SUBTYPEID);

        public const string CARROT_SEEDS_SUBTYPEID = "CarrotSeeds";
        public static readonly UniqueEntityId CARROT_SEEDS_ID = new UniqueEntityId(typeof(MyObjectBuilder_Ore), CARROT_SEEDS_SUBTYPEID);

        public const string COFFEE_SEEDS_SUBTYPEID = "CoffeeSeeds";
        public static readonly UniqueEntityId COFFEE_SEEDS_ID = new UniqueEntityId(typeof(MyObjectBuilder_Ore), COFFEE_SEEDS_SUBTYPEID);

        public const string MINT_SEEDS_SUBTYPEID = "MintSeeds";
        public static readonly UniqueEntityId MINT_SEEDS_ID = new UniqueEntityId(typeof(MyObjectBuilder_Ore), MINT_SEEDS_SUBTYPEID);

        public const string TOMATO_SEEDS_SUBTYPEID = "TomatoSeeds";
        public static readonly UniqueEntityId TOMATO_SEEDS_ID = new UniqueEntityId(typeof(MyObjectBuilder_Ore), TOMATO_SEEDS_SUBTYPEID);

        public const string WHEAT_SEEDS_SUBTYPEID = "WheatSeeds";
        public static readonly UniqueEntityId WHEAT_SEEDS_ID = new UniqueEntityId(typeof(MyObjectBuilder_Ore), WHEAT_SEEDS_SUBTYPEID);

        public const string CHAMOMILE_SEEDS_SUBTYPEID = "ChamomileSeeds";
        public static readonly UniqueEntityId CHAMOMILE_SEEDS_ID = new UniqueEntityId(typeof(MyObjectBuilder_Ore), CHAMOMILE_SEEDS_SUBTYPEID);

        public const string ALOEVERA_SEEDS_SUBTYPEID = "AloeVeraSeeds";
        public static readonly UniqueEntityId ALOEVERA_SEEDS_ID = new UniqueEntityId(typeof(MyObjectBuilder_Ore), ALOEVERA_SEEDS_SUBTYPEID);

        public const string ERYTHROXYLUM_SEEDS_SUBTYPEID = "ErythroxylumSeeds";
        public static readonly UniqueEntityId ERYTHROXYLUM_SEEDS_ID = new UniqueEntityId(typeof(MyObjectBuilder_Ore), ERYTHROXYLUM_SEEDS_SUBTYPEID);

        public static readonly List<UniqueEntityId> SEEDS = new List<UniqueEntityId>()
        {
            ARNICA_SEEDS_ID,
            BEETROOT_SEEDS_ID,
            BROCCOLI_SEEDS_ID,
            CARROT_SEEDS_ID,
            COFFEE_SEEDS_ID,
            MINT_SEEDS_ID,
            TOMATO_SEEDS_ID,
            WHEAT_SEEDS_ID,
            CHAMOMILE_SEEDS_ID,
            ALOEVERA_SEEDS_ID,
            ERYTHROXYLUM_SEEDS_ID
        };

        public const string APPLETREESEEDLING_SUBTYPEID = "AppleTreeSeedling";
        public static readonly UniqueEntityId APPLETREESEEDLING_ID = new UniqueEntityId(typeof(MyObjectBuilder_GasContainerObject), APPLETREESEEDLING_SUBTYPEID);

        public const string APPLETREE_SUBTYPEID = "AppleTree";
        public static readonly UniqueEntityId APPLETREE_ID = new UniqueEntityId(typeof(MyObjectBuilder_GasContainerObject), APPLETREE_SUBTYPEID);

        public static readonly List<UniqueEntityId> SEEDLINGS = new List<UniqueEntityId>()
        {
            APPLETREESEEDLING_ID
        };

        public static readonly List<UniqueEntityId> TREES = new List<UniqueEntityId>()
        {
            APPLETREE_ID
        };

        public const string MEATRATION_SUBTYPEID = "MeatRation";
        public static readonly UniqueEntityId MEATRATION_ID = new UniqueEntityId(typeof(MyObjectBuilder_Ore), MEATRATION_SUBTYPEID);

        public const string VEGETABLERATION_SUBTYPEID = "VegetablesRation";
        public static readonly UniqueEntityId VEGETABLERATION_ID = new UniqueEntityId(typeof(MyObjectBuilder_Ore), VEGETABLERATION_SUBTYPEID);

        public const string GRAINSRATION_SUBTYPEID = "GrainsRation";
        public static readonly UniqueEntityId GRAINSRATION_ID = new UniqueEntityId(typeof(MyObjectBuilder_Ore), GRAINSRATION_SUBTYPEID);

        public static readonly List<UniqueEntityId> RATIONS_DEFINITIONS = new List<UniqueEntityId>()
        {
            MEATRATION_ID,
            VEGETABLERATION_ID, 
            GRAINSRATION_ID
        };

        public const string COWMALE_SUBTYPEID = "CowMale";
        public static readonly UniqueEntityId COWMALE_ID = new UniqueEntityId(typeof(MyObjectBuilder_GasContainerObject), COWMALE_SUBTYPEID);

        public const string COWFEMALE_SUBTYPEID = "CowFemale";
        public static readonly UniqueEntityId COWFEMALE_ID = new UniqueEntityId(typeof(MyObjectBuilder_GasContainerObject), COWFEMALE_SUBTYPEID);

        public const string COWBABY_SUBTYPEID = "CowBaby";
        public static readonly UniqueEntityId COWBABY_ID = new UniqueEntityId(typeof(MyObjectBuilder_GasContainerObject), COWBABY_SUBTYPEID);

        public const string DEERMALE_SUBTYPEID = "DeerMale";
        public static readonly UniqueEntityId DEERMALE_ID = new UniqueEntityId(typeof(MyObjectBuilder_GasContainerObject), DEERMALE_SUBTYPEID);

        public const string DEERFEMALE_SUBTYPEID = "DeerFemale";
        public static readonly UniqueEntityId DEERFEMALE_ID = new UniqueEntityId(typeof(MyObjectBuilder_GasContainerObject), DEERFEMALE_SUBTYPEID);

        public const string DEERBABY_SUBTYPEID = "DeerBaby";
        public static readonly UniqueEntityId DEERBABY_ID = new UniqueEntityId(typeof(MyObjectBuilder_GasContainerObject), DEERBABY_SUBTYPEID);

        public const string HORSEMALE_SUBTYPEID = "HorseMale";
        public static readonly UniqueEntityId HORSEMALE_ID = new UniqueEntityId(typeof(MyObjectBuilder_GasContainerObject), HORSEMALE_SUBTYPEID);

        public const string HORSEFEMALE_SUBTYPEID = "HorseFemale";
        public static readonly UniqueEntityId HORSEFEMALE_ID = new UniqueEntityId(typeof(MyObjectBuilder_GasContainerObject), HORSEFEMALE_SUBTYPEID);

        public const string HORSEBABY_SUBTYPEID = "HorseBaby";
        public static readonly UniqueEntityId HORSEBABY_ID = new UniqueEntityId(typeof(MyObjectBuilder_GasContainerObject), HORSEBABY_SUBTYPEID);

        public const string SHEEPMALE_SUBTYPEID = "SheepMale";
        public static readonly UniqueEntityId SHEEPMALE_ID = new UniqueEntityId(typeof(MyObjectBuilder_GasContainerObject), SHEEPMALE_SUBTYPEID);

        public const string SHEEPFEMALE_SUBTYPEID = "SheepFemale";
        public static readonly UniqueEntityId SHEEPFEMALE_ID = new UniqueEntityId(typeof(MyObjectBuilder_GasContainerObject), SHEEPFEMALE_SUBTYPEID);

        public const string SHEEPBABY_SUBTYPEID = "SheepBaby";
        public static readonly UniqueEntityId SHEEPBABY_ID = new UniqueEntityId(typeof(MyObjectBuilder_GasContainerObject), SHEEPBABY_SUBTYPEID);

        public const string SPIDERMALE_SUBTYPEID = "SpiderMale";
        public static readonly UniqueEntityId SPIDERMALE_ID = new UniqueEntityId(typeof(MyObjectBuilder_GasContainerObject), SPIDERMALE_SUBTYPEID);

        public const string SPIDERFEMALE_SUBTYPEID = "SpiderFemale";
        public static readonly UniqueEntityId SPIDERFEMALE_ID = new UniqueEntityId(typeof(MyObjectBuilder_GasContainerObject), SPIDERFEMALE_SUBTYPEID);

        public const string SPIDERBABY_SUBTYPEID = "SpiderBaby";
        public static readonly UniqueEntityId SPIDERBABY_ID = new UniqueEntityId(typeof(MyObjectBuilder_GasContainerObject), SPIDERBABY_SUBTYPEID);

        public const string WOLFMALE_SUBTYPEID = "WolfMale";
        public static readonly UniqueEntityId WOLFMALE_ID = new UniqueEntityId(typeof(MyObjectBuilder_GasContainerObject), WOLFMALE_SUBTYPEID);

        public const string WOLFFEMALE_SUBTYPEID = "WolfFemale";
        public static readonly UniqueEntityId WOLFFEMALE_ID = new UniqueEntityId(typeof(MyObjectBuilder_GasContainerObject), WOLFFEMALE_SUBTYPEID);

        public const string WOLFBABY_SUBTYPEID = "WolfBaby";
        public static readonly UniqueEntityId WOLFBABY_ID = new UniqueEntityId(typeof(MyObjectBuilder_GasContainerObject), WOLFBABY_SUBTYPEID);

        public const string PIGMALE_SUBTYPEID = "PigMale";
        public static readonly UniqueEntityId PIGMALE_ID = new UniqueEntityId(typeof(MyObjectBuilder_GasContainerObject), PIGMALE_SUBTYPEID);

        public const string PIGFEMALE_SUBTYPEID = "PigFemale";
        public static readonly UniqueEntityId PIGFEMALE_ID = new UniqueEntityId(typeof(MyObjectBuilder_GasContainerObject), PIGFEMALE_SUBTYPEID);

        public const string PIGBABY_SUBTYPEID = "PigBaby";
        public static readonly UniqueEntityId PIGBABY_ID = new UniqueEntityId(typeof(MyObjectBuilder_GasContainerObject), PIGBABY_SUBTYPEID);

        public const string CHICKENMALE_SUBTYPEID = "ChickenMale";
        public static readonly UniqueEntityId CHICKENMALE_ID = new UniqueEntityId(typeof(MyObjectBuilder_GasContainerObject), CHICKENMALE_SUBTYPEID);

        public const string CHICKENFEMALE_SUBTYPEID = "ChickenFemale";
        public static readonly UniqueEntityId CHICKENFEMALE_ID = new UniqueEntityId(typeof(MyObjectBuilder_GasContainerObject), CHICKENFEMALE_SUBTYPEID);

        public const string CHICKENBABY_SUBTYPEID = "ChickenBaby";
        public static readonly UniqueEntityId CHICKENBABY_ID = new UniqueEntityId(typeof(MyObjectBuilder_GasContainerObject), CHICKENBABY_SUBTYPEID);

        public static readonly List<UniqueEntityId> ANIMALS_HERBICORES_IDS = new List<UniqueEntityId>()
        {
            COWMALE_ID,
            COWFEMALE_ID,
            COWBABY_ID,
            DEERMALE_ID,
            DEERFEMALE_ID,
            DEERBABY_ID,
            HORSEMALE_ID,
            HORSEFEMALE_ID,
            HORSEBABY_ID,
            SHEEPMALE_ID,
            SHEEPFEMALE_ID,
            SHEEPBABY_ID,
            PIGMALE_ID,
            PIGFEMALE_ID,
            PIGBABY_ID
        };

        public static readonly List<UniqueEntityId> ANIMALS_CARNIVORES_IDS = new List<UniqueEntityId>()
        {
            SPIDERMALE_ID,
            SPIDERFEMALE_ID,
            SPIDERBABY_ID,
            WOLFMALE_ID,
            WOLFFEMALE_ID,
            WOLFBABY_ID,
            PIGMALE_ID,
            PIGFEMALE_ID,
            PIGBABY_ID
        };

        public static readonly List<UniqueEntityId> ANIMALS_BIRDS_IDS = new List<UniqueEntityId>()
        {
            CHICKENMALE_ID,
            CHICKENFEMALE_ID,
            CHICKENBABY_ID
        };

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