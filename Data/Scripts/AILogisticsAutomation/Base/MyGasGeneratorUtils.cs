using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Definitions;
using Sandbox.ModAPI;
using System.Collections.Generic;
using System.Linq;
using VRage.Game;

namespace AILogisticsAutomation
{
    public static class MyGasGeneratorUtils
    {

        public static MyDefinitionId[] GetFuelIds(this IMyGasGenerator gasGenerator)
        {
            if (gasGenerator != null)
            {
                var gasGeneratorDef = MyDefinitionManager.Static.GetCubeBlockDefinition(gasGenerator.BlockDefinition) as MyOxygenGeneratorDefinition;
                if (gasGeneratorDef != null)
                {
                    return gasGeneratorDef.BlueprintClasses.Select(x => x.Select(y => y.Prerequisites.Where(z => z.Id.TypeId != typeof(MyObjectBuilder_GasContainerObject)).Select(z => z.Id))).JoinChild().JoinChild().ToArray();
                }
            }
            return null;
        }

    }

}