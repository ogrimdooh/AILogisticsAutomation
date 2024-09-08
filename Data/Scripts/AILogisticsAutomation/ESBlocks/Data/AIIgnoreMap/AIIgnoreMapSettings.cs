using Sandbox.ModAPI;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using VRage.Game.ModAPI;
using VRageMath;

namespace AILogisticsAutomation
{

    public class AIIgnoreMapSettings : IAIBlockSettings<AIIgnoreMapSettingsData>
    {

        /* Interface Properties */

        public long SelectedIgnoreEntityId { get; set; } = 0;
        public long SelectedAddedIgnoreEntityId { get; set; } = 0;

        /* Data Properties */

        private float powerConsumption = 0;
        public float GetPowerConsumption()
        {
            return powerConsumption;
        }
        public void SetPowerConsumption(float value)
        {
            powerConsumption = value;
        }

        private bool enabled = true;
        public bool GetEnabled()
        {
            return enabled;
        }
        public void SetEnabled(bool value)
        {
            enabled = value;
        }

        private HashSet<long> ignoreCargos = new HashSet<long>();
        public HashSet<long> GetIgnoreCargos()
        {
            return ignoreCargos;
        }

        private readonly HashSet<long> ignoreFunctionalBlocks = new HashSet<long>();
        public HashSet<long> GetIgnoreFunctionalBlocks()
        {
            return ignoreFunctionalBlocks;
        }

        private readonly HashSet<long> ignoreConnectors = new HashSet<long>();
        public HashSet<long> GetIgnoreConnectors()
        {
            return ignoreConnectors;
        }

        private HashSet<Vector3I> ignoreCargosPos = new HashSet<Vector3I>();
        public HashSet<Vector3I> GetIgnoreCargosPos()
        {
            return ignoreCargosPos;
        }

        private readonly HashSet<Vector3I> ignoreFunctionalBlocksPos = new HashSet<Vector3I>();
        public HashSet<Vector3I> GetIgnoreFunctionalBlocksPos()
        {
            return ignoreFunctionalBlocksPos;
        }

        private readonly HashSet<Vector3I> ignoreConnectorsPos = new HashSet<Vector3I>();
        public HashSet<Vector3I> GetIgnoreConnectorsPos()
        {
            return ignoreConnectorsPos;
        }

        public IEnumerable<long> GetIgnoreBlocks()
        {
            return ignoreCargos.Concat(ignoreFunctionalBlocks).Concat(ignoreConnectors);
        }

        public IEnumerable<Vector3I> GetIgnoreBlocksPos()
        {
            return ignoreCargosPos.Concat(ignoreFunctionalBlocksPos).Concat(ignoreConnectorsPos);
        }

        /* Contrutor */

        public AIIgnoreMapSettingsData GetData()
        {
            var data = new AIIgnoreMapSettingsData
            {
                powerConsumption = powerConsumption,
                enabled = enabled,
                ignoreCargos = ignoreCargos.ToArray(),
                ignoreFunctionalBlocks = ignoreFunctionalBlocks.ToArray(),
                ignoreConnectors = ignoreConnectors.ToArray(),
                ignoreCargosPos = ignoreCargosPos.ToArray(),
                ignoreConnectorsPos = ignoreConnectorsPos.ToArray(),
                ignoreFunctionalBlocksPos = ignoreFunctionalBlocksPos.ToArray(),
            };
            return data;
        }

        public bool UpdateData(string key, string action, string value, string owner)
        {
            long valueAsId = 0;
            bool valueAsFlag = false;
            int valueAsIndex = 0;
            float valueAsFloat = 0f;
            switch (key.ToUpper())
            {
                case "ENABLED":
                    if (bool.TryParse(value, out valueAsFlag))
                    {
                        enabled = valueAsFlag;
                        return true;
                    }
                    break;
                case "IGNORECARGOS":
                    if (long.TryParse(value, out valueAsId))
                    {
                        switch (action)
                        {
                            case "ADD":
                                ignoreCargos.Add(valueAsId);
                                return true;
                            case "DEL":
                                ignoreCargos.Remove(valueAsId);
                                return true;
                        }
                    }
                    break;
                case "IGNOREFUNCTIONALBLOCKS":
                    if (long.TryParse(value, out valueAsId))
                    {
                        switch (action)
                        {
                            case "ADD":
                                ignoreFunctionalBlocks.Add(valueAsId);
                                return true;
                            case "DEL":
                                ignoreFunctionalBlocks.Remove(valueAsId);
                                return true;
                        }
                    }
                    break;
                case "IGNORECONNECTORS":
                    if (long.TryParse(value, out valueAsId))
                    {
                        switch (action)
                        {
                            case "ADD":
                                ignoreConnectors.Add(valueAsId);
                                return true;
                            case "DEL":
                                ignoreConnectors.Remove(valueAsId);
                                return true;
                        }
                    }
                    break;
            }
            return false;
        }

        public void UpdateData(AIIgnoreMapSettingsData data)
        {
            powerConsumption = data.powerConsumption;
            enabled = data.enabled;
            ignoreCargos.Clear();
            foreach (var item in data.ignoreCargos)
            {
                ignoreCargos.Add(item);
            }
            ignoreFunctionalBlocks.Clear();
            foreach (var item in data.ignoreFunctionalBlocks)
            {
                ignoreFunctionalBlocks.Add(item);
            }
            ignoreConnectors.Clear();
            foreach (var item in data.ignoreConnectors)
            {
                ignoreConnectors.Add(item);
            }
            ignoreCargosPos.Clear();
            foreach (var item in data.ignoreCargosPos)
            {
                ignoreCargosPos.Add(item);
            }
            ignoreFunctionalBlocksPos.Clear();
            foreach (var item in data.ignoreFunctionalBlocksPos)
            {
                ignoreFunctionalBlocksPos.Add(item);
            }
            ignoreConnectorsPos.Clear();
            foreach (var item in data.ignoreConnectorsPos)
            {
                ignoreConnectorsPos.Add(item);
            }
        }

