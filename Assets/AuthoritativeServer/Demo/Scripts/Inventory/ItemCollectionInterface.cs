using System;
using System.Linq;
using UnityEngine;

namespace AuthoritativeServer.Demo
{
    /// <summary>
    /// An interface that represents an item collection.
    /// </summary>
    [AddComponentMenu("Autho Server/Demo/Inventory System/UI/Item Collection UI")]
    [DisallowMultipleComponent]
    public class ItemCollectionInterface : MonoBehaviour
    {
        [SerializeField]
        private string m_CollectionName;
        [SerializeField]
        private RectTransform m_SlotContainer;
        [SerializeField]
        private bool m_AutoGenerateSlots;
        [SerializeField]
        private GameObject m_ItemSlotPrefab;

        /// <summary>
        /// The item collection name.
        /// </summary>
        public string CollectionName { get { return m_CollectionName; } }

        /// <summary>
        /// The item collection.
        /// </summary>
        public ItemCollection Collection { get; private set; }

        /// <summary>
        /// The item slots for this ui.
        /// </summary>
        public ItemSlotInterface[] Slots { get; private set; }

        /// <summary>
        /// Bind this UI to a specific collection.
        /// </summary>
        /// <param name="collection"></param>
        public virtual void Initialize(ItemCollection collection)
        {
            Collection = collection;
            Slots = new ItemSlotInterface[collection.Count];

            if (m_AutoGenerateSlots)
            {
                for (int i = 0; i < collection.Count; i++)
                {
                    GameObject instance = Instantiate(m_ItemSlotPrefab);

                    instance.name += " [Slot: " + i + "]";
                    instance.transform.SetParent(m_SlotContainer);
                    instance.transform.localScale = Vector3.one;
                    
                    Slots[i] = instance.GetComponent<ItemSlotInterface>();
                    Slots[i].Initialize(i, this);
                }
            }
            else
            {
                for (int i = 0; i < collection.Count; i++)
                {
                    Transform child = m_SlotContainer.GetChild(i);
                    ItemSlotInterface slotInterface = child.GetComponent<ItemSlotInterface>();

                    child.name += " [Slot: " + i + "]";
                    Slots[i] = slotInterface;
                    Slots[i].Initialize(i, this);
                }
            }

            Repaint();
        }

        /// <summary>
        /// Repaints the interface based on our collection.
        /// </summary>
        public virtual void Repaint()
        {
            for (int i = 0; i < Slots.Length; i++)
            {
                ItemSlotInterface slot = Slots[i];
                slot.Repaint(Collection[i]);
            }
        }

        /// <summary>
        /// Executed when an item slot drag has been ended.
        /// </summary>
        /// <param name="dragHandler">The drag handler.</param>
        public virtual void DragEnded(SlotDragHandlerData dragHandler)
        {
            if (dragHandler == null)
                return;

            if (!dragHandler.IsDirty())
                return;

            //Debug.Log(
            //    string.Format("Dragged from slot {0} in collection {1} to slot {2} in collection {3}.", 
            //    dragHandler.Source.Index, dragHandler.Source.Interface.name, 
            //    dragHandler.Hovered.Index, dragHandler.Hovered.Interface.name));

            Collection.Inventory.MoveItem(
                dragHandler.Source.Item, 
                dragHandler.Source.Index, 
                dragHandler.Source.Interface.Collection, 
                dragHandler.Hovered.Index, 
                dragHandler.Hovered.Interface.Collection);
        }

        /// <summary>
        /// Called when an item was right-clicked.
        /// </summary>
        /// <param name="item">The item we clicked.</param>
        /// <param name="slotIndex">The slot index who notified us of the click.</param>
        public virtual void ClickHandled(InventoryItem item, int slotIndex)
        {
            ItemCollection[] collections = Collection.Inventory.Collections;
            foreach (ItemCollection col in collections)
            {
                if (col == Collection)
                    continue;

                if (!col.Interfaces.Any(x => x.gameObject.activeInHierarchy))
                    continue;

                int slot = col.FirstEmptySlot(item.Category);
                if (slot == -1)
                    continue;

                col.Inventory.MoveItem(item, slotIndex, Collection, slot, col);
                break;
            }
        }
    }
}
