using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using System;
using System.Collections.Generic;
using System.Linq;
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
        public static readonly MyObjectBuilderType[] targetGunFilter = new MyObjectBuilderType[] { typeof(MyObjectBuilder_SmallGatlingGun), typeof(MyObjectBuilder_SmallMissileLauncher), typeof(MyObjectBuilder_SmallMissileLauncherReload) };
        public static readonly MyObjectBuilderType[] targetTurretFilter = new MyObjectBuilderType[] { typeof(MyObjectBuilder_InteriorTurret), typeof(MyObjectBuilder_LargeGatlingTurret), typeof(MyObjectBuilder_LargeMissileTurret) };
        public static readonly MyObjectBuilderType[] targetAssemblerFilter = new MyObjectBuilderType[] { typeof(MyObjectBuilder_Assembler), typeof(MyObjectBuilder_SurvivalKit) };

        public static readonly string[] isWaterSolidificator = new string[] { "LargeWaterSolidificator", "WaterSolidificator" };
        public static readonly string[] isRefrigerator = new string[] { "LargeBlockRefrigerator", "SmallBlockRefrigerator" };
        public static readonly string[] isComposter = new string[] { "LargeBlockComposter" };
        public static readonly string[] isFishTrap = new string[] { "FishTrap" };
        public static readonly string[] isNanobot = new string[] { "SELtdLargeNanobotBuildAndRepairSystem", "SELtdSmallNanobotBuildAndRepairSystem" };

        public static readonly string[] isFarm = new string[] { "LargeBlockFarm" };
        public static readonly string[] isTreeFarm = new string[] { "LargeBlockTreeFarm" };

        private static MyDefinitionId[] weaponCoreGuns = new MyDefinitionId[] { };
        public static MyDefinitionId[] WeaponCoreGuns
        {
            get
            {
                return weaponCoreGuns;
            }
        }

        private static MyDefinitionId[] weaponCoreTurrets = new MyDefinitionId[] { };
        public static MyDefinitionId[] WeaponCoreTurrets
        {
            get
            {
                return weaponCoreTurrets;
            }
        }

        public static void DoLoadWC(ICollection<MyDefinitionId> guns, ICollection<MyDefinitionId> turrets)
        {
            weaponCoreGuns = guns.ToArray();
            weaponCoreTurrets = turrets.ToArray();
        }

        public static bool IsGun(this MyDefinitionId id)
        {
            return targetGunFilter.Contains(id.TypeId) || weaponCoreGuns.Contains(id);
        }

        public static bool IsTurret(this MyDefinitionId id)
        {
            return targetTurretFilter.Contains(id.TypeId) || weaponCoreTurrets.Contains(id);
        }

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

        public static bool IsGasTank(this SerializableDefinitionId id, MyDefinitionId gasType)
        {
            return ((MyDefinitionId)id).IsGasTank(gasType);
        }

        public static bool IsGasTank(this MyDefinitionId id, MyDefinitionId gasType)
        {
            if (id.IsGasTank())
            {
                var gasTankDef = MyDefinitionManager.Static.GetCubeBlockDefinition(id) as MyGasTankDefinition;
                if (gasTankDef != null)
                {
                    return gasTankDef.StoredGasId == gasType;
                }
            }
            return false;
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

        public static bool IsNanobot(this MyDefinitionId id)
        {
            return id.TypeId == typeof(MyObjectBuilder_ShipWelder) && isNanobot.Contains(id.SubtypeName);
        }

        public static bool IsShipWelder(this MyDefinitionId id)
        {
            return id.TypeId == typeof(MyObjectBuilder_ShipWelder);
        }

        public static bool IsAnyFarm(this MyDefinitionId id)
        {
            return id.TypeId == typeof(MyObjectBuilder_OxygenFarm) && (isFarm.Contains(id.SubtypeName) || isTreeFarm.Contains(id.SubtypeName));
        }

        public static bool IsFarm(this MyDefinitionId id)
        {
            return id.TypeId == typeof(MyObjectBuilder_OxygenFarm) && isFarm.Contains(id.SubtypeName);
        }

        public static bool IsTreeFarm(this MyDefinitionId id)
        {
            return id.TypeId == typeof(MyObjectBuilder_OxygenFarm) && isTreeFarm.Contains(id.SubtypeName);
        }

    }

}