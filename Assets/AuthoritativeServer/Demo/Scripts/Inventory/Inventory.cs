using AuthoritativeServer.Attributes;

using System;

using UnityEngine;

namespace AuthoritativeServer.Demo
{
    /// <summary>
    /// The main player inventory component. Manages network authority.
    /// </summary>
    [AddComponentMenu("Autho Server/Demo/Inventory System/Inventory")]
    [DisallowMultipleComponent]
    public class Inventory : NetworkBehaviour
    {
        public delegate void InventoryItemAddedDelegate(ItemCollection collection, InventoryItem item, int slot);
        public delegate void InventoryItemSwappedDelegate(ItemCollection item1Collection, InventoryItem item1, int item1NewSlot, ItemCollection item2Collection, InventoryItem item2, int item2NewSlot);

        public delegate void InventoryItemMovedDelegate(ItemCollection oldCollection, ItemCollection newCollection, int oldSlot, int newSlot, InventoryItem item);
        public delegate void InventoryItemRemovedDelegate(ItemCollection oldCollection, InventoryItem item, int oldSlot);

        /// <summary>
        /// Invoked when an item was added to a collection.
        /// </summary>
        public static event InventoryItemAddedDelegate ItemAdded;

        /// <summary>
        /// Invoked when an item was swapped in (or between) a (or multiple) collection(s).
        /// </summary>
        public static event InventoryItemSwappedDelegate ItemSwapped;

        /// <summary>
        /// Invoked when an item was moved in (or between) a (or multiple) collection(s).
        /// </summary>
        public static event InventoryItemMovedDelegate ItemMoved;

        /// <summary>
        /// Invoked when an item was removed from a collection.
        /// </summary>
        public static event InventoryItemRemovedDelegate ItemRemoved;

        #region INSPECTOR

        [Header("Collections")]
        [SerializeField]
        private InventoryItemDatabase m_ItemDatabase;
        [SerializeField, HideInInspector]
        private int m_DefaultCollectionIndex = 0;
        [SerializeField, HideInInspector]
        private int m_CraftingCollectionIndex = -1;
        [SerializeField]
        private ItemCollection[] m_Collections;

        [Header("Input")]
        [SerializeField]
        private string m_PickupButton = "Use";
        [SerializeField]
        private float m_PickupDistance = 2f;
        [SerializeField]
        private float m_PickupRayRadius = 0.25f;
        [SerializeField]
        private LayerMask m_PickupLayers = 1 << 0;

        #endregion

        #region FIELDS

        private bool m_PickupInput;

        #endregion

        #region PROPERTIES

        /// <summary>
        /// The item collections in this inventory.
        /// </summary>
        public ItemCollection[] Collections { get { return m_Collections; } }

        /// <summary>
        /// The item database.
        /// </summary>
        public InventoryItemDatabase ItemDatabase { get { return m_ItemDatabase; } }

        #endregion

        #region UNITY

        private void Awake()
        {
            RegisterRPC<int, int>(ServerRpcAdd);
            RegisterRPC<int, int, int>(ServerRpcDrop);
            RegisterRPC<int, int, int, int, int>(ServerRpcMove);
            RegisterRPC<int>(SharedRpcClientClaim);
            RegisterRPC<int, int, int, Vector3, Quaternion>(SharedRpcClientReleaseClaim);
            RegisterRPC<int, int, int>(ClientRpcSetItemStack);
            RegisterRPC<NetworkConnection, int, int, int, int, int>(ClientRpcMoveItem);
            RegisterRPC<NetworkConnection, int, int, int, int, int, int>(ClientRpcSwapItem);
            RegisterRPC(ServerRpcCraft);
            RegisterRPC<NetworkConnection, int>(ClientRpcClearCollection);
        }

        private void OnEnable()
        {
            ItemAdded += OnItemAdded;
            ItemRemoved += OnItemRemoved;
            NetworkScene.DestroyedGameObject += OnObjectDestroyed;
        }

        private void OnDisable()
        {
            ItemAdded -= OnItemAdded;
            ItemRemoved -= OnItemRemoved;
            NetworkScene.DestroyedGameObject -= OnObjectDestroyed;
        }

