using Sandbox.Common.ObjectBuilders;
using System;
using VRage.Game;
using VRage.ObjectBuilders;

namespace AILogisticsAutomation
{
    public static class MyDefinitionIdUtils
    {

        public static readonly MyObjectBuilderType[] tankFilter = new MyObjectBuilderType[] { typeof(MyObjectBuilder_OxygenTank), typeof(MyObjectBuilder_GasTank) };
        public static readonly string[] notGasGenerator = new string[] { "LargeWaterSolidificator", "WaterSolidificator", "LargeBlockRefrigerator", "SmallBlockRefrigerator", "LargeBlockComposter", "FishTrap" };
        public static readonly MyObjectBuilderType[] targetReactorFilter = new MyObjectBuilderType[] { typeof(MyObjectBuilder_Reactor) };
        public static readonly MyObjectBuilderType[] targetEngineFilter = new MyObjectBuilderType[] { typeof(MyObjectBuilder_HydrogenEngine) };
        public static readonly MyObjectBuilderType[] targetConnectorFilter = new MyObjectBuilderType[] { typeof(MyObjectBuilder_ShipConnector) };
        public static readonly MyObjectBuilderType[] targetRefineryFilter = new MyObjectBuilderType[] { typeof(MyObjectBuilder_Refinery) };
        public static readonly MyObjectBuilderType[] targetParachuteFilter = new MyObjectBuilderType[] { typeof(MyObjectBuilder_Parachute) };
        public static readonly MyObjectBuilderType[] targetAssemblerFilter = new MyObjectBuilderType[] { typeof(MyObjectBuilder_Assembler), typeof(MyObjectBuilder_SurvivalKit) };

        public static readonly string[] isWaterSolidificator = new string[] { "LargeWaterSolidificator", "WaterSolidificator" };
        public static readonly string[] isRefrigerator = new string[] { "LargeBlockRefrigerator", "SmallBlockRefrigerator" };
        public static readonly string[] isComposter = new string[] { "LargeBlockComposter" };
        public static readonly string[] isFishTrap = new string[] { "FishTrap" };

        public static bool IsParachute(this MyDefinitionId id)
        {
            return targetParachuteFilter.Contains(id.TypeId);
        }

        public static bool IsRefinery(this MyDefinitionId id)
        {
            return targetRefineryFilter.Contains(id.TypeId);
        }

        public static bool IsAssembler(this MyDefinitionId id)
        {
            return targetAssemblerFilter.Contains(id.TypeId);
        }

        public static bool IsReactor(this MyDefinitionId id)
        {
            return targetReactorFilter.Contains(id.TypeId);
        }

        public static bool IsHydrogenEngine(this MyDefinitionId id)
        {
            return targetEngineFilter.Contains(id.TypeId);
        }

        public static bool IsShipConnector(this MyDefinitionId id)
        {
            return targetConnectorFilter.Contains(id.TypeId);
        }

        public static bool IsGasTank(this MyDefinitionId id)
        {
            return tankFilter.Contains(id.TypeId);
        }

        public static bool IsBottleTaget(this MyDefinitionId id)
        {
            return id.IsGasTank() || id.IsGasGenerator();
        }

        public static bool IsGasGenerator(this MyDefinitionId id)
        {
            return id.TypeId == typeof(MyObjectBuilder_OxygenGenerator) && !notGasGenerator.Contains(id.SubtypeName);
        }

        public static bool IsWaterSolidificator(this MyDefinitionId id)
        {
            return id.TypeId == typeof(MyObjectBuilder_OxygenGenerator) && isWaterSolidificator.Contains(id.SubtypeName);
        }

        public static bool IsRefrigerator(this MyDefinitionId id)
        {
            return id.TypeId == typeof(MyObjectBuilder_OxygenGenerator) && isRefrigerator.Contains(id.SubtypeName);
        }

        public static bool IsComposter(this MyDefinitionId id)
        {
            return id.TypeId == typeof(MyObjectBuilder_OxygenGenerator) && isComposter.Contains(id.SubtypeName);
        }

        public static bool IsFishTrap(this MyDefinitionId id)
        {
            return id.TypeId == typeof(MyObjectBuilder_OxygenGenerator) && isFishTrap.Contains(id.SubtypeName);
        }

    }

}