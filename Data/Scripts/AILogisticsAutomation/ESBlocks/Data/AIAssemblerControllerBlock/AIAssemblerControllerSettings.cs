using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.ObjectBuilders;
using VRageMath;

namespace AILogisticsAutomation
{

    public class AIAssemblerControllerSettings : IAIBlockSettings<AIAssemblerControllerSettingsData>
    {

        /* Interface Properties */

        public string SelectedAddedMetaId { get; set; }
        public long SelectedIgnoreEntityId { get; set; }
        public long SelectedAddedIgnoreEntityId { get; set; }
        public MyDefinitionId SelectedDefaultPriority { get; set; }
        public long SelectedTriggerId { get; set; }
        public int SelectedTriggerConditionIndex { get; set; }
        public int SelectedTriggerActionIndex { get; set; }

        /* Data Properties */

        public AIAssemblerControllerStockSettings DefaultStock { get; set; } = new AIAssemblerControllerStockSettings();
        public AIAssemblerControllerPrioritySettings DefaultPriority { get; set; } = new AIAssemblerControllerPrioritySettings();

        private ConcurrentDictionary<long, AIAssemblerControllerTriggerSettings> triggers = new ConcurrentDictionary<long, AIAssemblerControllerTriggerSettings>();
        public ConcurrentDictionary<long, AIAssemblerControllerTriggerSettings> GetTriggers()
        {
            return triggers;
        }
        
        private HashSet<long> ignoreAssembler = new HashSet<long>();
        public HashSet<long> GetIgnoreAssembler()
        {
            return ignoreAssembler;
        }

        private HashSet<Vector3I> ignoreAssemblerPos = new HashSet<Vector3I>();
        public HashSet<Vector3I> GetIgnoreAssemblerPos()
        {
            return ignoreAssemblerPos;
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

        public AIAssemblerControllerSettingsData GetData()
        {
            var data = new AIAssemblerControllerSettingsData
            {
                powerConsumption = powerConsumption,
                enabled = enabled,
                triggers = triggers.Select(x => x.Value.GetData()).ToArray(),
                defaultPriority = DefaultPriority.GetAll().Select(x => new DocumentedDefinitionId(x)).ToArray(),
                ignoreAssembler = ignoreAssembler.ToArray(),
                ignoreAssemblerPos = ignoreAssemblerPos.ToArray(),
                stock = DefaultStock.GetData()
            };
            return data;
        }

        public bool UpdateData(string key, string action, string value, string owner)
        {
            long valueAsId = 0;
            bool valueAsFlag = false;
            int valueAsIndex = 0;
            float valueAsFloat = 0f;
            MyDefinitionId valueAsDefId;
            switch (key.ToUpper())
            {
                case "VALIDIDS":
                case "VALIDTYPES":
                    if (string.IsNullOrWhiteSpace(owner) || int.TryParse(owner, out valueAsIndex))
                    {
                        return DefaultStock.UpdateData(key, action, value, valueAsIndex);
                    }
                    break;
                case "IGNOREIDS":
                case "IGNORETYPES":
                    return DefaultStock.UpdateData(key, action, value, valueAsIndex);
                case "NAME":
                case "CONDITIONS":
                case "ACTIONS":
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
                                triggers[valueAsId] = new AIAssemblerControllerTriggerSettings() { TriggerId = valueAsId };
                                return true;
                            case "DEL":
                                triggers.Remove(valueAsId);
                                return true;
                        }
                    }
                    break;
                case "IGNOREASSEMBLER":
                    if (long.TryParse(value, out valueAsId))
                    {
                        switch (action)
                        {
                            case "ADD":
                                ignoreAssembler.Add(valueAsId);
                                return true;
                            case "DEL":
                                ignoreAssembler.Remove(valueAsId);
                                return true;
                        }
                    }
                    break;
                case "DEFAULTPRIORITY":
                    if (MyDefinitionId.TryParse(value, out valueAsDefId))
                    {
                        switch (action)
                        {
                            case "ADD":
                                DefaultPriority.AddPriority(valueAsDefId);
                                return true;
                            case "DEL":
                                DefaultPriority.RemovePriority(valueAsDefId);
                                return true;
                            case "UP":
                                DefaultPriority.MoveUp(valueAsDefId);
                                return true;
                            case "DOWN":
                                DefaultPriority.MoveDown(valueAsDefId);
                                return true;
                        }
                    }
                    break;
                case "ENABLED":
                    if (bool.TryParse(value, out valueAsFlag))
                    {
                        enabled = valueAsFlag;
                        return true;
                    }
                    break;
            }
            return false;
        }

