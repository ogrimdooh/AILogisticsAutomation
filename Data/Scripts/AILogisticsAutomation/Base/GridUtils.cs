using Sandbox.ModAPI;
using System.Collections.Generic;
using VRage.Game;
using VRage.Game.ModAPI;

namespace AILogisticsAutomation
{
    public static class GridUtils
    {

        public static int CountBlocks(this IMyCubeGrid grid, MyDefinitionId blockId)
        {
            if (grid != null)
            {
                if (ExtendedSurvivalCoreAPI.Registered && MyAPIGateway.Session.IsServer)
                {
                    var lista = ExtendedSurvivalCoreAPI.GetGridBlocks(grid.EntityId, blockId.TypeId, blockId.SubtypeName);
                    return lista?.Count ?? 0;
                }
                else
                {
                    List<IMySlimBlock> lista = new List<IMySlimBlock>();
                    grid.GetBlocks(lista, x => x.BlockDefinition.Id == blockId);
                    return lista.Count;
                }
            }
            return 0;
        }

    }

}