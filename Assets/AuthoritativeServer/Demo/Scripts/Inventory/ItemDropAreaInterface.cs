using UnityEngine;

namespace AuthoritativeServer.Demo
{
    /// <summary>
    /// Allows items to be dropped when entering this graphic.
    /// </summary>
    [AddComponentMenu("Autho Server/Demo/Inventory System/UI/Drop Area UI")]
    [RequireComponent(typeof(RectTransform))]
    public class ItemDropAreaInterface : MonoBehaviour, ISlotDropHandler
    {
        public void OnDrop(ItemSlotInterface slot)
        {
            if (slot.Item == null)
                return;

            slot.Interface.Collection.Inventory.DropItem(slot.Item, slot.Index, slot.Interface.Collection);
        }
    }
}