        public void UpdateData(AIAssemblerControllerSettingsData data)
        {
            DefaultPriority.Clear();
            foreach (var item in data.defaultPriority)
            {
                var id = item.GetId();
                if (id.HasValue)
                {
                    DefaultPriority.AddPriority(id.Value);
                }
            }
            ignoreAssembler.Clear();
            foreach (var item in data.ignoreAssembler)
            {
                ignoreAssembler.Add(item);
            }
            ignoreAssemblerPos.Clear();
            foreach (var item in data.ignoreAssemblerPos)
            {
                ignoreAssemblerPos.Add(item);
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
                    var newItem = new AIAssemblerControllerTriggerSettings()
                    {
                        TriggerId = item.triggerId
                    };
                    newItem.UpdateData(item);
                    triggers[item.triggerId] = newItem;
                }
            }
            DefaultStock.UpdateData(data.stock);
            powerConsumption = data.powerConsumption;
            enabled = data.enabled;
        }

        public void DoBeforeSave(IMyTerminalBlock source)
        {
            if (source?.CubeGrid == null)
                return;
            ignoreAssemblerPos.Clear();
            if (GetIgnoreAssembler().Any())
            {
                List<IMySlimBlock> blocks = new List<IMySlimBlock>();
                source.CubeGrid.GetBlocks(blocks, (x) => GetIgnoreAssembler().Contains(x.FatBlock?.EntityId ?? 0));
                if (blocks.Any())
                {
                    var blocksInfo = blocks.ToDictionary(k => k.FatBlock.EntityId, v => v.FatBlock);
                    foreach (var entityId in ignoreAssembler)
                    {
                        if (blocksInfo.ContainsKey(entityId))
                        {
                            ignoreAssemblerPos.Add(blocksInfo[entityId].Position);
                        }
                    }
                }
            }
        }

        public void DoAfterLoad(IMyTerminalBlock source)
        {
            if (source?.CubeGrid == null)
                return;
            if (GetIgnoreAssembler().Any())
            {
                List<IMySlimBlock> blocks = new List<IMySlimBlock>();
                source.CubeGrid.GetBlocks(blocks, (x) => GetIgnoreAssembler().Contains(x.FatBlock?.EntityId ?? 0));
                var blocksInfo = blocks.ToDictionary(k => k.FatBlock.EntityId, v => v.FatBlock);
                if (blocks.Any())
                {
                    // Remove chaves com problema
                    var keys = ignoreAssembler.ToArray();
                    foreach (var entityId in keys)
                    {
                        if (!blocksInfo.ContainsKey(entityId))
                        {
                            ignoreAssembler.Remove(entityId);
                        }
                    }
                    // Adiciona posições faltantes
                    foreach (var entityId in ignoreAssembler)
                    {
                        if (blocksInfo.ContainsKey(entityId))
                        {
                            if (!ignoreAssemblerPos.Contains(blocksInfo[entityId].Position))
                            {
                                ignoreAssemblerPos.Add(blocksInfo[entityId].Position);
                            }
                        }
                    }
                }
                else
                {
                    ignoreAssembler.Clear();
                }
            }
            if (GetIgnoreAssemblerPos().Any())
            {
                foreach (var entityPos in ignoreAssemblerPos)
                {
                    if (source.CubeGrid.CubeExists(entityPos))
                    {
                        var block = source.CubeGrid.GetCubeBlock(entityPos);
                        if (block?.FatBlock != null && !ignoreAssembler.Contains(block.FatBlock.EntityId))
                        {
                            ignoreAssembler.Add(block.FatBlock.EntityId);
                        }
                    }
                }
            }
        }

    }

}