using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AuthoritativeServer.Demo
{
    [AddComponentMenu("Autho Server/Demo/Inventory System/UI/Crafting Slot UI")]
    [DisallowMultipleComponent]
    public class ItemCraftSlotInterface : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField]
        private Image m_ItemIcon;

        private InventoryItem m_Item;
        private bool m_Hovered;

        public InventoryItem Item { get { return m_Item; } }
        public bool Hovered { get { return m_Hovered; } }

        private void Update()
        {
            if (m_Hovered && m_Item)
            {
                ItemHoverInterface.Show(Input.mousePosition, Item);
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            m_Hovered = true;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            m_Hovered = false;
        }

        public void Repaint(InventoryItem item)
        {
            m_Item = item;
            m_ItemIcon.sprite = item?.Icon;
            m_ItemIcon.enabled = item != null;
        }
    }
}
