using System.Collections.Generic;
using System.Linq;
using VRage.Game;
using VRageMath;

namespace AILogisticsAutomation
{
    public class AIQuotaMapQuotaDefinition
    {
        public long EntityId { get; set; }
        public Vector3I Position { get; set; }
        public List<AIQuotaMapQuotaEntry> Entries { get; set; } = new List<AIQuotaMapQuotaEntry>();

        public AIQuotaMapQuotaDefinitionData GetData()
        {
            var data = new AIQuotaMapQuotaDefinitionData()
            {
                entityId = EntityId,
                position = Position
            };
            data.entries = Entries.Select(x => x.GetData()).ToArray();
            return data;
        }

        public bool UpdateData(string key, string action, string value)
        {
            int valueAsIndex = 0;
            MyDefinitionId id;
            float itemValue = 0;
            int index = 0;
            switch (key.ToUpper())
            {
                case "ENTRIES":
                    switch (action)
                    {
                        case "ADD":
                            var data = value.Split(';');
                            if (data.Length == 5)
                            {
                                if (MyDefinitionId.TryParse(data[1], out id) &&
                                    float.TryParse(data[3], out itemValue) &&
                                    int.TryParse(data[4], out index))
                                {
                                    Entries.Add(new AIQuotaMapQuotaEntry()
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
                                Entries.RemoveAll(x => x.Index == valueAsIndex);
                                return true;
                            }
                            break;
                    }
                    break;
            }
            return false;
        }

        public void UpdateData(AIQuotaMapQuotaDefinitionData data)
        {
            Position = data.position;
            Entries.Clear();
            foreach (var item in data.entries)
            {
                var id = item.id.GetId();
                if (id.HasValue)
                {
                    Entries.Add(new AIQuotaMapQuotaEntry()
                    {
                        Id = id.Value,
                        Value = item.value,
                        Index = item.index
                    });
                }
            }
        }

    }

}