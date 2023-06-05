using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using VRage.Game;
using VRage.ObjectBuilders;

namespace AILogisticsAutomation
{

    public class AIAssemblerControllerStockSettings
    {

        public ConcurrentDictionary<MyDefinitionId, int> ValidIds { get; set; } = new ConcurrentDictionary<MyDefinitionId, int>();
        public ConcurrentDictionary<MyObjectBuilderType, int> ValidTypes { get; set; } = new ConcurrentDictionary<MyObjectBuilderType, int>();
        public HashSet<MyDefinitionId> IgnoreIds { get; set; } = new HashSet<MyDefinitionId>();
        public HashSet<MyObjectBuilderType> IgnoreTypes { get; set; } = new HashSet<MyObjectBuilderType>();

        public AIAssemblerControllerStockSettingsData GetData()
        {
            var data = new AIAssemblerControllerStockSettingsData
            {
                validIds = ValidIds.Select(x => new AIAssemblerControllerStockIdSettingsData() { id = x.Key, amount = x.Value }).ToArray(),
                validTypes = ValidTypes.Select(x => new AIAssemblerControllerStockTypeSettingsData() { type = x.Key.ToString(), amount = x.Value }).ToArray(),
                ignoreIds = IgnoreIds.Select(x => (SerializableDefinitionId)x).ToArray(),
                ignoreTypes = IgnoreTypes.Select(x => x.ToString()).ToArray()
            };
            return data;
        }

        public bool UpdateData(string key, string action, string value, int value2)
        {
            MyDefinitionId valueAsId;
            MyObjectBuilderType valueAsType;
            switch (key.ToUpper())
            {
                case "VALIDIDS":
                    if (MyDefinitionId.TryParse(value, out valueAsId))
                    {
                        switch (action)
                        {
                            case "ADD":
                                ValidIds[valueAsId] = value2;
                                return true;
                            case "DEL":
                                ValidIds.Remove(valueAsId);
                                return true;
                            case "SET":
                                if (ValidIds.ContainsKey(valueAsId))
                                {
                                    ValidIds[valueAsId] = value2;
                                    return true;
                                }
                                break;
                        }
                    }
                    break;
                case "VALIDTYPES":
                    if (MyObjectBuilderType.TryParse(value, out valueAsType))
                    {
                        switch (action)
                        {
                            case "ADD":
                                ValidTypes[valueAsType] = value2;
                                return true;
                            case "DEL":
                                ValidTypes.Remove(valueAsType);
                                return true;
                            case "SET":
                                if (ValidTypes.ContainsKey(valueAsType))
                                {
                                    ValidTypes[valueAsType] = value2;
                                    return true;
                                }
                                break;
                        }
                    }
                    break;
                case "IGNOREIDS":
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
                case "IGNORETYPES":
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

        public void UpdateData(AIAssemblerControllerStockSettingsData data)
        {
            ValidIds.Clear();
            foreach (var item in data.validIds)
            {
                ValidIds[item.id] = item.amount;
            }
            ValidTypes.Clear();
            foreach (var item in data.validTypes)
            {
                MyObjectBuilderType type;
                if (MyObjectBuilderType.TryParse(item.type, out type))
                    ValidTypes[type] = item.amount;
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