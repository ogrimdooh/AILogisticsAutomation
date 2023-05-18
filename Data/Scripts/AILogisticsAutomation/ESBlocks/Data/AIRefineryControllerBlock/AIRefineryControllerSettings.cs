using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace AILogisticsAutomation
{

    public class AIRefineryControllerSettings : IAIBlockSettings<AIRefineryControllerSettingsData>
    {

        /* Interface Properties */


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

        /* Contrutor */

        public AIRefineryControllerSettingsData GetData()
        {
            var data = new AIRefineryControllerSettingsData
            {
                powerConsumption = powerConsumption,
                enabled = enabled
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
            }
            return false;
        }

        public void UpdateData(AIRefineryControllerSettingsData data)
        {
            powerConsumption = data.powerConsumption;
            enabled = data.enabled;
        }

    }

}