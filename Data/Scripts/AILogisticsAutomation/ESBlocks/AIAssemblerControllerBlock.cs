using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Game;
using Sandbox.Common.ObjectBuilders;
using VRage.Game.ModAPI;
using System.Linq;
using System.Collections.Generic;
using Sandbox.Game.Entities;

namespace AILogisticsAutomation
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_OreDetector), false, "AIAssemblerController", "AIAssemblerControllerReskin")]
    public class AIAssemblerControllerBlock : BaseAIBlock<IMyOreDetector, AIAssemblerControllerSettings, AIAssemblerControllerSettingsData>
    {

        protected override bool GetHadWorkToDo()
        {
            return Settings.DefaultStock.ValidIds.Any() || Settings.DefaultStock.ValidTypes.Any();
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
            var count = 0;
            var validSubTypes = new string[] { "AIAssemblerController", "AIAssemblerControllerReskin" };
            foreach (var item in validSubTypes)
            {
                count += grid?.CountBlocks(new MyDefinitionId(typeof(MyObjectBuilder_OreDetector), item)) ?? 0;
            }
            return count;
        }

        public IEnumerable<MyCubeBlock> ValidInventories
        {
            get
            {
                return DoApplyBasicFilter(CubeGrid.Inventories, new long[] { });
            }
        }

        private IEnumerable<MyCubeBlock> DoApplyBasicFilter(HashSet<MyCubeBlock> inventories, IEnumerable<long> customIgnoreList, bool ignoreFunctional = false)
        {
            return inventories.Where(x =>
                (
                    (x.IsFunctional && ((x as IMyFunctionalBlock)?.Enabled ?? true)) ||
                    ignoreFunctional
                ) &&
                !x.BlockDefinition.Id.IsAssembler() &&
                !customIgnoreList.Contains(x.EntityId) &&
                !Settings.GetIgnoreAssembler().Contains(x.EntityId)
            );
        }

        protected override void DoExecuteCycle()
        {

        }

    }

}