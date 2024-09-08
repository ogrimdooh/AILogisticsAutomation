using Sandbox.ModAPI;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using VRage.Game.ModAPI;
using VRage.ObjectBuilders;
using VRageMath;

namespace AILogisticsAutomation
{

    public class AIRefineryControllerSettings : IAIBlockSettings<AIRefineryControllerSettingsData>
    {

        /* Interface Properties */

        public string SelectedDefaultOre { get; set; } = "";
        public string SelectedRefineryOre { get; set; } = "";
        public long SelectedRefinery { get; set; } = 0;
        public long SelectedIgnoreEntityId { get; set; } = 0;
        public long SelectedAddedIgnoreEntityId { get; set; } = 0;

        public long SelectedTriggerId { get; set; }
        public int SelectedTriggerConditionIndex { get; set; }
        public string SelectedTriggerOre { get; set; } = "";

        /* Data Properties */

        public AIRefineryControllerPrioritySettings DefaultOres { get; set; } = new AIRefineryControllerPrioritySettings();

        private ConcurrentDictionary<long, AIRefineryControllerRefineryPrioritySettings> definitions = new ConcurrentDictionary<long, AIRefineryControllerRefineryPrioritySettings>();
        public ConcurrentDictionary<long, AIRefineryControllerRefineryPrioritySettings> GetDefinitions()
        {
            return definitions;
        }

        private ConcurrentDictionary<long, AIRefineryControllerTriggerSettings> triggers = new ConcurrentDictionary<long, AIRefineryControllerTriggerSettings>();
        public ConcurrentDictionary<long, AIRefineryControllerTriggerSettings> GetTriggers()
        {
            return triggers;
        }

        private HashSet<long> ignoreRefinery = new HashSet<long>();
        public HashSet<long> GetIgnoreRefinery()
        {
            return ignoreRefinery;
        }

        private HashSet<Vector3I> ignoreRefineryPos = new HashSet<Vector3I>();
        public HashSet<Vector3I> GetIgnoreRefineryPos()
        {
            return ignoreRefineryPos;
        }

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

        /* Contrutor */

        public AIRefineryControllerSettingsData GetData()
        {
            var data = new AIRefineryControllerSettingsData
            {
                powerConsumption = powerConsumption,
                enabled = enabled,
                definitions = definitions.Select(x => x.Value.GetData()).ToArray(),
                ores = DefaultOres.GetAll(),
                ignoreRefinery = ignoreRefinery.ToArray(),
                triggers = triggers.Select(x => x.Value.GetData()).ToArray(),
                ignoreRefineryPos = ignoreRefineryPos.ToArray()
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
                case "ORES":
                    if (long.TryParse(owner, out valueAsId))
                    {
                        var def = definitions.ContainsKey(valueAsId) ? definitions[valueAsId] : null;
                        if (def != null)
                        {
                            return def.UpdateData(key, action, value);
                        }
                    }
                    break;
                case "DEFINITIONS":
                    if (long.TryParse(value, out valueAsId))
                    {
                        switch (action)
                        {
                            case "ADD":
                                definitions[valueAsId] = new AIRefineryControllerRefineryPrioritySettings() { EntityId = valueAsId };
                                return true;
                            case "DEL":
                                definitions.Remove(valueAsId);
                                return true;
                        }
                    }
                    break;
                case "IGNOREREFINERY":
                    if (long.TryParse(value, out valueAsId))
                    {
                        switch (action)
                        {
                            case "ADD":
                                ignoreRefinery.Add(valueAsId);
                                return true;
                            case "DEL":
                                ignoreRefinery.Remove(valueAsId);
                                return true;
                        }
                    }
                    break;
                case "DEFAULTORES":
                    switch (action)
                    {
                        case "ADD":
                            DefaultOres.AddPriority(value);
                            return true;
                        case "DEL":
                            DefaultOres.RemovePriority(value);
                            return true;
                        case "UP":
                            DefaultOres.MoveUp(value);
                            return true;
                        case "DOWN":
                            DefaultOres.MoveDown(value);
                            return true;
                    }
                    break;
                case "ENABLED":
                    if (bool.TryParse(value, out valueAsFlag))
                    {
                        enabled = valueAsFlag;
                        return true;
                    }
                    break;
                case "NAME":
                case "CONDITIONS":
                case "TRIGGERORES":
                    if (long.TryParse(owner, out valueAsId))
                    {
                        var def = triggers.ContainsKey(valueAsId) ? triggers[valueAsId] : null;
                        if (def != null)
                        {
                            return def.UpdateData(key, action, value);
                        }
                    }
                    break;
                case "TRIGGERS":
                    if (long.TryParse(value, out valueAsId))
                    {
                        switch (action)
                        {
                            case "ADD":
                                triggers[valueAsId] = new AIRefineryControllerTriggerSettings() { TriggerId = valueAsId };
                                return true;
                            case "DEL":
                                triggers.Remove(valueAsId);
                                return true;
                        }
                    }
                    break;
            }
            return false;
        }

