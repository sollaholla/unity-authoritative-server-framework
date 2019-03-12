using UnityEngine;

namespace AuthoritativeServer.Demo
{
    /// <summary>
    /// Displays item stats in a panel.
    /// </summary>
    [AddComponentMenu("Autho Server/Demo/Inventory System/UI/Item Stat Panel UI")]
    public class ItemStatPanelInterface : MonoBehaviour
    {
        [SerializeField]
        private GameObject m_ItemStatSlotPrefab;
        [SerializeField]
        private RectTransform m_StatContainer;
        [SerializeField]
        private InventoryItemDatabase m_ItemDatabase;

        private void Awake()
        {
            if (m_ItemDatabase == null)
            {
                Debug.LogError("No item database defined for this " + nameof(ItemStatPanelInterface), gameObject);
                enabled = false;
            }

            foreach (ItemCategory cat in m_ItemDatabase.Categories)
            {

            }
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
            Repaint();
        }

        private void OnItemRemoved(ItemCollection oldCollection, InventoryItem item, int oldSlot)
        {
            Repaint();
        }

        private void Repaint()
        {

        }
    }
}