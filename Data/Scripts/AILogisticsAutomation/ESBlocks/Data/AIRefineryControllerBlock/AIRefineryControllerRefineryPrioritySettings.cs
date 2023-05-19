namespace AILogisticsAutomation
{
    public class AIRefineryControllerRefineryPrioritySettings
    {

        public long EntityId { get; set; }
        public AIRefineryControllerPrioritySettings Ores { get; set; } = new AIRefineryControllerPrioritySettings();

        public AIRefineryControllerRefinerySettingsData GetData()
        {
            var data = new AIRefineryControllerRefinerySettingsData
            {
                entityId = EntityId,
                ores = Ores.GetOres()
            };
            return data;
        }

        public bool UpdateData(string key, string action, string value)
        {
            switch (key.ToUpper())
            {
                case "ORES":
                    switch (action)
                    {
                        case "ADD":
                            Ores.AddOrePriority(value);
                            return true;
                        case "DEL":
                            Ores.RemoveOrePriority(value);
                            return true;
                        case "UP":
                            Ores.MoveUp(value);
                            return true;
                        case "DOWN":
                            Ores.MoveDown(value);
                            return true;
                    }
                    break;
            }
            return false;
        }

        public void UpdateData(AIRefineryControllerRefinerySettingsData data)
        {
            Ores.Clear();
            foreach (var item in data.ores)
            {
                Ores.AddOrePriority(item);
            }
        }

    }

}