        public void DoBeforeSave(IMyTerminalBlock source)
        {
            if (source?.CubeGrid == null)
                return;
            ignoreCargosPos.Clear();
            ignoreFunctionalBlocksPos.Clear();
            ignoreConnectorsPos.Clear();
            if (GetIgnoreBlocks().Any())
            {
                List<IMySlimBlock> blocks = new List<IMySlimBlock>();
                source.CubeGrid.GetBlocks(blocks, (x) => GetIgnoreBlocks().Contains(x.FatBlock?.EntityId ?? 0));
                if (blocks.Any())
                {
                    var blocksInfo = blocks.ToDictionary(k => k.FatBlock.EntityId, v => v.FatBlock);
                    foreach (var entityId in ignoreCargos)
                    {
                        if (blocksInfo.ContainsKey(entityId))
                        {
                            ignoreCargosPos.Add(blocksInfo[entityId].Position);
                        }
                    }
                    foreach (var entityId in ignoreFunctionalBlocks)
                    {
                        if (blocksInfo.ContainsKey(entityId))
                        {
                            ignoreFunctionalBlocksPos.Add(blocksInfo[entityId].Position);
                        }
                    }
                    foreach (var entityId in ignoreConnectors)
                    {
                        if (blocksInfo.ContainsKey(entityId))
                        {
                            ignoreConnectorsPos.Add(blocksInfo[entityId].Position);
                        }
                    }
                }
            }
        }

        public void DoAfterLoad(IMyTerminalBlock source)
        {
            if (source?.CubeGrid == null)
                return;
            if (GetIgnoreBlocks().Any())
            {
                List<IMySlimBlock> blocks = new List<IMySlimBlock>();
                source.CubeGrid.GetBlocks(blocks, (x) => GetIgnoreBlocks().Contains(x.FatBlock?.EntityId ?? 0));
                var blocksInfo = blocks.ToDictionary(k => k.FatBlock.EntityId, v => v.FatBlock);
                if (blocks.Any())
                {
                    // Remove chaves com problema
                    var keys = ignoreCargos.ToArray();
                    foreach (var entityId in keys)
                    {
                        if (!blocksInfo.ContainsKey(entityId))
                        {
                            ignoreCargos.Remove(entityId);
                        }
                    }
                    keys = ignoreFunctionalBlocks.ToArray();
                    foreach (var entityId in keys)
                    {
                        if (!blocksInfo.ContainsKey(entityId))
                        {
                            ignoreFunctionalBlocks.Remove(entityId);
                        }
                    }
                    keys = ignoreConnectors.ToArray();
                    foreach (var entityId in keys)
                    {
                        if (!blocksInfo.ContainsKey(entityId))
                        {
                            ignoreConnectors.Remove(entityId);
                        }
                    }
                    // Adiciona posições faltantes
                    foreach (var entityId in ignoreCargos)
                    {
                        if (blocksInfo.ContainsKey(entityId))
                        {
                            if (!ignoreCargosPos.Contains(blocksInfo[entityId].Position))
                            {
                                ignoreCargosPos.Add(blocksInfo[entityId].Position);
                            }
                        }
                    }
                    foreach (var entityId in ignoreFunctionalBlocks)
                    {
                        if (blocksInfo.ContainsKey(entityId))
                        {
                            if (!ignoreFunctionalBlocksPos.Contains(blocksInfo[entityId].Position))
                            {
                                ignoreFunctionalBlocksPos.Add(blocksInfo[entityId].Position);
                            }
                        }
                    }
                    foreach (var entityId in ignoreConnectors)
                    {
                        if (blocksInfo.ContainsKey(entityId))
                        {
                            if (!ignoreConnectorsPos.Contains(blocksInfo[entityId].Position))
                            {
                                ignoreConnectorsPos.Add(blocksInfo[entityId].Position);
                            }
                        }
                    }
                }
                else
                {
                    ignoreCargos.Clear();
                    ignoreFunctionalBlocks.Clear();
                    ignoreConnectors.Clear();
                }
            }
            if (GetIgnoreBlocksPos().Any())
            {
                foreach (var entityPos in ignoreCargosPos)
                {
                    if (source.CubeGrid.CubeExists(entityPos))
                    {
                        var block = source.CubeGrid.GetCubeBlock(entityPos);
                        if (block?.FatBlock != null && !ignoreCargos.Contains(block.FatBlock.EntityId))
                        {
                            ignoreCargos.Add(block.FatBlock.EntityId);
                        }
                    }
                }
                foreach (var entityPos in ignoreFunctionalBlocksPos)
                {
                    if (source.CubeGrid.CubeExists(entityPos))
                    {
                        var block = source.CubeGrid.GetCubeBlock(entityPos);
                        if (block?.FatBlock != null && !ignoreFunctionalBlocks.Contains(block.FatBlock.EntityId))
                        {
                            ignoreFunctionalBlocks.Add(block.FatBlock.EntityId);
                        }
                    }
                }
                foreach (var entityPos in ignoreConnectorsPos)
                {
                    if (source.CubeGrid.CubeExists(entityPos))
                    {
                        var block = source.CubeGrid.GetCubeBlock(entityPos);
                        if (block?.FatBlock != null && !ignoreConnectors.Contains(block.FatBlock.EntityId))
                        {
                            ignoreConnectors.Add(block.FatBlock.EntityId);
                        }
                    }
                }
            }
        }

    }

}