        private void Update()
        {
            if (Input.GetButtonDown(m_PickupButton))
            {
                m_PickupInput = true;
            }
        }

        private void FixedUpdate()
        {
            if (!IsOwner)
                return;

            if (m_PickupInput)
            {
                Camera cam = Camera.main;

                Ray pickupRay = new Ray(cam.transform.position, cam.transform.forward);

                if (Physics.SphereCast(pickupRay, m_PickupRayRadius, out RaycastHit hit, m_PickupDistance))
                {
                    InventoryItem item = hit.collider.GetComponent<InventoryItem>();

                    if (item != null)
                    {
                        AddItem(item, m_Collections[m_DefaultCollectionIndex]);
                    }
                }
            }

            m_PickupInput = false;
        }

        #endregion

        #region PUBLIC

        /// <summary>
        /// Initialize the collections.
        /// </summary>
        public override void OnOwnerInitialize()
        {
            foreach (ItemCollection collection in m_Collections)
            {
                collection.Initialize(this);
            }
        }

        /// <summary>
        /// Attempts to craft an item based on the current crafting inventory.
        /// </summary>
        public void CraftItem()
        {
            if (m_CraftingCollectionIndex == -1)
                return;

            if (NetworkController.Instance.IsServer)
            {
                Craft();
            }
            else
            {
                NetworkController.Instance.RemoteProcedures.Call(Identity, RPCType.ServerOnly, nameof(ServerRpcCraft));
            }
        }

        /// <summary>
        /// Drops an item in the collection from the specified slot.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="slot"></param>
        /// <param name="collection"></param>
        public void DropItem(InventoryItem item, int slot, ItemCollection collection)
        {
            if (NetworkController.Instance.IsServer)
            {
                Drop(item, slot, collection);
            }
            else
            {
                NetworkController.Instance.RemoteProcedures.Call(Identity, RPCType.ServerOnly, nameof(ServerRpcDrop), item.InstanceID, slot, GetCollectionIndex(collection));
            }
        }

        /// <summary>
        /// Add an item to a specific collection.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="collection"></param>
        public void AddItem(InventoryItem item, ItemCollection collection)
        {
            if (NetworkController.Instance.IsServer)
            {
                Add(item, collection);
            }
            else
            {
                NetworkController.Instance.RemoteProcedures.Call(Identity, RPCType.ServerOnly, nameof(ServerRpcAdd), item.InstanceID, GetCollectionIndex(collection));
            }
        }

        /// <summary>
        /// Move an item to a slot in the specified collection. (Move was made in the same collection)
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="fromSlot">The slot the item was moved from.</param>
        /// <param name="toSlot">The slot to move to.</param>
        /// <param name="collection">The collection in which the item was moved.</param>
        public void MoveItem(InventoryItem item, int fromSlot, int toSlot, ItemCollection collection)
        {
            MoveItem(item, fromSlot, collection, toSlot, collection);
        }

        /// <summary>
        /// Move an item to a slot in the specified collection.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="fromSlot">The slot the item was moved from.</param>
        /// <param name="fromCollection">The collection that the item is being moved from.</param>
        /// <param name="toSlot">The slot the item is being moved to.</param>
        /// <param name="toCollection">The slot that the item is being moved to.</param>
        public void MoveItem(InventoryItem item, int fromSlot, ItemCollection fromCollection, int toSlot, ItemCollection toCollection)
        {
            if (IsServer)
            {
                Move(item, fromSlot, fromCollection, toSlot, toCollection);
            }
            else
            {
                NetworkController.Instance.RemoteProcedures.Call(Identity, RPCType.ServerOnly, nameof(ServerRpcMove), item.InstanceID, fromSlot, GetCollectionIndex(fromCollection), toSlot, GetCollectionIndex(toCollection));
            }
        }

        #endregion

        #region PRIVATE

        private void OnObjectDestroyed(NetworkIdentity identity)
        {
            InventoryItem item = identity.GetComponent<InventoryItem>();
            if (item != null)
                RemoveItemReferences(item, false);
        }

        private void OnItemRemoved(ItemCollection oldCollection, InventoryItem item, int oldSlot)
        {
            item.NotifyItemRemoved(oldCollection, oldSlot);
        }

