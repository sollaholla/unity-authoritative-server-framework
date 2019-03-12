using AuthoritativeServer.Attributes;

using System.Collections.Generic;

using UnityEngine;

namespace AuthoritativeServer.Demo
{
    /// <summary>
    /// Manages inventory equipments.
    /// </summary>
    [AddComponentMenu("Autho Server/Demo/Inventory System/Inventory Equipment")]
    [RequireComponent(typeof(Inventory))]
    [DisallowMultipleComponent]
    public class InventoryEquipment : NetworkBehaviour
    {
        #region INSPECTOR

        [SerializeField]
        private int m_EquipmentCollectionIndex = 1;
        [SerializeField]
        private int m_WeaponCollectionIndex = 2;

        #endregion

        #region FIELDS

        private Inventory m_Inventory;
        private ItemCollection m_EquipmentCollection;
        private ItemCollection m_WeaponCollection;

        private List<EquippableInventoryItem> m_Equippables;
        private List<WeaponInventoryItem> m_Weapons;

        #endregion

        #region PROPERTIES

        /// <summary>
        /// The equippable items.
        /// </summary>
        public EquippableInventoryItem[] Equippables { get { return m_Equippables.ToArray(); } }

        /// <summary>
        /// The weapon items.
        /// </summary>
        public WeaponInventoryItem[] Weapons { get { return m_Weapons.ToArray(); } }

        #endregion

        #region UNITY

        private void Awake()
        {
            m_Equippables = new List<EquippableInventoryItem>();
            m_Weapons = new List<WeaponInventoryItem>();

            RegisterRPC<int>(EquipEquippable);
            RegisterRPC<int>(EquipWeapon);
            RegisterRPC<int>(UnequipEquippable);
            RegisterRPC<int>(UnequipWeapon);
            RegisterRPC<NetworkConnection, int>(EquipEquippableBuffered);
            RegisterRPC<NetworkConnection, int>(EquipWeaponBuffered);

            m_Inventory = GetComponent<Inventory>();
            m_EquipmentCollection = m_Inventory.Collections[m_EquipmentCollectionIndex];
            m_WeaponCollection = m_Inventory.Collections[m_WeaponCollectionIndex];
        }

        private void OnDisable()
        {
            Inventory.ItemAdded -= OnItemAdded;
            Inventory.ItemRemoved -= OnItemRemoved;
            NetworkController.ServerClientConnected -= ServerClientConnected;
        }

        #endregion

        #region PUBLIC

        public override void OnServerInitialize()
        {
            Inventory.ItemAdded += OnItemAdded;
            Inventory.ItemRemoved += OnItemRemoved;
            NetworkController.ServerClientConnected += ServerClientConnected;
        }

        #endregion

        #region PRIVATE

        private void ServerClientConnected(NetworkConnection conn)
        {
            foreach (EquippableInventoryItem item in m_Equippables)
            {
                if (item == null)
                    continue;

                NetworkController.Instance.RemoteProcedures.Call(Identity, RPCType.Target, nameof(EquipEquippableBuffered), conn, item.InstanceID);
            }

            foreach (WeaponInventoryItem item in m_Weapons)
            {
                if (item == null)
                    continue;

                NetworkController.Instance.RemoteProcedures.Call(Identity, RPCType.Target, nameof(EquipWeaponBuffered), conn, item.InstanceID);
            }
        }

        private void OnItemAdded(ItemCollection collection, InventoryItem item, int slot)
        {
            if (ExecuteInventoryProcedure<EquippableInventoryItem>(collection, m_EquipmentCollection, item, nameof(EquipEquippable)))
            {
                m_Equippables.Add(item as EquippableInventoryItem);
            }

            if (ExecuteInventoryProcedure<WeaponInventoryItem>(collection, m_WeaponCollection, item, nameof(EquipWeapon)))
            {
                m_Weapons.Add(item as WeaponInventoryItem);
            }
        }

        private void OnItemRemoved(ItemCollection oldCollection, InventoryItem item, int oldSlot)
        {
            if (ExecuteInventoryProcedure<EquippableInventoryItem>(oldCollection, m_EquipmentCollection, item, nameof(UnequipEquippable)))
            {
                m_Equippables.Remove(item as EquippableInventoryItem);
            }

            if (ExecuteInventoryProcedure<WeaponInventoryItem>(oldCollection, m_WeaponCollection, item, nameof(UnequipWeapon)))
            {
                m_Weapons.Remove(item as WeaponInventoryItem);
            }
        }

        private bool ExecuteInventoryProcedure<T>(ItemCollection sourceCol, ItemCollection targetCol, InventoryItem item, string rpcName) where T : InventoryItem
        {
            if (sourceCol != targetCol)
                return false;

            T tItem = item as T;
            if (tItem == null)
                return false;

            NetworkController.Instance.RemoteProcedures.Call(Identity, RPCType.All, rpcName, tItem.InstanceID);

            return true;
        }

        [NetworkRPC] private void EquipWeaponBuffered(NetworkConnection conn, int instanceID) => EquipWeapon(instanceID);
        [NetworkRPC] private void EquipEquippableBuffered(NetworkConnection conn, int instanceID) => EquipEquippable(instanceID);

        [NetworkRPC]
        private void UnequipWeapon(int instanceID)
        {
            Debug.Log(string.Format("Weapon {0} unequipped from player {1}", instanceID, Identity.OwnerConnection.ConnectionID));

            NetworkIdentity networkIdentity = NetworkIdentityManager.Instance.Get(instanceID);
            WeaponInventoryItem item = networkIdentity.GetComponent<WeaponInventoryItem>();
            m_Weapons.Remove(item);
            WeaponUnequipped(item);
        }

        [NetworkRPC]
        private void UnequipEquippable(int instanceID)
        {
            Debug.Log(string.Format("Item {0} unequipped from player {1}", instanceID, Identity.OwnerConnection.ConnectionID));

            NetworkIdentity networkIdentity = NetworkIdentityManager.Instance.Get(instanceID);
            EquippableInventoryItem item = networkIdentity.GetComponent<EquippableInventoryItem>();
            m_Equippables.Remove(item);
            EquippableUnequipped(item);
        }

        [NetworkRPC]
        private void EquipWeapon(int instanceID)
        {
            Debug.Log(string.Format("Weapon {0} equipped on player {1}", instanceID, Identity.OwnerConnection.ConnectionID));

            NetworkIdentity networkIdentity = NetworkIdentityManager.Instance.Get(instanceID);
            WeaponInventoryItem item = networkIdentity.GetComponent<WeaponInventoryItem>();
            m_Weapons.Add(item);
            WeaponEquipped(item);
        }

        [NetworkRPC]
        private void EquipEquippable(int instanceID)
        {
            Debug.Log(string.Format("Item {0} equipped on player {1}", instanceID, Identity.OwnerConnection.ConnectionID));

            NetworkIdentity networkIdentity = NetworkIdentityManager.Instance.Get(instanceID);
            EquippableInventoryItem item = networkIdentity.GetComponent<EquippableInventoryItem>();
            m_Equippables.Add(item);
            EquippableEquipped(item);
        }

        #endregion

        #region PROTECTED

        protected virtual void EquippableEquipped(EquippableInventoryItem item) { }
        protected virtual void WeaponEquipped(WeaponInventoryItem item) { }

        protected virtual void EquippableUnequipped(EquippableInventoryItem item) { }
        protected virtual void WeaponUnequipped(WeaponInventoryItem item) { }

        #endregion
    }
}
