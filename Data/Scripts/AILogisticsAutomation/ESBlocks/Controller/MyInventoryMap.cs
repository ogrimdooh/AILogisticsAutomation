using VRage.ObjectBuilders;
using VRage.Game;
using System.Collections.Generic;
using System;
using VRage.Utils;
using System.Linq;
using VRage;
using Sandbox.Game;
using Sandbox.Definitions;
using System.Collections.Concurrent;
using Sandbox.Common.ObjectBuilders.Definitions;

namespace AILogisticsAutomation
{

    public class MyInventoryMap
    {

        public static readonly ConcurrentDictionary<MyDefinitionId, MyFixedPoint> MaxStackItem = new ConcurrentDictionary<MyDefinitionId, MyFixedPoint>();
        public static readonly MyObjectBuilderType[] SingleStackTypes = new MyObjectBuilderType[] 
        { 
            typeof(MyObjectBuilder_PhysicalGunObject),
            typeof(MyObjectBuilder_GasContainerObject),
            typeof(MyObjectBuilder_OxygenContainerObject),
            typeof(MyObjectBuilder_Datapad),
            typeof(MyObjectBuilder_Package),
            typeof(MyObjectBuilder_TreeObject)
        };

        static MyInventoryMap()
        {
            var query = MyDefinitionManager.Static.GetPhysicalItemDefinitions().Where(x => !SingleStackTypes.Contains(x.Id.TypeId) && x.MaxStackAmount > 0 && x.MaxStackAmount != (MyFixedPoint)float.MaxValue);
            if (query.Any())
            {
                foreach (var item in query.ToArray())
                {
                    MaxStackItem[item.Id] = item.MaxStackAmount;
                }
            }
        }

        public class MyInventoryMapEntry
        {

            public long lastCycle { get; set; }
            public MyDefinitionId ItemId { get; set; }
            public MyFixedPoint TotalAmount { get; set; }
            public HashSet<uint> Slots { get; set; } = new HashSet<uint>();

        }

        public long EntityId { get; private set; }
        public MyInventory Inventory { get; private set; }

        private readonly ConcurrentDictionary<MyDefinitionId, MyInventoryMapEntry> Items = new ConcurrentDictionary<MyDefinitionId, MyInventoryMapEntry>();

        public MyDefinitionId[] GetItems()
        {
            return Items.Keys.ToArray();
        }

        public MyInventoryMapEntry GetItem(MyDefinitionId key)
        {
            if (Items.ContainsKey(key))
                return Items[key];
            return null;
        }

        private IEnumerable<KeyValuePair<MyDefinitionId, MyInventoryMapEntry>> GetStackableQuery()
        {
            return Items.Where(x => x.Value.Slots.Count > 1 && !SingleStackTypes.Contains(x.Key.TypeId) && !MaxStackItem.ContainsKey(x.Key));
        }

        public bool HadAnyStackable()
        {
            return GetStackableQuery().Any();
        }

        public MyDefinitionId[] GetStackableItems()
        {
            return GetStackableQuery().Select(x => x.Key).ToArray();
        }

        public MyInventoryMap(long entityId, MyInventory inventory)
        {
            EntityId = entityId;
            Inventory = inventory;
        }

        public void Update()
        {
            long cycle = MyUtils.GetRandomLong();
            if (Inventory != null)
            {
                var itemsToCheck = Inventory.GetItems().ToArray();
                for (int j = 0; j < itemsToCheck.Length; j++)
                {
                    var itemid = itemsToCheck[j].Content.GetId();
                    if (!Items.ContainsKey(itemid))
                        Items[itemid] = new MyInventoryMapEntry() { ItemId = itemid, lastCycle = cycle };
                    else
                    {
                        if (Items[itemid].lastCycle != cycle)
                        {
                            Items[itemid].lastCycle = cycle;
                            Items[itemid].TotalAmount = 0;
                            Items[itemid].Slots.Clear();
                        }
                    }
                    Items[itemid].Slots.Add(itemsToCheck[j].ItemId);
                    Items[itemid].TotalAmount += itemsToCheck[j].Amount;
                }
                if (Items.Values.Any(x => x.lastCycle != cycle))
                {
                    var keysToRemove = Items.Values.Where(x => x.lastCycle != cycle).Select(x => x.ItemId).ToArray();
                    foreach (var item in keysToRemove)
                    {
                        Items.Remove(item);
                    }
                }
            }
        }

    }

}