        private void OnItemAdded(ItemCollection collection, InventoryItem item, int slot)
        {
            item.NotifyItemAdded(collection, slot);
        }

        private int GetCollectionIndex(ItemCollection collection)
        {
            return Array.IndexOf(m_Collections, collection);
        }

        private ItemCollection GetCollectionFromIndex(int index)
        {
            return m_Collections[index];
        }

        private void Craft()
        {
            ItemCollection craftCollection = m_Collections[m_CraftingCollectionIndex];
            ItemBlueprint blueprint = ItemDatabase.GetBlueprint(craftCollection.ToArray());
            if (blueprint == null)
                return;

            ItemCollection defaultCollection = m_Collections[m_DefaultCollectionIndex];
            if (defaultCollection.IsFull(blueprint.Output))
                return;
            if (!defaultCollection.IsAllowed(blueprint.Output))
                return;

            craftCollection.Clear(true);

            GameObject obj = NetworkController.Instance.Scene.CreateForClient(Identity.OwnerConnection, blueprint.Output.gameObject, Vector3.zero, Quaternion.identity);
            InventoryItem item = obj.GetComponent<InventoryItem>();

            Add(item, defaultCollection);

            NetworkController.Instance.RemoteProcedures.Call(Identity, RPCType.Target, nameof(ClientRpcClearCollection), Identity.OwnerConnection, GetCollectionIndex(craftCollection));
        }

        private void Add(InventoryItem item, ItemCollection collection)
        {
            if (item.Stack <= 0)
                return;

            if (!collection.IsAllowed(item))
                return;

            if (collection.IsFull(item))
                return;

            InventoryItem[] foundItems = Array.FindAll(collection.ToArray(), x => x != null && x.ItemID == item.ItemID && x.Stack < x.MaxStack);
            bool amountChanged = false;

            if (foundItems.Length > 0)
            {
                for (int i = 0; i < foundItems.Length; i++)
                {
                    InventoryItem foundItem = foundItems[i];
                    int count = foundItem.MaxStack - foundItem.Stack;

                    for (int j = 0; j < count; j++)
                    {
                        foundItem.Stack++;
                        item.Stack--;
                        amountChanged = true;

                        if (item.Stack == 0)
                        {
                            NetworkController.Instance.Scene.Destroy(item.gameObject);
                            NotifyStackChanged(foundItem, collection);
                            return;
                        }
                    }

                    if (amountChanged)
                        NotifyStackChanged(foundItem, collection);
                }
            }

            if (amountChanged)
                NotifyStackChanged(item, collection);

            int slot = collection.FirstEmptySlot(item.Category);
            if (slot == -1)
                return;

            collection[slot] = item;
            NetworkController.Instance.Scene.RegisterObjectAuthority(Identity.OwnerConnection, item.Identity);
            ItemAdded?.Invoke(collection, item, slot);

            int collectionIndex = GetCollectionIndex(collection);
            NetworkController.Instance.RemoteProcedures.Call(Identity, RPCType.Target, nameof(ClientRpcMoveItem), Identity.OwnerConnection, item.InstanceID, collectionIndex, collectionIndex, slot, slot);
            NetworkController.Instance.RemoteProcedures.Call(Identity, RPCType.All, nameof(SharedRpcClientClaim), item.InstanceID);
            SharedRpcClientClaim(item.InstanceID);
        }

        private void Drop(InventoryItem item, int slot, ItemCollection collection)
        {
            if (!collection.Contains(item))
                return;

            if (collection[slot] != item)
                return;

            if (!collection.IsReferenceCollection)
            {
                NetworkController.Instance.Scene.UnregisterObjectAuthority(item.Identity);
            }

            Vector3 dropPos = transform.position + transform.forward + Vector3.up;
            Quaternion dropRot = transform.rotation;
            SharedRpcClientReleaseClaim(item.InstanceID, slot, GetCollectionIndex(collection), dropPos, dropRot);

            NetworkController.Instance.RemoteProcedures.Call(
                Identity,
                RPCType.All,
                nameof(SharedRpcClientReleaseClaim),
                item.InstanceID,
                slot,
                GetCollectionIndex(collection),
                dropPos,
                dropRot
                );
        }

