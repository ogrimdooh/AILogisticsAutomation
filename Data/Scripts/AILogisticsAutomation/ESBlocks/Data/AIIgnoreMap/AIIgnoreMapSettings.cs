using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

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

        public IEnumerable<long> GetIgnoreBlocks()
        {
            return ignoreCargos.Concat(ignoreFunctionalBlocks).Concat(ignoreConnectors);
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
                ignoreConnectors = ignoreConnectors.ToArray()
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
        }

    }

}