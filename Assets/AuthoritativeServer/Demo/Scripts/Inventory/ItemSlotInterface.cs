using TMPro;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AuthoritativeServer.Demo
{
    /// <summary>
    /// The item slot interface.
    /// </summary>
    [AddComponentMenu("Autho Server/Demo/Inventory System/UI/Item Slot UI")]
    public class ItemSlotInterface : MonoBehaviour, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        #region INSPECTOR

        [SerializeField]
        private TMP_Text m_AmountText;
        [SerializeField]
        private Image m_ItemIconImage;
        [SerializeField]
        private bool m_DisableIconWhenEmpty = true;

        #endregion

        #region FIELDS

        private Sprite m_InitialIcon;
        private static SlotDragHandlerData m_DragHandler;
        private Vector2 m_CursorPos;
        private bool m_IsHovered;

        #endregion

        #region PROPERTIES

        /// <summary>
        /// The index of this item slot.
        /// </summary>
        public int Index { get; private set; } = -1;

        /// <summary>
        /// The interface that owns this item slot.
        /// </summary>
        public ItemCollectionInterface Interface { get; private set; }

        /// <summary>
        /// The item that this slot represents.
        /// </summary>
        public InventoryItem Item { get; private set; }

        #endregion

        #region UNITY

        private void Awake()
        {
            if (m_DisableIconWhenEmpty)
            {
                m_ItemIconImage.enabled = false;
            }
            else
            {
                m_InitialIcon = m_ItemIconImage.sprite;
            }
        }

        private void OnDisable()
        {
            m_IsHovered = false;
            m_DragHandler?.CleanUp();
            m_DragHandler = null;
        }

        private void Update()
        {
            if (m_IsHovered && Item != null && m_DragHandler == null)
            {
                ItemHoverInterface.Show(Input.mousePosition, Item);
            }
        }

        #endregion

        #region PUBLIC

        /// <summary>
        /// Initializes this item slot.
        /// </summary>
        /// <param name="index">The item slots index.</param>
        /// <param name="collectionInterface">The collections interface.</param>
        public void Initialize(int index, ItemCollectionInterface collectionInterface)
        {
            Index = index;
            Interface = collectionInterface;
        }

        /// <summary>
        /// Repaint the slot and update the item.
        /// </summary>
        /// <param name="item">The item.</param>
        public void Repaint(InventoryItem item)
        {
            Item = item;

            if (item == null)
            {
                m_AmountText.text = string.Empty;

                if (m_DisableIconWhenEmpty)
                {
                    m_ItemIconImage.enabled = false;
                }
                else
                {
                    m_ItemIconImage.sprite = m_InitialIcon;
                }
                return;
            }

            m_AmountText.text = string.Format("x{0}", item.Stack);
            m_ItemIconImage.enabled = true;
            m_ItemIconImage.sprite = item.Icon;
        }

        /// <summary>
        /// Called when the item is being dragged.
        /// </summary>
        /// <param name="eventData"></param>
        public void OnDrag(PointerEventData eventData)
        {
            if (Item == null)
                return;

            if (m_DragHandler == null)
            {
                m_DragHandler = new SlotDragHandlerData(this);
            }

            m_DragHandler.Draw(m_ItemIconImage.rectTransform.rect.size, eventData);
        }

        /// <summary>
        /// Called when the pointer enters this slot.
        /// </summary>
        /// <param name="eventData"></param>
        public void OnPointerEnter(PointerEventData eventData)
        {
            m_DragHandler?.SetHovered(this);
            m_CursorPos = eventData.position;
            m_IsHovered = true;
        }

        /// <summary>
        /// Called when we stopp dragging.
        /// </summary>
        /// <param name="eventData"></param>
        public void OnEndDrag(PointerEventData eventData)
        {
            GameObject pointerEnter = eventData.pointerEnter;
            if (pointerEnter != null)
            {
                ISlotDropHandler dropArea = pointerEnter.GetComponent<ISlotDropHandler>();
                if (dropArea != null)
                {
                    dropArea.OnDrop(this);
                    if (m_DragHandler != null)
                    {
                        m_DragHandler.CleanUp();
                        m_DragHandler = null;
                    }
                    return;
                }
            }

            if (m_DragHandler == null)
                return;

            Interface.DragEnded(m_DragHandler);
            m_DragHandler.CleanUp();
            m_DragHandler = null;
        }

        /// <summary>
        /// Called when the pointer exits this slot.
        /// </summary>
        /// <param name="eventData"></param>
        public void OnPointerExit(PointerEventData eventData)
        {
            m_IsHovered = false;

            if (m_DragHandler == null)
                return;

            if (m_DragHandler.Hovered == this)
                m_DragHandler.SetHovered(null);
        }

        /// <summary>
        /// Called when the pointer clicks this slot.
        /// </summary>
        /// <param name="eventData"></param>
        public void OnPointerClick(PointerEventData eventData)
        {
            if (m_DragHandler != null)
                return;

            if (eventData.button != PointerEventData.InputButton.Right)
                return;

            if (Item == null)
                return;

            Interface.ClickHandled(Item, Index);
        }

        #endregion
    }

    /// <summary>
    /// Drag information for item slots.
    /// </summary>
    public class SlotDragHandlerData
    {
        private Image m_DragPreview;
        private Canvas m_Canvas;

        public SlotDragHandlerData(ItemSlotInterface sourceSlot)
        {
            Source = sourceSlot;
        }

        /// <summary>
        /// The source slot of the drag.
        /// </summary>
        public ItemSlotInterface Source { get; set; }

        /// <summary>
        /// The hovered slot.
        /// </summary>
        public ItemSlotInterface Hovered { get; private set; }

        private void CreateDragPreview()
        {
            if (m_DragPreview != null)
                return;

            GameObject dragObject = new GameObject("Drag Preview: " + Source.name);
            dragObject.transform.SetParent((m_Canvas = Source.GetComponentInParent<Canvas>()).transform);
            m_DragPreview = dragObject.AddComponent<Image>();
            m_DragPreview.preserveAspect = true;
            m_DragPreview.sprite = Source.Item.Icon;
            m_DragPreview.raycastTarget = false;
        }

        /// <summary>
        /// Sets the hovered slot.
        /// </summary>
        /// <param name="slot">The slot.</param>
        public void SetHovered(ItemSlotInterface slot)
        {
            Hovered = slot;
        }

        /// <summary>
        /// Draw the drag source.
        /// </summary>
        public void Draw(Vector2 scale, PointerEventData pointerData)
        {
            if (Source == null)
                return;

            CreateDragPreview();
            m_DragPreview.rectTransform.position = pointerData.position;
            m_DragPreview.rectTransform.sizeDelta = scale;
        }

        /// <summary>
        /// True if the <see cref="Source"/> has an <see cref="InventoryItem"/> reference and that the <see cref="Source"/> is not the <see cref="Hovered"/>.
        /// </summary>
        /// <returns>True if dirty.</returns>
        public bool IsDirty()
        {
            return Source != Hovered && Hovered != null && Source.Item != null;
        }

        /// <summary>
        /// Cleanup leftover objects after the draw function is called.
        /// </summary>
        public void CleanUp()
        {
            if (m_DragPreview != null)
            {
                Object.Destroy(m_DragPreview.gameObject);
            }
        }
    }
}
