using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace AuthoritativeServer.Demo
{
    /// <summary>
    /// A collection of stat instances.
    /// </summary>
    [System.Serializable]
    public class InventoryItemStatCollection : IEnumerable<InventoryItemStatInstance>
    {
        [SerializeField]
        private InventoryItemStatInstance[] m_StatInstances;

        public InventoryItemStatInstance this[int index] {
            get { return m_StatInstances[index]; }
            set { m_StatInstances[index] = value; }
        }

        public int Length { get { return m_StatInstances?.Length ?? 0; } }

        public IEnumerator<InventoryItemStatInstance> GetEnumerator()
        {
            return (IEnumerator<InventoryItemStatInstance>)m_StatInstances.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return m_StatInstances.GetEnumerator();
        }

    }

    /// <summary>
    /// The inventory item stat instance for the inventory item.
    /// </summary>
    [System.Serializable]
    public class InventoryItemStatInstance
    {
        [SerializeField]
        private InventoryItemStat m_Stat;
        [SerializeField]
        private float m_Value;

        /// <summary>
        /// The stat name.
        /// </summary>
        public string Name { get { return m_Stat.Name; } }

        /// <summary>
        /// The stat description.
        /// </summary>
        public string Description { get { return m_Stat.Description; } }

        /// <summary>
        /// The stat value.
        /// </summary>
        public float Value { get { return m_Value; } }

        /// <summary>
        /// The max value of the stat.
        /// </summary>
        public float MaxValue { get { return m_Stat?.MaxValue ?? 0f; } }

        /// <summary>
        /// The stat type.
        /// </summary>
        public StatType Type { get { return m_Stat.Type; } }

        public static implicit operator InventoryItemStat(InventoryItemStatInstance inst)
        {
            return inst.m_Stat;
        }
    }
}
