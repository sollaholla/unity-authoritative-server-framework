using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AuthoritativeServer.Demo
{
    /// <summary>
    /// Adds status effects to characters based on equipped items.
    /// </summary>
    [AddComponentMenu("Autho Server/Demo/Inventory System/Status Effect")]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Inventory))]
    public class StatusEffect : NetworkBehaviour
    {
        [SerializeField]
        private int m_EquipmentCollectionIndex;

        private Inventory m_Inventory;
        private StackedStat[] m_CurrentStats;
        private Dictionary<string, float> m_StatCache;

        private void Awake()
        {
            m_Inventory = GetComponent<Inventory>();
        }

        private void OnEnable()
        {
            Inventory.ItemAdded += OnItemAdded;
            Inventory.ItemRemoved += OnItemRemoved;
        }

        private void OnDisable()
        {
            Inventory.ItemAdded -= OnItemAdded;
            Inventory.ItemRemoved -= OnItemRemoved;
        }

        private void OnItemAdded(ItemCollection collection, InventoryItem item, int slot)
        {
            if (!IsOwner && !IsServer)
                return;

            if (collection == m_Inventory.Collections[m_EquipmentCollectionIndex])
            {
                m_CurrentStats = collection.CalculateStatTotal();
                m_StatCache = m_CurrentStats.ToDictionary(x => x.Stat.name, y => y.Value);
            }
        }

        private void OnItemRemoved(ItemCollection oldCollection, InventoryItem item, int oldSlot)
        {
            if (!IsOwner && !IsServer)
                return;

            if (oldCollection == m_Inventory.Collections[m_EquipmentCollectionIndex])
            {
                m_CurrentStats = oldCollection.CalculateStatTotal();
                m_StatCache = m_CurrentStats.ToDictionary(x => x.Stat.name, y => y.Value);
            }
        }

        /// <summary>
        /// Get a stat value.
        /// </summary>
        /// <param name="statName"></param>
        /// <returns></returns>
        public float GetValue(string statName, float defaultValue = 0f)
        {
            if (m_StatCache == null)
                return defaultValue;

            if (m_StatCache.TryGetValue(statName, out float v))
            {
                return v;
            }

            return defaultValue;
        }
    }
}
