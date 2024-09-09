using System.Collections.Generic;
using System.Linq;
using VRage.Game;

namespace AILogisticsAutomation
{
    public class AIRefineryControllerTriggerSettings : BasePrioritySettings<string>
    {

        public long TriggerId { get; set; }
        public string Name { get; set; }
        public List<AIRefineryControllerTriggerConditionSettings> Conditions { get; set; } = new List<AIRefineryControllerTriggerConditionSettings>();

        protected override bool Compare(string item, string item2)
        {
            return item == item2;
        }

        protected override bool IsNull(string item)
        {
            return string.IsNullOrWhiteSpace(item);
        }

        public AIRefineryControllerTriggerSettingsData GetData()
        {
            var data = new AIRefineryControllerTriggerSettingsData
            {
                triggerId = TriggerId,
                name = Name,
                conditions = Conditions.OrderBy(x => x.Index).Select(x => x.GetData()).ToArray(),
                ores = GetAll(),
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
                                    Conditions.Add(new AIRefineryControllerTriggerConditionSettings()
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
                case "TRIGGERORES":
                    switch (action)
                    {
                        case "ADD":
                            AddPriority(value);
                            return true;
                        case "DEL":
                            RemovePriority(value);
                            return true;
                        case "UP":
                            MoveUp(value);
                            return true;
                        case "DOWN":
                            MoveDown(value);
                            return true;
                    }
                    break;
            }
            return false;
        }

        public void UpdateData(AIRefineryControllerTriggerSettingsData data)
        {
            Conditions.Clear();
            foreach (var item in data.conditions)
            {
                var id = item.id.GetId();
                if (id.HasValue)
                {
                    Conditions.Add(new AIRefineryControllerTriggerConditionSettings()
                    {
                        QueryType = item.queryType,
                        Id = id.Value,
                        OperationType = item.operationType,
                        Value = item.value,
                        Index = item.index
                    });
                }
            }
            Clear();
            foreach (var item in data.ores)
            {
                AddPriority(item);
            }
            TriggerId = data.triggerId;
            Name = data.name;
        }

    }

}