using System;
using UnityEngine;
using UnityEngine.UI;

namespace AuthoritativeServer.Demo
{
    /// <summary>
    /// Adds capability to craft item blueprints based on items in our collection.
    /// </summary>
    [AddComponentMenu("Autho Server/Demo/Inventory System/UI/Crafting Collection UI")]
    public class CraftingCollectionInterface : ItemCollectionInterface
    {
        [Header("Crafting UI")]
        [SerializeField]
        private Button m_CraftButton;
        [SerializeField]
        private ItemCraftSlotInterface m_CraftSlot;

        private ItemBlueprint m_CurrentBlueprint;

        private void OnEnable()
        {
            m_CraftButton.onClick.AddListener(OnCraftButton);
        }

        private void OnDisable()
        {
            m_CraftButton.onClick.RemoveListener(OnCraftButton);
        }

        private void OnCraftButton()
        {
            Collection.Inventory.CraftItem();
        }

        public override void Repaint()
        {
            base.Repaint();

            InventoryItem[] items = Collection.ToArray();
            ItemBlueprint bp = Collection.Database.GetBlueprint(items);
            m_CraftButton.interactable = bp != null;
            m_CraftSlot.Repaint(bp?.Output);
            m_CurrentBlueprint = bp;
        }
    }
}
