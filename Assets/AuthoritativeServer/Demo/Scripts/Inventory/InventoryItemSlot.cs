using UnityEngine;

namespace AuthoritativeServer.Demo
{
    [System.Serializable]
    public class InventoryItemSlot
    {
        [SerializeField]
        private InventoryItem m_Item;
        [SerializeField]
        private ItemCategoryTypeMask m_CategoryMask;

        /// <summary>
        /// The item.
        /// </summary>
        public InventoryItem Item { get { return m_Item; } set { m_Item = value; } }

        public static implicit operator InventoryItem(InventoryItemSlot slot)
        {
            return slot.m_Item;
        }

        /// <summary>
        /// True if the item is allowed.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool IsAllowed(InventoryItem item)
        {
            return m_CategoryMask.Contains(item.Category);
        }

        /// <summary>
        /// The item category.
        /// </summary>
        /// <param name="category"></param>
        /// <returns></returns>
        public bool Allows(ItemCategory category)
        {
            return m_CategoryMask.Contains(category);
        }
    }
}
