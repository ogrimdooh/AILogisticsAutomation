using VRage.ObjectBuilders;
using System.Collections.Generic;
using System.Linq;
using VRage.Game;

namespace AILogisticsAutomation
{
    public class AIInventoryManagerCargoDefinition
    {

        /* Data Flags */

        public long EntityId { get; set; }
        public List<SerializableDefinitionId> ValidIds { get; set; } = new List<SerializableDefinitionId>();
        public List<MyObjectBuilderType> ValidTypes { get; set; } = new List<MyObjectBuilderType>();
        public List<SerializableDefinitionId> IgnoreIds { get; set; } = new List<SerializableDefinitionId>();
        public List<MyObjectBuilderType> IgnoreTypes { get; set; } = new List<MyObjectBuilderType>();

        public AIInventoryManagerCargoDefinitionData GetData()
        {
            var data = new AIInventoryManagerCargoDefinitionData()
            {
                entityId = EntityId
            };
            data.validIds = ValidIds.ToArray();
            data.validTypes = ValidTypes.Select(x => x.ToString()).ToArray();
            data.ignoreIds = IgnoreIds.ToArray();
            data.ignoreTypes = IgnoreTypes.Select(x => x.ToString()).ToArray();
            return data;
        }

        public bool UpdateData(string key, string action, string value)
        {
            MyDefinitionId valueAsId;
            MyObjectBuilderType valueAsType;
            switch (key)
            {
                case "ValidIds":
                    if (MyDefinitionId.TryParse(value, out valueAsId))
                    {
                        switch (action)
                        {
                            case "ADD":
                                ValidIds.Add(valueAsId);
                                return true;
                            case "DEL":
                                ValidIds.Remove(valueAsId);
                                return true;
                        }
                    }
                    break;
                case "ValidTypes":
                    if (MyObjectBuilderType.TryParse(value, out valueAsType))
                    {
                        switch (action)
                        {
                            case "ADD":
                                ValidTypes.Add(valueAsType);
                                return true;
                            case "DEL":
                                ValidTypes.Remove(valueAsType);
                                return true;
                        }
                    }
                    break;
                case "IgnoreIds":
                    if (MyDefinitionId.TryParse(value, out valueAsId))
                    {
                        switch (action)
                        {
                            case "ADD":
                                IgnoreIds.Add(valueAsId);
                                return true;
                            case "DEL":
                                IgnoreIds.Remove(valueAsId);
                                return true;
                        }
                    }
                    break;
                case "IgnoreTypes":
                    if (MyObjectBuilderType.TryParse(value, out valueAsType))
                    {
                        switch (action)
                        {
                            case "ADD":
                                IgnoreTypes.Add(valueAsType);
                                return true;
                            case "DEL":
                                IgnoreTypes.Remove(valueAsType);
                                return true;
                        }
                    }
                    break;
            }
            return false;
        }

        public void UpdateData(AIInventoryManagerCargoDefinitionData data)
        {
            ValidIds.Clear();
            foreach (var item in data.validIds)
            {
                ValidIds.Add(item);
            }
            ValidTypes.Clear();
            foreach (var item in data.validTypes)
            {
                MyObjectBuilderType type;
                if (MyObjectBuilderType.TryParse(item, out type))
                    ValidTypes.Add(type);
            }
            IgnoreIds.Clear();
            foreach (var item in data.ignoreIds)
            {
                IgnoreIds.Add(item);
            }
            IgnoreTypes.Clear();
            foreach (var item in data.ignoreTypes)
            {
                MyObjectBuilderType type;
                if (MyObjectBuilderType.TryParse(item, out type))
                    IgnoreTypes.Add(type);
            }
        }

    }

}