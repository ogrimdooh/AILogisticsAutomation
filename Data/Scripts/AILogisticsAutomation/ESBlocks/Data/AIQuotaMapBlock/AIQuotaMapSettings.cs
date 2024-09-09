using Sandbox.ModAPI;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using VRage.Game.ModAPI;
using VRageMath;

namespace AILogisticsAutomation
{

    public class AIQuotaMapSettings : IAIBlockSettings<AIQuotaMapSettingsData>
    {

        /* Interface Properties */

        public long SelectedQuotaEntityId { get; set; } = 0;
        public long SelectedQuotaEntryIndex { get; set; } = 0;

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

        private readonly ConcurrentDictionary<long, AIQuotaMapQuotaDefinition> quotas = new ConcurrentDictionary<long, AIQuotaMapQuotaDefinition>();
        public ConcurrentDictionary<long, AIQuotaMapQuotaDefinition> GetQuotas()
        {
            return quotas;
        }

        /* Contrutor */

        public AIQuotaMapSettingsData GetData()
        {
            var data = new AIQuotaMapSettingsData
            {
                powerConsumption = powerConsumption,
                enabled = enabled,
                quotas = quotas.Select(x => x.Value.GetData()).ToArray()
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
                case "ENTRIES":
                    if (long.TryParse(owner, out valueAsId))
                    {
                        var def = quotas.ContainsKey(valueAsId) ? quotas[valueAsId] : null;
                        if (def != null)
                        {
                            return def.UpdateData(key, action, value);
                        }
                    }
                    break;
                case "QUOTAS":
                    if (long.TryParse(value, out valueAsId))
                    {
                        switch (action)
                        {
                            case "ADD":
                                quotas[valueAsId] = new AIQuotaMapQuotaDefinition() { EntityId = valueAsId };
                                return true;
                            case "DEL":
                                quotas.Remove(valueAsId);
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

        public void UpdateData(AIQuotaMapSettingsData data)
        {
            powerConsumption = data.powerConsumption;
            enabled = data.enabled;
            var dataToRemove = quotas.Keys.Where(x => !data.quotas.Any(y => y.entityId == x)).ToArray();
            foreach (var item in dataToRemove)
            {
                quotas.Remove(item);
            }
            foreach (var item in data.quotas)
            {
                var def = quotas.ContainsKey(item.entityId) ? quotas[item.entityId] : null;
                if (def != null)
                {
                    def.UpdateData(item);
                }
                else
                {
                    var newItem = new AIQuotaMapQuotaDefinition()
                    {
                        EntityId = item.entityId
                    };
                    newItem.UpdateData(item);
                    quotas[item.entityId] = newItem;
                }
            }
        }

        public void DoBeforeSave(IMyTerminalBlock source)
        {
            if (source?.CubeGrid == null)
                return;
            if (GetQuotas().Any())
            {
                List<IMySlimBlock> blocks = new List<IMySlimBlock>();
                source.CubeGrid.GetBlocks(blocks, (x) =>
                    GetQuotas().ContainsKey(x.FatBlock?.EntityId ?? 0)
                );
                if (blocks.Any())
                {
                    var blocksInfo = blocks.ToDictionary(k => k.FatBlock.EntityId, v => v.FatBlock);
                    foreach (var entityId in GetQuotas().Keys)
                    {
                        if (blocksInfo.ContainsKey(entityId))
                        {
                            GetQuotas()[entityId].Position = blocksInfo[entityId].Position;
                        }
                        else
                        {
                            GetQuotas()[entityId].Position = Vector3I.Zero;
                        }
                    }
                }
            }
        }

        public void DoAfterLoad(IMyTerminalBlock source)
        {
            if (source?.CubeGrid == null)
                return;
            if (GetQuotas().Any())
            {
                List<IMySlimBlock> blocks = new List<IMySlimBlock>();
                source.CubeGrid.GetBlocks(blocks, (x) =>
                    GetQuotas().ContainsKey(x.FatBlock?.EntityId ?? 0)
                );
                if (blocks.Any())
                {
                    var blocksInfo = blocks.ToDictionary(k => k.FatBlock.EntityId, v => v.FatBlock);
                    // Remove chaves com problema
                    foreach (var entityId in GetQuotas().Keys)
                    {
                        if (!blocksInfo.ContainsKey(entityId))
                        {
                            GetQuotas()[entityId].EntityId = 0;
                        }
                        else
                        {
                            GetQuotas()[entityId].Position = blocksInfo[entityId].Position;
                        }
                    }
                }
                else
                {
                    foreach (var entityId in GetQuotas().Keys)
                    {
                        GetQuotas()[entityId].EntityId = 0;
                    }
                }
                if (GetQuotas().Any())
                {
                    foreach (var entityId in GetQuotas().Keys)
                    {
                        var entityPos = GetQuotas()[entityId].Position;
                        if (source.CubeGrid.CubeExists(entityPos))
                        {
                            var block = source.CubeGrid.GetCubeBlock(entityPos);
                            if (block?.FatBlock != null)
                            {
                                GetQuotas()[entityId].EntityId = block.FatBlock.EntityId;
                            }
                        }
                    }
                    // Remove all with EntityId = 0
                    var keysToRemove = GetQuotas().Where(x => x.Value.EntityId == 0).Select(x => x.Key).ToArray();
                    if (keysToRemove.Any())
                    {
                        foreach (var key in keysToRemove)
                        {
                            GetQuotas().Remove(key);
                        }
                    }
                    // Change key id to all with new EntityId
                    var keysToReAdd = GetQuotas().Where(x => x.Value.EntityId != x.Key).Select(x => x.Key).ToArray();
                    if (keysToReAdd.Any())
                    {
                        foreach (var key in keysToReAdd)
                        {
                            var baseItem = GetQuotas()[key];
                            GetQuotas()[baseItem.EntityId] = baseItem;
                            GetQuotas().Remove(key);
                        }
                    }
                }
            }
        }

    }

}