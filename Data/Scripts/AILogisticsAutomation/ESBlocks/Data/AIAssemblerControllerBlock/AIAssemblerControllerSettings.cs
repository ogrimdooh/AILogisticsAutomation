using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using VRage.Game;
using VRage.ObjectBuilders;

namespace AILogisticsAutomation
{

    public class AIAssemblerControllerSettings : IAIBlockSettings<AIAssemblerControllerSettingsData>
    {

        /* Interface Properties */

        public string SelectedAddedMetaId { get; set; }
        public long SelectedIgnoreEntityId { get; set; }
        public long SelectedAddedIgnoreEntityId { get; set; }

        /* Data Properties */

        public AIAssemblerControllerStockSettings DefaultStock { get; set; } = new AIAssemblerControllerStockSettings();
        public AIAssemblerControllerPrioritySettings DefaultPriority { get; set; } = new AIAssemblerControllerPrioritySettings();

        private ConcurrentDictionary<long, AIAssemblerControllerAssemblerSettings> definitions = new ConcurrentDictionary<long, AIAssemblerControllerAssemblerSettings>();
        public ConcurrentDictionary<long, AIAssemblerControllerAssemblerSettings> GetDefinitions()
        {
            return definitions;
        }

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
                definitions = definitions.Select(x => x.Value.GetData()).ToArray(),
                triggers = triggers.Select(x => x.Value.GetData()).ToArray(),
                defaultPriority = DefaultPriority.GetAll().Cast<SerializableDefinitionId>().ToArray(),
                ignoreAssembler = ignoreAssembler.ToArray(),
                stock = DefaultStock.GetData(),
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
                case "IGNOREIDS":
                case "IGNORETYPES":
                    if (int.TryParse(owner, out valueAsIndex))
                    {
                        return DefaultStock.UpdateData(key, action, value, valueAsIndex);
                    }
                    break;
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
                case "PRIORITY":
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
                                definitions[valueAsId] = new AIAssemblerControllerAssemblerSettings() { EntityId = valueAsId };
                                return true;
                            case "DEL":
                                definitions.Remove(valueAsId);
                                return true;
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
                    var newItem = new AIAssemblerControllerAssemblerSettings()
                    {
                        EntityId = item.entityId
                    };
                    newItem.UpdateData(item);
                    definitions[item.entityId] = newItem;
                }
            }
            DefaultPriority.Clear();
            foreach (var item in data.defaultPriority)
            {
                DefaultPriority.AddPriority(item);
            }
            ignoreAssembler.Clear();
            foreach (var item in data.ignoreAssembler)
            {
                ignoreAssembler.Add(item);
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

    }

}