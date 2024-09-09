using System.Collections.Generic;
using System.Linq;
using VRage.Game;

namespace AILogisticsAutomation
{
    public class AIAssemblerControllerTriggerSettings
    {

        public long TriggerId { get; set; }
        public string Name { get; set; }
        public List<AIAssemblerControllerTriggerConditionSettings> Conditions { get; set; } = new List<AIAssemblerControllerTriggerConditionSettings>();
        public List<AIAssemblerControllerTriggerActionSettings> Actions { get; set; } = new List<AIAssemblerControllerTriggerActionSettings>();

        public AIAssemblerControllerTriggerSettingsData GetData()
        {
            var data = new AIAssemblerControllerTriggerSettingsData
            {
                triggerId = TriggerId,
                name = Name,
                conditions = Conditions.OrderBy(x => x.Index).Select(x => x.GetData()).ToArray(),
                actions = Actions.OrderBy(x => x.Index).Select(x => x.GetData()).ToArray(),
            };
            return data;
        }

        public bool UpdateData(string key, string action, string value)
        {
            int valueAsIndex = 0;
            int queryType = 0;
            MyDefinitionId id;
            int operationType = 0;
            float itemValue = 0;
            int index = 0;
            switch (key.ToUpper())
            {
                case "NAME":
                    Name = value;
                    return true;
                case "CONDITIONS":
                    switch (action)
                    {
                        case "ADD":
                            var data = value.Split(';');
                            if (data.Length == 5)
                            {
                                if (int.TryParse(data[0], out queryType) &&
                                    MyDefinitionId.TryParse(data[1], out id) &&
                                    int.TryParse(data[2], out operationType) &&
                                    float.TryParse(data[3], out itemValue) &&
                                    int.TryParse(data[4], out index))
                                {
                                    Conditions.Add(new AIAssemblerControllerTriggerConditionSettings()
                                    {
                                        QueryType = queryType,
                                        Id = id,
                                        OperationType = operationType,
                                        Value = itemValue,
                                        Index = index
                                    });
                                }
                            }
                            return true;
                        case "DEL":
                            if (int.TryParse(value, out valueAsIndex))
                            {
                                Conditions.RemoveAll(x => x.Index == valueAsIndex);
                                return true;
                            }
                            break;
                    }                    
                    break;
                case "ACTIONS":
                    switch (action)
                    {
                        case "ADD":
                            var data = value.Split(';');
                            if (data.Length == 3)
                            {
                                if (MyDefinitionId.TryParse(data[0], out id) &&
                                    float.TryParse(data[1], out itemValue) &&
                                    int.TryParse(data[2], out index))
                                {
                                    Actions.Add(new AIAssemblerControllerTriggerActionSettings()
                                    {
                                        Id = id,
                                        Value = itemValue,
                                        Index = index
                                    });
                                }
                            }
                            return true;
                        case "DEL":
                            if (int.TryParse(value, out valueAsIndex))
                            {
                                Actions.RemoveAll(x => x.Index == valueAsIndex);
                                return true;
                            }
                            break;
                    }
                    break;
            }
            return false;
        }

        public void UpdateData(AIAssemblerControllerTriggerSettingsData data)
        {
            Conditions.Clear();
            foreach (var item in data.conditions)
            {
                var id = item.id.GetId();
                if (id.HasValue)
                {
                    Conditions.Add(new AIAssemblerControllerTriggerConditionSettings()
                    {
                        QueryType = item.queryType,
                        Id = id.Value,
                        OperationType = item.operationType,
                        Value = item.value,
                        Index = item.index
                    });
                }
            }
            Actions.Clear();
            foreach (var item in data.actions)
            {
                var id = item.id.GetId();
                if (id.HasValue)
                {
                    Actions.Add(new AIAssemblerControllerTriggerActionSettings()
                    {
                        Id = id.Value,
                        Value = item.value,
                        Index = item.index
                    });
                }
            }
            TriggerId = data.triggerId;
            Name = data.name;
        }

    }

}