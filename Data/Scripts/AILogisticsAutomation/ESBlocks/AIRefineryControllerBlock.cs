using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Game;
using Sandbox.Common.ObjectBuilders;
using VRage.Game.ModAPI;

namespace AILogisticsAutomation
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_OreDetector), false, "AIRefineryController")]
    public class AIRefineryControllerBlock : BaseAIBlock<IMyOreDetector, AIRefineryControllerSettings, AIRefineryControllerSettingsData>
    {

        protected override bool GetHadWorkToDo()
        {
            return false; // TODO
        }

        protected override bool GetIsValidToWork()
        {
            return CountAIRefineryController(Grid) == 1;
        }

        protected override void OnInit(MyObjectBuilder_EntityBase objectBuilder)
        {
            Settings = new AIRefineryControllerSettings();
            base.OnInit(objectBuilder);
            NeedsUpdate |= MyEntityUpdateEnum.EACH_100TH_FRAME;
        }

        protected int CountAIRefineryController(IMyCubeGrid grid)
        {
            return grid?.CountBlocks(new MyDefinitionId(typeof(MyObjectBuilder_OreDetector), "AIRefineryController")) ?? 0;
        }

        protected override void DoExecuteCycle()
        {

        }

    }

}