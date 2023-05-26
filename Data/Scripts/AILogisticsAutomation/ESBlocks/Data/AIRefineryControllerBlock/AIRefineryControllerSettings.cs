using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using VRage.ObjectBuilders;

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

        /* Data Properties */

        public AIRefineryControllerPrioritySettings DefaultOres { get; set; } = new AIRefineryControllerPrioritySettings();

        private ConcurrentDictionary<long, AIRefineryControllerRefineryPrioritySettings> definitions = new ConcurrentDictionary<long, AIRefineryControllerRefineryPrioritySettings>();
        public ConcurrentDictionary<long, AIRefineryControllerRefineryPrioritySettings> GetDefinitions()
        {
            return definitions;
        }

        private HashSet<long> ignoreRefinery = new HashSet<long>();
        public HashSet<long> GetIgnoreRefinery()
        {
            return ignoreRefinery;
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
                ignoreRefinery = ignoreRefinery.ToArray()
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
            powerConsumption = data.powerConsumption;
            enabled = data.enabled;
        }

    }

}