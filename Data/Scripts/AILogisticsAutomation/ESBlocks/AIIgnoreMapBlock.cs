using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Game;
using Sandbox.Common.ObjectBuilders;
using VRage.Game.ModAPI;
using System.Linq;
using System.Collections.Generic;
using Sandbox.Game.Entities.Cube;
using Sandbox.ModAPI.Interfaces;

namespace AILogisticsAutomation
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_OreDetector), false, "AIIgnoreMap", "AIIgnoreMapSmall", "AIIgnoreMapReskin", "AIIgnoreMapReskinSmall")]
    public class AIIgnoreMapBlock : BaseAIBlock<IMyOreDetector, AIIgnoreMapSettings, AIIgnoreMapSettingsData>
    {

        protected override bool GetHadWorkToDo()
        {
            return Settings?.GetIgnoreBlocks().Any() ?? false;
        }

        protected override bool GetIsValidToWork()
        {
            return CountAIIgnoreMap(Grid) == 1;
        }

        protected override bool NeedInventoryManager()
        {
            return false;
        }

        protected override void OnInit(MyObjectBuilder_EntityBase objectBuilder)
        {
            Settings = new AIIgnoreMapSettings();
            base.OnInit(objectBuilder);
            NeedsUpdate |= MyEntityUpdateEnum.EACH_100TH_FRAME;
            var range = (ITerminalProperty<float>)CurrentEntity.GetProperty("Range");
            if (range != null)
            {
                range.SetValue(CurrentEntity, range.GetMinimum(CurrentEntity));
            }
            if (AILogisticsAutomationSession.IsUsingOreDetectorReforge())
            {
                var reforgedRange = (ITerminalProperty<float>)CurrentEntity.GetProperty("Reforged: Range");
                if (reforgedRange != null)
                {
                    reforgedRange.SetValue(CurrentEntity, reforgedRange.GetMinimum(CurrentEntity));
                }
            }
        }

        protected int CountAIIgnoreMap(IMyCubeGrid grid)
        {
            var count = 0;
            var validSubTypes = new string[] { "AIIgnoreMap", "AIIgnoreMapSmall", "AIIgnoreMapReskin", "AIIgnoreMapReskinSmall" };
            foreach (var item in validSubTypes)
            {
                count += grid?.CountBlocks(new MyDefinitionId(typeof(MyObjectBuilder_OreDetector), item)) ?? 0;
            }
            return count;
        }

        private float GetPowerConsumption()
        {

            if (!IsValidToWork)
                return 0;

            if (!HadWorkToDo)
                return StandbyPowerConsumption;

            return OperationalPowerConsumption;

        }

        private void CheckEntitiesExist()
        {
            bool needComuniteChange = false;
            var entityList = new List<long>();
            entityList.AddRange(Settings.GetIgnoreCargos());
            entityList.RemoveAll(x => CubeGrid.Inventories.Any(y => y.EntityId == x));
            foreach (var item in entityList)
            {
                Settings.GetIgnoreCargos().Remove(item);
                needComuniteChange = true;
            }
            entityList.Clear();
            entityList.AddRange(Settings.GetIgnoreFunctionalBlocks());
            entityList.RemoveAll(x => CubeGrid.Inventories.Any(y => y.EntityId == x));
            foreach (var item in entityList)
            {
                Settings.GetIgnoreFunctionalBlocks().Remove(item);
                needComuniteChange = true;
            }
            entityList.Clear();
            entityList.AddRange(Settings.GetIgnoreConnectors());
            entityList.RemoveAll(x => CubeGrid.Inventories.Any(y => y.EntityId == x));
            foreach (var item in entityList)
            {
                Settings.GetIgnoreConnectors().Remove(item);
                needComuniteChange = true;
            }
            if (needComuniteChange)
            {
                SendToClient();
            }
        }

        protected override void DoExecuteCycle()
        {
            var power = GetPowerConsumption();
            if (power != Settings.GetPowerConsumption())
            {
                Settings.SetPowerConsumption(power);
                SendPowerToClient();
                CurrentEntity.RefreshCustomInfo();
            }
            if (IsWorking)
            {
                CheckEntitiesExist();
            }
        }

    }

}