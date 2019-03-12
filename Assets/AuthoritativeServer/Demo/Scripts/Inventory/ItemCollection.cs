using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AuthoritativeServer.Demo
{
    /// <summary>
    /// The item collection holding the items.
    /// </summary>
    [System.Serializable]
    public class ItemCollection : IEnumerable<InventoryItem>
    {
        [SerializeField]
        private string m_Name;
        [SerializeField]
        private InventoryItemSlot[] m_Slots;
        [SerializeField]
        private InventoryItemTypeMask m_AllowedItemTypes;
        [SerializeField]
        private bool m_IsReferenceCollection;
        [SerializeField]
        private bool m_CanStackInCollection;

#if UNITY_EDITOR
        [SerializeField, HideInInspector]
        private ItemCategoryTypeMask m_CategoryOverride;
        public int m_Selected;

        public bool m_SlotsExpanded;
        public bool m_Expanded;
#endif

        private InventoryItem[] m_Items;

        /// <summary>
        /// The item count.
        /// </summary>
        public int Count {
            get { return m_Slots.Length; }
        }

        /// <summary>
        /// Get or set the item at the specified index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns></returns>
        public InventoryItem this[int index] {
            get {
                return m_Slots[index];
            }
            set {
                InitItems();
                m_Items[index] = value;
                m_Slots[index].Item = value;
            }
        }

        /// <summary>
        /// The item collection interface.
        /// </summary>
        public ItemCollectionInterface[] Interfaces { get; private set; }

        /// <summary>
        /// The item inventory.
        /// </summary>
        public Inventory Inventory { get; private set; }

        /// <summary>
        /// True if this is an item reference collection.
        /// </summary>
        public bool IsReferenceCollection { get { return m_IsReferenceCollection; } }

        /// <summary>
        /// True if we can stack items in this collection.
        /// </summary>
        public bool CanStackInCollection { get { return m_CanStackInCollection; } }

        /// <summary>
        /// The inventory's item database.
        /// </summary>
        public InventoryItemDatabase Database { get { return Inventory.ItemDatabase; } }

        /// <summary>
        /// Repaints any interfaces this collection may have.
        /// </summary>
        public void RepaintUI()
        {
            if (Interfaces == null)
                return;

            foreach (ItemCollectionInterface ui in Interfaces)
            {
                ui.Repaint();
            }
        }

        /// <summary>
        /// Initializes this item collection.
        /// </summary>
        public void Initialize(Inventory inventory)
        {
            var find = Utilities.FindAllObjectsOfType<ItemCollectionInterface>();

            Inventory = inventory;

            List<ItemCollectionInterface> interfaces = new List<ItemCollectionInterface>();

            foreach (ItemCollectionInterface ui in find)
            {
                if (ui.CollectionName == m_Name)
                {
                    ui.Initialize(this);
                    interfaces.Add(ui);
                }
            }

            Interfaces = interfaces.ToArray();
        }

        /// <summary>
        /// The inventory items in this collection.
        /// </summary>
        public InventoryItem[] ToArray()
        {
            InitItems();
            return m_Items.ToArray();
        }

        /// <summary>
        /// True if ths item is contained in this collection.
        /// </summary>
        /// <returns></returns>
        public bool Contains(InventoryItem item)
        {
            return Array.Find(m_Slots, x => x == item) != null;
        }

        /// <summary>
        /// True if the inventory is full.
        /// </summary>
        /// <returns></returns>
        public bool IsFull()
        {
            return m_Slots.All(x => x.Item != null);
        }

        /// <summary>
        /// Determines if the inventory is full where this item cannot be added.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool IsFull(InventoryItem item)
        {
            return m_Slots.All(x => x.Item != null && ((x.Item.ItemID == item.ItemID && x.Item.Stack == x.Item.MaxStack) || x.Item.ItemID != item.ItemID));
        }

        /// <summary>
        /// Finds the first empty slot in the inventory.
        /// </summary>
        /// <returns></returns>
        public int FirstEmptySlot()
        {
            return Array.FindIndex(m_Slots, x => x.Item == null);
        }

        /// <summary>
        /// Finds the first empty slot in the inventory that allows this category.
        /// </summary>
        /// <param name="category"></param>
        /// <returns></returns>
        public int FirstEmptySlot(ItemCategory category)
        {
            return Array.FindIndex(m_Slots, x => x.Item == null && x.Allows(category));
        }

        /// <summary>
        /// True if this item is allowed in the collection.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool IsAllowed(InventoryItem item)
        {
            return m_AllowedItemTypes.Contains(item.GetType().Name);
        }

        /// <summary>
        /// True if the item is allowed in the specified slot.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="slot"></param>
        /// <returns></returns>
        public bool IsAllowed(InventoryItem item, int slot)
        {
            return IsAllowed(item) && m_Slots[slot].IsAllowed(item);
        }

        /// <summary>
        /// Get the item slot that the specified item is placed in.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public int GetItemSlot(InventoryItem item)
        {
            if (item == null)
                return -1;

            return Array.FindIndex(m_Slots, x => x.Item == item);
        }

        /// <summary>
        /// Clear all slots in the collection.
        /// </summary>
        public void Clear(bool destroy = false)
        {
            for (int i = 0; i < m_Slots.Length; i++)
            {
                if (this[i] == null)
                    continue;

                if (NetworkController.Instance.IsServer && destroy)
                {
                    NetworkController.Instance.Scene.Destroy(this[i].gameObject);
                }

                this[i] = null;
            }
        }

        /// <summary>
        /// Calculate stat stacks.
        /// </summary>
        /// <returns></returns>
        public StackedStat[] CalculateStatTotal()
        {
            Dictionary<InventoryItemStatInstance, float> stacks = new Dictionary<InventoryItemStatInstance, float>();

            foreach (InventoryItem item in m_Slots)
            {
                if (item == null)
                    continue;
                for (int i = 0; i < item.Stats.Length; i++)
                {
                    InventoryItemStatInstance stat = item.Stats[i];

                    if (stat.Type != StatType.Add)
                        continue;

                    if (stacks.ContainsKey(stat))
                        stacks[stat] += stat.Value;
                    else stacks[stat] = stat.Value;
                }
            }

            return stacks.Select(x => new StackedStat(x.Key, x.Value)).ToArray();
        }

        /// <summary>
        /// Get the item enumerator.
        /// </summary>
        /// <returns></returns>
        public IEnumerator<InventoryItem> GetEnumerator()
        {
            return (IEnumerator<InventoryItem>)m_Slots.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return m_Slots.GetEnumerator();
        }

        private void InitItems()
        {
            if (m_Items != null)
                return;

            m_Items = new InventoryItem[m_Slots.Length];
        }
    }

    /// <summary>
    /// A class that defines a statistic that's been stacked.
    /// </summary>
    public class StackedStat
    {
        public StackedStat(InventoryItemStat stat, float value)
        {
            Stat = stat;
            Value = value;
        }

        /// <summary>
        /// The stat that was stacked.
        /// </summary>
        public InventoryItemStat Stat { get; }

        /// <summary>
        /// The value of the stacked stats.
        /// </summary>
        public float Value { get; }
    }
}