        private void Move(InventoryItem fromItem, int fromSlot, ItemCollection fromCollection, int toSlot, ItemCollection toCollection)
        {
            if (!toCollection.IsAllowed(fromItem, toSlot))
                return;

            if (toCollection != fromCollection && toCollection.IsReferenceCollection && toCollection.Contains(fromItem))
                return;

            InventoryItem toItem = toCollection[toSlot];
            if (toItem != null)
            {
                // From and to reference collections cannot be combined or swapped only replaced and destroyed..
                if (!fromCollection.IsReferenceCollection && !toCollection.IsReferenceCollection)
                {
                    if (toCollection.CanStackInCollection && toItem.ItemID == fromItem.ItemID && toItem.Stack < toItem.MaxStack)
                    {
                        Combine(fromItem, toItem, fromCollection, toCollection);
                        return;
                    }

                    Swap(fromItem, fromCollection, fromSlot, toItem, toCollection, toSlot);
                    return;
                }
            }

            if (!toCollection.IsReferenceCollection && !toCollection.CanStackInCollection && fromItem.Stack > 1)
            {
                fromItem.Stack--;
                NotifyStackChanged(fromItem, fromCollection);

                GameObject inst = NetworkController.Instance.Scene.CreateForClient(
                    Identity.OwnerConnection,
                    m_ItemDatabase.GetItem(fromItem.ItemID).gameObject,
                    Vector3.zero, Quaternion.identity
                    );

                NetworkIdentity identity = inst.GetComponent<NetworkIdentity>();
                NetworkController.Instance.RemoteProcedures.Call(Identity, RPCType.All, nameof(SharedRpcClientClaim), identity.InstanceID);
                SharedRpcClientClaim(identity.InstanceID);
                toItem = identity.GetComponent<InventoryItem>();
                toCollection[toSlot] = toItem;

                ItemMoved?.Invoke(fromCollection, toCollection, fromSlot, toSlot, toItem);
                ItemAdded?.Invoke(toCollection, toItem, toSlot);

                NetworkController.Instance.RemoteProcedures.Call(
                    Identity,
                    RPCType.Target,
                    nameof(ClientRpcMoveItem),
                    Identity.OwnerConnection,
                    toItem.InstanceID,
                    GetCollectionIndex(toCollection), GetCollectionIndex(toCollection),
                    toSlot, toSlot);
                return;
            }

            // You can't move an item out of a reference collection...
            if (fromCollection.IsReferenceCollection && fromCollection != toCollection)
                return;

            bool contains = toCollection.Contains(fromItem);

            // Do not clear the item if it's a reference collection we're dragging to.
            if (fromCollection == toCollection || !toCollection.IsReferenceCollection)
                fromCollection[fromSlot] = null;

            toCollection[toSlot] = fromItem;
            if (!contains)
            {
                ItemAdded?.Invoke(toCollection, fromItem, toSlot);
            }
            else
            {
                ItemMoved?.Invoke(fromCollection, toCollection, fromSlot, toSlot, fromItem);
            }

            // Only need to invoke removal if the item becomes null.
            if (fromCollection[fromSlot] == null && fromCollection != toCollection)
            {
                ItemRemoved?.Invoke(fromCollection, fromItem, fromSlot);
            }

            NetworkController.Instance.RemoteProcedures.Call(
                Identity,
                RPCType.Target,
                nameof(ClientRpcMoveItem),
                Identity.OwnerConnection,
                fromItem.InstanceID,
                GetCollectionIndex(fromCollection),
                GetCollectionIndex(toCollection),
                fromSlot,
                toSlot);
        }

        private void Combine(InventoryItem sourceItem, InventoryItem targetItem, ItemCollection sourceCollection, ItemCollection targetCollection)
        {
            int amountNeeded = targetItem.MaxStack - targetItem.Stack;
            if (sourceItem.Stack > amountNeeded)
            {
                sourceItem.Stack -= amountNeeded;
                targetItem.Stack += amountNeeded;
                NotifyStackChanged(sourceItem, sourceCollection);
                NotifyStackChanged(targetItem, targetCollection);
            }
            else
            {
                int amount = sourceItem.Stack;
                NetworkController.Instance.Scene.Destroy(sourceItem.gameObject);
                targetItem.Stack += amount;
                NotifyStackChanged(targetItem, targetCollection);
            }
        }

