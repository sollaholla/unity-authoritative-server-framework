using UnityEngine;

namespace AuthoritativeServer.Demo
{
    /// <summary>
    /// The item stat types.
    /// </summary>
    public enum StatType
    {
        /// <summary>
        /// A fixed stat is a stat that has a fixed range from 0 to max and is used like a fraction. For example 10/15 durability.
        /// </summary>
        Fixed,
        /// <summary>
        /// An add stat is a statistic that allows you to add or subtract a value used in a total. For example +15 damage
        /// </summary>
        Add
    }

    /// <summary>
    /// Defines inventory item stat information.
    /// </summary>
    [CreateAssetMenu(menuName = "Autho Server/Inventory/Create Item Stat")]
    public class InventoryItemStat : ScriptableObject
    {
        [SerializeField]
        private string m_StatName;
        [SerializeField, TextArea]
        private string m_Description;
        [SerializeField]
        private float m_MaxValue;
        [SerializeField]
        private StatType m_StatType;

        /// <summary>
        /// The stat name.
        /// </summary>
        public string Name { get { return m_StatName; } }

        /// <summary>
        /// The max value (for fixed stats only).
        /// </summary>
        public float MaxValue { get { return m_MaxValue; } }

        /// <summary>
        /// The description of this stat.
        /// </summary>
        public string Description { get { return m_Description; } }

        /// <summary>
        /// The stat type.
        /// </summary>
        public StatType Type { get { return m_StatType; } }
    }
}
