﻿using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Game;
using Sandbox.Common.ObjectBuilders;
using VRage.Game.ModAPI;
using Sandbox.Game.Entities.Cube;
using Sandbox.ModAPI.Interfaces;

namespace AILogisticsAutomation
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_OreDetector), false, "AIQuotaMap", "AIQuotaMapSmall", "AIQuotaMapReskin", "AIQuotaMapReskinSmall")]
    public class AIQuotaMapBlock : BaseAIBlock<IMyOreDetector, AIQuotaMapSettings, AIQuotaMapSettingsData>
    {

        protected override bool GetHadWorkToDo()
        {
            return false; // TODO
        }

        protected override bool GetIsValidToWork()
        {
            return CountAIQuotaMap(Grid) == 1;
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
        }

    }

}