        private void Swap(InventoryItem sourceItem, ItemCollection sourceCollection, int sourceSlot, InventoryItem targetItem, ItemCollection targetCollection, int targetSlot)
        {
            sourceCollection[sourceSlot] = targetItem;
            targetCollection[targetSlot] = sourceItem;

            ItemSwapped?.Invoke(targetCollection, sourceItem, targetSlot, sourceCollection, targetItem, sourceSlot);

            if (sourceCollection != targetCollection)
            {
                ItemRemoved?.Invoke(sourceCollection, sourceItem, sourceSlot);
                ItemRemoved?.Invoke(targetCollection, targetItem, targetSlot);
            }

            NetworkController.Instance.RemoteProcedures.Call(
                Identity,
                RPCType.Target,
                nameof(ClientRpcSwapItem),
                Identity.OwnerConnection,
                sourceItem.InstanceID,
                GetCollectionIndex(sourceCollection),
                sourceSlot,
                targetItem.InstanceID,
                GetCollectionIndex(targetCollection),
                targetSlot
                );
        }

        private void RemoveItemReferences(InventoryItem item, bool onlyReference = true)
        {
            ItemCollection[] references = Array.FindAll(m_Collections, x => onlyReference ? x.IsReferenceCollection && x.Contains(item) : x.Contains(item));
            foreach (ItemCollection col in references)
            {
                int s = col.GetItemSlot(item);
                col[s] = null;
                col.RepaintUI();
            }
        }

        private void NotifyStackChanged(InventoryItem item, ItemCollection collection)
        {
            NetworkController.Instance.RemoteProcedures.Call(Identity, RPCType.All, nameof(ClientRpcSetItemStack), item.InstanceID, item.Stack, GetCollectionIndex(collection));
        }

        [NetworkRPC]
        private void ServerRpcCraft()
        {
            if (m_CraftingCollectionIndex == -1 || m_CraftingCollectionIndex >= m_Collections.Length)
                return;

            if (m_DefaultCollectionIndex == -1 || m_DefaultCollectionIndex >= m_Collections.Length)
                return;

            Craft();
        }

