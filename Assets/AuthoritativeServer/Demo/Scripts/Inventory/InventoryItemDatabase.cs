using System;
using UnityEngine;

namespace AuthoritativeServer.Demo
{
    /// <summary>
    /// Item databases hold a reference to all references to inventory assets.
    /// </summary>
    [CreateAssetMenu(menuName = "Autho Server/Inventory/Create Item Database")]
    public class InventoryItemDatabase : ScriptableObject
    {
        [SerializeField]
        private InventoryItem[] m_Items;

        [SerializeField]
        private InventoryItemStat[] m_Stats;

        [SerializeField]
        private ItemCategory[] m_Categories;

        [SerializeField]
        private ItemBlueprint[] m_Blueprints;

        /// <summary>
        /// The inventory items.
        /// </summary>
        public InventoryItem[] Items {
            get { return m_Items; }
        }

        /// <summary>
        /// The inventory item stats.
        /// </summary>
        public InventoryItemStat[] Stats {
            get { return m_Stats; }
        }

        /// <summary>
        /// The inventory item categories.
        /// </summary>
        public ItemCategory[] Categories {
            get { return m_Categories; }
        }

        /// <summary>
        /// The inventory item blueprints.
        /// </summary>
        public ItemBlueprint[] Blueprints { get { return m_Blueprints; } }

        /// <summary>
        /// Get an item prefab from the specified id.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public InventoryItem GetItem(string id)
        {
            return System.Array.Find(Items, x => x.ItemID == id);
        }

        /// <summary>
        /// Gets the blueprint who's inputs match the given items.
        /// </summary>
        /// <param name="items">The items.</param>
        /// <returns></returns>
        public ItemBlueprint GetBlueprint(params InventoryItem[] items)
        {
            return Array.Find(Blueprints, x => x.Matches(items));
        }
    }
}
