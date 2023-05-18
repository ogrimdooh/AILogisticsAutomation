using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Game;
using Sandbox.Common.ObjectBuilders;
using VRage.Game.ModAPI;

namespace AILogisticsAutomation
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_OreDetector), false, "AIDisplayMonitor")]
    public class AIDisplayMonitorBlock : BaseAIBlock<IMyOreDetector, AIDisplayMonitorSettings, AIDisplayMonitorSettingsData>
    {

        protected override bool GetHadWorkToDo()
        {
            return false; // TODO
        }

        protected override bool GetIsValidToWork()
        {
            return CountAIDisplayMonitor(Grid) == 1;
        }

        protected override void OnInit(MyObjectBuilder_EntityBase objectBuilder)
        {
            Settings = new AIDisplayMonitorSettings();
            base.OnInit(objectBuilder);
            NeedsUpdate |= MyEntityUpdateEnum.EACH_100TH_FRAME;
        }

        protected int CountAIDisplayMonitor(IMyCubeGrid grid)
        {
            return grid?.CountBlocks(new MyDefinitionId(typeof(MyObjectBuilder_OreDetector), "AIDisplayMonitor")) ?? 0;
        }

        protected override void DoExecuteCycle()
        {

        }

    }

}