        public void UpdateData(AIRefineryControllerSettingsData data)
        {
            var dataToRemove = definitions.Keys.Where(x => !data.definitions.Any(y => y.entityId == x)).ToArray();
            foreach (var item in dataToRemove)
            {
                definitions.Remove(item);
            }
            foreach (var item in data.definitions)
            {
                var def = definitions.ContainsKey(item.entityId) ? definitions[item.entityId] : null;
                if (def != null)
                {
                    def.UpdateData(item);
                }
                else
                {
                    var newItem = new AIRefineryControllerRefineryPrioritySettings()
                    {
                        EntityId = item.entityId
                    };
                    newItem.UpdateData(item);
                    definitions[item.entityId] = newItem;
                }
            }
            DefaultOres.Clear();
            foreach (var item in data.ores)
            {
                DefaultOres.AddPriority(item);
            }
            ignoreRefinery.Clear();
            foreach (var item in data.ignoreRefinery)
            {
                ignoreRefinery.Add(item);
            }
            ignoreRefineryPos.Clear();
            foreach (var item in data.ignoreRefineryPos)
            {
                ignoreRefineryPos.Add(item);
            }
            var triggersToRemove = triggers.Keys.Where(x => !data.triggers.Any(y => y.triggerId == x)).ToArray();
            foreach (var item in triggersToRemove)
            {
                triggers.Remove(item);
            }
            foreach (var item in data.triggers)
            {
                var def = triggers.ContainsKey(item.triggerId) ? triggers[item.triggerId] : null;
                if (def != null)
                {
                    def.UpdateData(item);
                }
                else
                {
                    var newItem = new AIRefineryControllerTriggerSettings()
                    {
                        TriggerId = item.triggerId
                    };
                    newItem.UpdateData(item);
                    triggers[item.triggerId] = newItem;
                }
            }
            powerConsumption = data.powerConsumption;
            enabled = data.enabled;
        }

        public void DoBeforeSave(IMyTerminalBlock source)
        {
            if (source?.CubeGrid == null)
                return;
            ignoreRefineryPos.Clear();
            if (GetIgnoreRefinery().Any() || GetDefinitions().Any())
            {
                List<IMySlimBlock> blocks = new List<IMySlimBlock>();
                source.CubeGrid.GetBlocks(blocks, (x) => 
                    GetIgnoreRefinery().Contains(x.FatBlock?.EntityId ?? 0) ||
                    GetDefinitions().ContainsKey(x.FatBlock?.EntityId ?? 0)
                );
                if (blocks.Any())
                {
                    var blocksInfo = blocks.ToDictionary(k => k.FatBlock.EntityId, v => v.FatBlock);
                    foreach (var entityId in ignoreRefinery)
                    {
                        if (blocksInfo.ContainsKey(entityId))
                        {
                            ignoreRefineryPos.Add(blocksInfo[entityId].Position);
                        }
                    }
                    foreach (var entityId in GetDefinitions().Keys)
                    {
                        if (blocksInfo.ContainsKey(entityId))
                        {
                            GetDefinitions()[entityId].Position = blocksInfo[entityId].Position;
                        }
                        else
                        {
                            GetDefinitions()[entityId].Position = Vector3I.Zero;
                        }
                    }
                }
            }
        }

