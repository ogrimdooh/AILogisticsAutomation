using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using VRage.Game;
using VRage.ObjectBuilders;

namespace AILogisticsAutomation
{

    public class AIAssemblerControllerAssemblerSettings
    {

        public long EntityId { get; set; }
        public AIAssemblerControllerPrioritySettings Priority { get; set; } = new AIAssemblerControllerPrioritySettings();

        public AIAssemblerControllerAssemblerSettingsData GetData()
        {
            var data = new AIAssemblerControllerAssemblerSettingsData
            {
                entityId = EntityId,
                priority = Priority.GetAll().Cast<SerializableDefinitionId>().ToArray()
            };
            return data;
        }

        public bool UpdateData(string key, string action, string value)
        {
            MyDefinitionId valueAsDefId;
            switch (key.ToUpper())
            {
                case "PRIORITY":
                    if (MyDefinitionId.TryParse(value, out valueAsDefId))
                    {
                        switch (action)
                        {
                            case "ADD":
                                Priority.AddPriority(valueAsDefId);
                                return true;
                            case "DEL":
                                Priority.RemovePriority(valueAsDefId);
                                return true;
                            case "UP":
                                Priority.MoveUp(valueAsDefId);
                                return true;
                            case "DOWN":
                                Priority.MoveDown(valueAsDefId);
                                return true;
                        }
                    }
                    break;
            }
            return false;
        }

        public void UpdateData(AIAssemblerControllerAssemblerSettingsData data)
        {
            Priority.Clear();
            foreach (var item in data.priority)
            {
                Priority.AddPriority(item);
            }
        }

    }

}