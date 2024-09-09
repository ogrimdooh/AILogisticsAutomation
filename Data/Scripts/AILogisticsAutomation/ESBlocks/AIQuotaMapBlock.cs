using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Game;
using Sandbox.Common.ObjectBuilders;
using VRage.Game.ModAPI;
using Sandbox.Game.Entities.Cube;
using Sandbox.ModAPI.Interfaces;
using System.Linq;
using System.Collections.Generic;

namespace AILogisticsAutomation
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_OreDetector), false, "AIQuotaMap", "AIQuotaMapSmall", "AIQuotaMapReskin", "AIQuotaMapReskinSmall")]
    public class AIQuotaMapBlock : BaseAIBlock<IMyOreDetector, AIQuotaMapSettings, AIQuotaMapSettingsData>
    {

        public AIIgnoreMapBlock GetAIIgnoreMap()
        {
            var block = GetAIIgnoreBlock();
            if (block != null)
            {
                return block.FatBlock.GameLogic?.GetAs<AIIgnoreMapBlock>();
            }
            return null;
        }

        protected IMySlimBlock GetAIIgnoreBlock()
        {
            var validSubTypes = new string[] { "AIIgnoreMap", "AIIgnoreMapSmall", "AIIgnoreMapReskin", "AIIgnoreMapReskinSmall" };
            foreach (var item in validSubTypes)
            {
                var block = CubeGrid.GetBlocks(new MyDefinitionId(typeof(MyObjectBuilder_OreDetector), item))?.FirstOrDefault();
                if (block != null)
                    return block;
            }
            return null;
        }

        protected override bool GetHadWorkToDo()
        {
            return Settings?.GetQuotas().Any() ?? false;
        }

        protected override bool GetIsValidToWork()
        {
            return CountAIQuotaMap(Grid) == 1;
        }

        protected override bool NeedInventoryManager()
        {
            return false;
        }

        protected override void OnInit(MyObjectBuilder_EntityBase objectBuilder)
        {
            Settings = new AIQuotaMapSettings();
            base.OnInit(objectBuilder);
            NeedsUpdate |= MyEntityUpdateEnum.EACH_100TH_FRAME;
        }

        protected int CountAIQuotaMap(IMyCubeGrid grid)
        {
            var count = 0;
            var validSubTypes = new string[] { "AIQuotaMap", "AIQuotaMapSmall", "AIQuotaMapReskin", "AIQuotaMapReskinSmall" };
            foreach (var item in validSubTypes)
            {
                count += grid?.CountBlocks(new MyDefinitionId(typeof(MyObjectBuilder_OreDetector), item)) ?? 0;
            }
            return count;
        }

        protected bool _rangeReset = false;
        protected int _tryResetCount = 0;
        protected override void DoExecuteCycle()
        {
            if (!_rangeReset && _tryResetCount < 10)
                _rangeReset = CurrentEntity.DoResetRange();
            if (!_rangeReset)
                _tryResetCount++;
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
            entityList.AddRange(Settings.GetQuotas().Keys);
            entityList.RemoveAll(x => CubeGrid.Inventories.Any(y => y.EntityId == x));
            foreach (var item in entityList)
            {
                Settings.GetQuotas().Remove(item);
                needComuniteChange = true;
            }
            if (needComuniteChange)
            {
                SendToClient();
            }
        }

    }

}