using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Game;
using Sandbox.Common.ObjectBuilders;
using VRage.Game.ModAPI;

namespace AILogisticsAutomation
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_OreDetector), false, "AIAssemblerController")]
    public class AIAssemblerControllerBlock : BaseAIBlock<IMyOreDetector, AIAssemblerControllerSettings, AIAssemblerControllerSettingsData>
    {

        protected override bool GetHadWorkToDo()
        {
            return false; // TODO
        }

        protected override bool GetIsValidToWork()
        {
            return CountAIAssemblerController(Grid) == 1;
        }

        protected override void OnInit(MyObjectBuilder_EntityBase objectBuilder)
        {
            Settings = new AIAssemblerControllerSettings();
            base.OnInit(objectBuilder);
            NeedsUpdate |= MyEntityUpdateEnum.EACH_100TH_FRAME;
        }

        protected int CountAIAssemblerController(IMyCubeGrid grid)
        {
            return grid?.CountBlocks(new MyDefinitionId(typeof(MyObjectBuilder_OreDetector), "AIAssemblerController")) ?? 0;
        }

        protected override void DoExecuteCycle()
        {

        }

    }

}