using ProtoBuf;

namespace AILogisticsAutomation
{
    [ProtoContract]
    public class AIInventoryManagerSettingsData
    {

        [ProtoMember(1)]
        public float powerConsumption;

        [ProtoMember(2)]
        public AIInventoryManagerCargoDefinitionData[] definitions = new AIInventoryManagerCargoDefinitionData[] { };

        [ProtoMember(3)]
        public long[] ignoreCargos = new long[] { };

        [ProtoMember(4)]
        public long[] ignoreFunctionalBlocks = new long[] { };

        [ProtoMember(5)]
        public long[] ignoreConnectors = new long[] { };

        [ProtoMember(6)]
        public bool pullFromConnectedGrids;

        [ProtoMember(7)]
        public bool pullSubGrids;

        [ProtoMember(8)]
        public long sortItensType;

        [ProtoMember(9)]
        public bool fillReactor;

        [ProtoMember(10)]
        public bool pullFromReactor;

        [ProtoMember(11)]
        public bool fillBottles;

        [ProtoMember(12)]
        public bool pullFromAssembler;

        [ProtoMember(13)]
        public bool pullFromRefinary;

        [ProtoMember(14)]
        public bool enabled;

        [ProtoMember(15)]
        public float largeReactorFuelAmount;

        [ProtoMember(16)]
        public float smallReactorFuelAmount;

        [ProtoMember(17)]
        public bool pullFromGasGenerator;

        [ProtoMember(18)]
        public bool fillGasGenerator;

        [ProtoMember(19)]
        public float smallGasGeneratorAmount;

        [ProtoMember(20)]
        public float largeGasGeneratorAmount;

        [ProtoMember(21)]
        public bool pullFromGasTank;

        [ProtoMember(22)]
        public bool pullFromComposter;

        [ProtoMember(23)]
        public bool fillComposter;

        [ProtoMember(24)]
        public bool pullFishTrap;

        [ProtoMember(25)]
        public bool fillFishTrap;

        [ProtoMember(26)]
        public bool pullRefrigerator;

        [ProtoMember(27)]
        public bool fillRefrigerator;

        [ProtoMember(28)]
        public bool stackIfPossible;

    }

}