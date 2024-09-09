using Sandbox.ModAPI;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace AILogisticsAutomation
{

    public class AIQuotaMapSettings : IAIBlockSettings<AIQuotaMapSettingsData>
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

        public AIQuotaMapSettingsData GetData()
        {
            var data = new AIQuotaMapSettingsData
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

        public void UpdateData(AIQuotaMapSettingsData data)
        {
            powerConsumption = data.powerConsumption;
            enabled = data.enabled;
        }

        public void DoBeforeSave(IMyTerminalBlock source)
        {

        }

        public void DoAfterLoad(IMyTerminalBlock source)
        {

        }

    }

}