        [NetworkRPC]
        private void ServerRpcDrop(int instanceID, int slot, int itemCollection)
        {
            try
            {
                NetworkIdentity identity = NetworkIdentityManager.Instance.Get(instanceID);

                InventoryItem item = identity.GetComponent<InventoryItem>();

                if (item.Identity.OwnerConnection == null)
                    return;

                ItemCollection collection = GetCollectionFromIndex(itemCollection);

                Drop(item, slot, collection);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }

        [NetworkRPC]
        private void ServerRpcAdd(int instanceID, int itemCollection)
        {
            try
            {
                NetworkIdentity identity = NetworkIdentityManager.Instance.Get(instanceID);

                InventoryItem item = identity.GetComponent<InventoryItem>();

                if (item.Identity.OwnerConnection != null)
                    return;

                ItemCollection collection = GetCollectionFromIndex(itemCollection);

                Add(item, collection);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }

        [NetworkRPC]
        private void ServerRpcMove(int instanceID, int fromSlot, int fromCollectionID, int toSlot, int toCollectionID)
        {
            try
            {
                NetworkIdentity identity = NetworkIdentityManager.Instance.Get(instanceID);

                InventoryItem item = identity.GetComponent<InventoryItem>();

                if (item.Identity.OwnerConnection != identity.OwnerConnection)
                    return;

                ItemCollection fromCollection = GetCollectionFromIndex(fromCollectionID);
                ItemCollection toCollection = GetCollectionFromIndex(toCollectionID);

                Move(item, fromSlot, fromCollection, toSlot, toCollection);
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
            }
        }

        [NetworkRPC]
        private void SharedRpcClientReleaseClaim(int instanceID, int slot, int collectionIndex, Vector3 pos, Quaternion rot)
        {
            ItemCollection collection = GetCollectionFromIndex(collectionIndex);
            if (!collection.IsReferenceCollection)
            {
                NetworkIdentity identity = NetworkIdentityManager.Instance.Get(instanceID);
                identity.transform.position = pos;
                identity.transform.rotation = rot;
                identity.gameObject.SetActive(true);
            }

            if (IsOwner || IsServer)
            {
                if (!collection.IsReferenceCollection)
                    RemoveItemReferences(collection[slot]);

                collection[slot] = null;
                collection.RepaintUI();
            }
        }

        [NetworkRPC]
        private void SharedRpcClientClaim(int instanceID)
        {
            NetworkIdentity identity = NetworkIdentityManager.Instance.Get(instanceID);
            identity.gameObject.SetActive(false);
        }

        [NetworkRPC]
        private void ClientRpcSetItemStack(int instanceID, int stack, int collection)
        {
            NetworkIdentity identity = NetworkIdentityManager.Instance.Get(instanceID);
            InventoryItem item = identity.GetComponent<InventoryItem>();
            item.Stack = stack;

            ItemCollection col = GetCollectionFromIndex(collection);
            col.RepaintUI();
        }

        [NetworkRPC]
        private void ClientRpcSwapItem(NetworkConnection conn, int sourceInstanceID, int sourceCollectionIndex, int sourceSlot, int targetInstanceID, int targetCollectionIndex, int targetSlot)
        {
            NetworkIdentity sourceIdentity = NetworkIdentityManager.Instance.Get(sourceInstanceID);
            InventoryItem sourceItem = sourceIdentity.GetComponent<InventoryItem>();
            ItemCollection sourceCollection = GetCollectionFromIndex(sourceCollectionIndex);

            NetworkIdentity targetIdentity = NetworkIdentityManager.Instance.Get(targetInstanceID);
            InventoryItem targetItem = targetIdentity.GetComponent<InventoryItem>();
            ItemCollection targetCollection = GetCollectionFromIndex(targetCollectionIndex);

            sourceCollection[sourceSlot] = targetItem;
            targetCollection[targetSlot] = sourceItem;

            ItemSwapped?.Invoke(targetCollection, sourceItem, targetSlot, sourceCollection, targetItem, sourceSlot);

            if (sourceCollection == targetCollection)
            {
                sourceCollection.RepaintUI();
                return;
            }

            ItemRemoved?.Invoke(sourceCollection, sourceItem, sourceSlot);
            ItemRemoved?.Invoke(targetCollection, targetItem, targetSlot);

            sourceCollection.RepaintUI();
            targetCollection.RepaintUI();
        }

        [NetworkRPC]
        private void ClientRpcMoveItem(NetworkConnection conn, int instanceID, int fromCollectionIndex, int toCollectionIndex, int fromSlot, int toSlot)
        {
            NetworkIdentity identity = NetworkIdentityManager.Instance.Get(instanceID);
            InventoryItem item = identity.GetComponent<InventoryItem>();
            ItemCollection fromCollection = GetCollectionFromIndex(fromCollectionIndex);
            ItemCollection toCollection = GetCollectionFromIndex(toCollectionIndex);

            bool contains = toCollection.Contains(item);

            if (fromCollection == toCollection || !toCollection.IsReferenceCollection)
                fromCollection[fromSlot] = null;

            toCollection[toSlot] = item;

            if (!contains)
            {
                ItemAdded?.Invoke(toCollection, item, toSlot);
            }
            else
            {
                ItemMoved?.Invoke(fromCollection, toCollection, fromSlot, toSlot, item);
            }

            if (toCollection == fromCollection)
            {
                toCollection.RepaintUI();
                return;
            }

            if (fromCollection[fromSlot] == null)
                ItemRemoved?.Invoke(fromCollection, item, fromSlot);

            toCollection.RepaintUI();
            fromCollection.RepaintUI();
        }

        [NetworkRPC]
        private void ClientRpcClearCollection(NetworkConnection conn, int collectionIndex)
        {
            ItemCollection col = GetCollectionFromIndex(collectionIndex);
            col.Clear();
            col.RepaintUI();
        }

        #endregion
    }
}