        public void DoAfterLoad(IMyTerminalBlock source)
        {
            if (source?.CubeGrid == null)
                return;
            if (GetIgnoreRefinery().Any() || GetDefinitions().Any())
            {
                List<IMySlimBlock> blocks = new List<IMySlimBlock>();
                source.CubeGrid.GetBlocks(blocks, (x) => 
                    GetIgnoreRefinery().Contains(x.FatBlock?.EntityId ?? 0) ||
                    GetDefinitions().ContainsKey(x.FatBlock?.EntityId ?? 0)
                );
                var blocksInfo = blocks.ToDictionary(k => k.FatBlock.EntityId, v => v.FatBlock);
                if (blocks.Any())
                {
                    // Remove chaves com problema
                    var keys = ignoreRefinery.ToArray();
                    foreach (var entityId in keys)
                    {
                        if (!blocksInfo.ContainsKey(entityId))
                        {
                            ignoreRefinery.Remove(entityId);
                        }
                    }
                    foreach (var entityId in GetDefinitions().Keys)
                    {
                        if (!blocksInfo.ContainsKey(entityId))
                        {
                            GetDefinitions()[entityId].EntityId = 0;
                        }
                        else
                        {
                            GetDefinitions()[entityId].Position = blocksInfo[entityId].Position;
                        }
                    }
                    // Adiciona posições faltantes
                    foreach (var entityId in ignoreRefinery)
                    {
                        if (blocksInfo.ContainsKey(entityId))
                        {
                            if (!ignoreRefineryPos.Contains(blocksInfo[entityId].Position))
                            {
                                ignoreRefineryPos.Add(blocksInfo[entityId].Position);
                            }
                        }
                    }
                }
                else
                {
                    ignoreRefinery.Clear();
                    foreach (var entityId in GetDefinitions().Keys)
                    {
                        GetDefinitions()[entityId].EntityId = 0;
                    }
                }
            }
            if (GetIgnoreRefineryPos().Any())
            {
                foreach (var entityPos in ignoreRefineryPos)
                {
                    if (source.CubeGrid.CubeExists(entityPos))
                    {
                        var block = source.CubeGrid.GetCubeBlock(entityPos);
                        if (block?.FatBlock != null && !ignoreRefinery.Contains(block.FatBlock.EntityId))
                        {
                            ignoreRefinery.Add(block.FatBlock.EntityId);
                        }
                    }
                }
            }
            if (GetDefinitions().Any())
            {
                foreach (var entityId in GetDefinitions().Keys)
                {
                    var entityPos = GetDefinitions()[entityId].Position;
                    if (source.CubeGrid.CubeExists(entityPos))
                    {
                        var block = source.CubeGrid.GetCubeBlock(entityPos);
                        if (block?.FatBlock != null)
                        {
                            GetDefinitions()[entityId].EntityId = block.FatBlock.EntityId;
                        }
                    }
                }
                // Remove all with EntityId = 0
                var keysToRemove = GetDefinitions().Where(x => x.Value.EntityId == 0).Select(x => x.Key).ToArray();
                if (keysToRemove.Any())
                {
                    foreach (var key in keysToRemove)
                    {
                        GetDefinitions().Remove(key);
                    }
                }
                // Change key id to all with new EntityId
                var keysToReAdd = GetDefinitions().Where(x => x.Value.EntityId != x.Key).Select(x => x.Key).ToArray();
                if (keysToReAdd.Any())
                {
                    foreach (var key in keysToReAdd)
                    {
                        var baseItem = GetDefinitions()[key];
                        GetDefinitions()[baseItem.EntityId] = baseItem;
                        GetDefinitions().Remove(key);
                    }
                }
            }
        }

    }

}