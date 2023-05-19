using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace AILogisticsAutomation
{
    public class AIRefineryControllerPrioritySettings
    {

        public ConcurrentDictionary<string, int> OrePriority { get; set; } = new ConcurrentDictionary<string, int>();

        private int GetMaxOreIndex()
        {
            return OrePriority.Count > 0 ? OrePriority.Values.Max(x => x) + 1 : 0;
        }
        private void SortOreList()
        {
            var ores = GetOres();
            for (int i = 0; i < ores.Length; i++)
            {
                OrePriority[ores[i]] = i;
            }
        }
        public void AddOrePriority(string ore)
        {
            if (!Contains(ore) && !string.IsNullOrWhiteSpace(ore))
                OrePriority[ore] = GetMaxOreIndex();
        }
        public void RemoveOrePriority(string ore)
        {
            if (Contains(ore))
            {
                OrePriority.Remove(ore);
                SortOreList();
            }
        }
        public void MoveUp(string ore)
        {
            if (Contains(ore))
            {
                var currentIndex = OrePriority[ore];
                if (currentIndex > 0)
                {
                    var targetIndex = currentIndex - 1;
                    var targetOre = GetOre(targetIndex);
                    if (targetOre != null)
                        OrePriority[targetOre] = currentIndex;
                    OrePriority[ore] = targetIndex;
                    SortOreList();
                }
            }
        }
        public void MoveDown(string ore)
        {
            if (Contains(ore))
            {
                var currentIndex = OrePriority[ore];
                if (currentIndex < OrePriority.Count - 1)
                {
                    var targetIndex = currentIndex + 1;
                    var targetOre = GetOre(targetIndex);
                    if (targetOre != null)
                        OrePriority[targetOre] = currentIndex;
                    OrePriority[ore] = targetIndex;
                    SortOreList();
                }
            }
        }
        public string[] GetOres()
        {
            return OrePriority.OrderBy(x => x.Value).Select(x => x.Key).ToArray();
        }
        public string GetOre(int index)
        {
            var query = OrePriority.Where(x => x.Value == index);
            if (query.Any())
                return query.FirstOrDefault().Key;
            return null;
        }
        public void Clear()
        {
            OrePriority.Clear();
        }
        public bool Contains(string ore)
        {
            if (!string.IsNullOrWhiteSpace(ore))
                return OrePriority.ContainsKey(ore);
            return false;
        }

    }

}