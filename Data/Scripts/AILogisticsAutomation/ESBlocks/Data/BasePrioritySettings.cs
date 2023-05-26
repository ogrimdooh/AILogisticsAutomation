using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace AILogisticsAutomation
{
    public abstract class BasePrioritySettings<T>
    {

        public ConcurrentDictionary<T, int> Priority { get; set; } = new ConcurrentDictionary<T, int>();

        protected abstract bool IsNull(T item);
        protected abstract bool Compare(T item, T item2);

        protected int GetMaxIndex()
        {
            return Priority.Count > 0 ? Priority.Values.Max(x => x) + 1 : 0;
        }
        protected void SortList()
        {
            var ores = GetAll();
            for (int i = 0; i < ores.Length; i++)
            {
                Priority[ores[i]] = i;
            }
        }
        public void AddPriority(T item)
        {
            if (!Contains(item) && !IsNull(item))
                Priority[item] = GetMaxIndex();
        }
        public void RemovePriority(T item)
        {
            if (Contains(item))
            {
                Priority.Remove(item);
                SortList();
            }
        }
        public void MoveUp(T item)
        {
            if (Contains(item))
            {
                var currentIndex = Priority[item];
                if (currentIndex > 0)
                {
                    var targetIndex = currentIndex - 1;
                    var targetItem = GetOne(targetIndex);
                    if (targetItem != null)
                        Priority[targetItem] = currentIndex;
                    Priority[item] = targetIndex;
                    SortList();
                }
            }
        }
        public void MoveDown(T item)
        {
            if (Contains(item))
            {
                var currentIndex = Priority[item];
                if (currentIndex < Priority.Count - 1)
                {
                    var targetIndex = currentIndex + 1;
                    var targetItem = GetOne(targetIndex);
                    if (targetItem != null)
                        Priority[targetItem] = currentIndex;
                    Priority[item] = targetIndex;
                    SortList();
                }
            }
        }
        public T[] GetAll()
        {
            return Priority.OrderBy(x => x.Value).Select(x => x.Key).ToArray();
        }
        public T GetOne(int index)
        {
            var query = Priority.Where(x => x.Value == index);
            if (query.Any())
                return query.FirstOrDefault().Key;
            return default(T);
        }
        public int GetIndex(T item)
        {
            if (Contains(item))
                return Priority[item];
            return -1;
        }
        public void Clear()
        {
            Priority.Clear();
        }
        public int Count()
        {
            return Priority.Count();
        }
        public bool Any()
        {
            return Priority.Any();
        }
        public bool Contains(T item)
        {
            if (!IsNull(item))
                return Priority.ContainsKey(item);
            return false;
        }

    }

}