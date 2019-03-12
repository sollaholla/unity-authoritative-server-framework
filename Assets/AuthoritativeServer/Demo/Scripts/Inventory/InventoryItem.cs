using System;

using UnityEngine;

namespace AuthoritativeServer.Demo
{
    /// <summary>
    /// Defines an base inventory item type.
    /// </summary>
    [AddComponentMenu("Autho Server/Demo/Inventory System/Inventory Item")]
    [ExecuteInEditMode]
    [DisallowMultipleComponent]
    public class InventoryItem : NetworkBehaviour
    {
        #region INSPECTOR

        [SerializeField, NotEditable]
        private string m_ItemId;
        [SerializeField]
        private string m_ItemName;
        [SerializeField, TextArea]
        private string m_Description;
        [SerializeField]
        private int m_MaxStack = 1;
        [SerializeField]
        private Sprite m_Icon;
        [SerializeField]
        private InventoryItemStatCollection m_Stats;
        [SerializeField]
        private ItemCategory m_Category;

        #endregion

        #region PROPERTIES

        /// <summary>
        /// The items unique ID.
        /// </summary>
        public string ItemID { get { return m_ItemId; } private set { m_ItemId = value; } }

        /// <summary>
        /// The item's name.
        /// </summary>
        public string Name { get { return m_ItemName; } }

        /// <summary>
        /// The item's description.
        /// </summary>
        public string Description { get { return m_Description; } }

        /// <summary>
        /// The items stack size.
        /// </summary>
        public int Stack { get; set; }

        /// <summary>
        /// The maximum stack size.
        /// </summary>
        public int MaxStack { get { return m_MaxStack; } }

        /// <summary>
        /// The item icon.
        /// </summary>
        public Sprite Icon { get { return m_Icon; } }

        /// <summary>
        /// The item stats.
        /// </summary>
        public InventoryItemStatCollection Stats { get { return m_Stats; } }

        /// <summary>
        /// The item category.
        /// </summary>
        public ItemCategory Category { get { return m_Category; } }

        #endregion

        #region PRIVATE

        private void Awake()
        {
            if (Application.isPlaying)
                Stack = 1;
        }

        /// <summary>
        /// EDITOR ONLY: Generates the item's Item ID.
        /// </summary>
        public void GenerateID()
        {
#if UNITY_EDITOR
            if (string.IsNullOrEmpty(ItemID))
            {
                ItemID = Guid.NewGuid().ToString();
            }
#endif
        }

        #endregion

        #region PUBLIC

        /// <summary>
        /// Can be used to add custom logic for this item when it is removed from a collection.
        /// </summary>
        /// <param name="oldCollection"></param>
        /// <param name="oldSlot"></param>
        public virtual void NotifyItemRemoved(ItemCollection oldCollection, int oldSlot) { }

        /// <summary>
        /// Can be used to add custom logic for this item when it is added to a collection.
        /// </summary>
        /// <param name="collection"></param>
        /// <param name="slot"></param>
        public virtual void NotifyItemAdded(ItemCollection collection, int slot) { }

        public override byte[] OnSerialize()
        {
            NetworkWriter writer = new NetworkWriter();
            writer.Write((short)Stack);
            return writer.ToArray();
        }

        public override void OnDeserialize(byte[] data)
        {
            NetworkWriter writer = new NetworkWriter(data);
            Stack = writer.ReadInt16();
        }

        #endregion
    }
}
