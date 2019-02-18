﻿using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace AuthoritativeServer
{
    /// <summary>
    /// A class that allows network instantiations and manages networked objects.
    /// </summary>
    public class NetworkScene
    {
        /// <summary>
        /// The message ID used for network instantiation.
        /// </summary>
        public const short InstantiateMsg = 3;

        /// <summary>
        /// The message ID used for player creation.
        /// </summary>
        public const short CreatePlayerMsg = 4;

        #region PRIVATE

        private Dictionary<int, NetworkWriter> m_BufferedPlayersCreations;
        private List<NetworkConnection> m_BufferedPlayerConnections;
        private Dictionary<int, NetworkPlayerObject> m_PlayerObjectCache;

        #endregion

        public NetworkScene()
        {
            RegisterEvents();
            RegisterHandlers();
        }

        #region PROPERTIES

        /// <summary>
        /// True if the <see cref="NetworkController"/> was initialized as the server.
        /// </summary>
        public bool IsServer { get { return NetworkController.Instance.IsServer; } }

        /// <summary>
        /// The objects that have been spawned in the network scene.
        /// </summary>
        public List<GameObject> SpawnedObjects { get; private set; }

        /// <summary>
        /// The player objects in the network scene.
        /// </summary>
        public List<NetworkPlayerObject> PlayerObjects { get; private set; }

        /// <summary>
        /// The network settings on the <see cref="NetworkController"/>.
        /// </summary>
        public NetworkSettings Settings { get { return NetworkController.Instance.Settings; } }

        #endregion

        #region PRIVATE

        private void OnlineSceneLoaded(Scene scene)
        {
            // When we finally load the online scene
            // debuffer the player spawns if any.
            if (m_BufferedPlayerConnections == null)
                return;

            foreach (NetworkConnection conn in m_BufferedPlayerConnections)
            {
                DebufferPlayerCreations(conn);
            }
        }

        private void OnClientConnected(NetworkConnection conn)
        {
            DebufferPlayerCreations(conn);
        }

        private void OnRemoteConnectionEstablished(NetworkConnection conn)
        {
            DebufferPlayerCreations(conn);
        }

        private void OnRemoteDisconnected(NetworkConnection conn)
        {
            OnDestroyPlayer(conn);
        }

        private void OnClientConnectedToServer(NetworkConnection conn)
        {
            OnCreatePlayer(conn);
        }

        private void OnClientDisconnectedFromServer(NetworkConnection conn)
        {
            OnDestroyPlayer(conn);
        }

        private void RegisterHandlers()
        {
            NetworkController.Instance.RegisterReceiveHandler(InstantiateMsg, OnNetworkInstantiate);
            NetworkController.Instance.RegisterReceiveHandler(CreatePlayerMsg, OnNetworkSpawnPlayer);
        }

        private bool ServerValidateInstantiate(GameObject obj)
        {
            if (!IsServer)
            {
                return false;
            }

            if (!Settings.m_RegisteredObjects.Contains(obj))
            {
                return false;
            }

            if (!obj.GetComponent<NetworkIdentity>())
            {
                return false;
            }

            return true;
        }

        private void DebufferPlayerCreations(NetworkConnection conn)
        {
            if (!NetworkController.Instance.IsOnlineScene)
            {
                if (m_BufferedPlayerConnections == null)
                    m_BufferedPlayerConnections = new List<NetworkConnection>();

                m_BufferedPlayerConnections.Add(conn);
                return;
            }

            if (m_BufferedPlayersCreations == null)
                return;

            if (m_BufferedPlayersCreations.TryGetValue(conn.ConnectionID, out NetworkWriter writer))
            {
                OnNetworkSpawnPlayer(writer);

                m_BufferedPlayersCreations.Remove(conn.ConnectionID);
            }

            m_BufferedPlayerConnections?.Remove(conn);
        }

        private GameObject NetworkCreatePlayer(int connectionID, Vector3 position, Vector3 eulerAngles)
        {
            if (!ServerValidateInstantiate(Settings.m_PlayerObject))
                return null;

            CreateRegisteredObject(Settings.m_PlayerObject, position, eulerAngles, NetworkController.Instance.GetConnection(connectionID), out GameObject inst, out NetworkIdentity instIdentity);

            NetworkWriter writer = GetCreatePlayerWriter(connectionID, inst, instIdentity);

            NetworkController.Instance.SendToAll(writer.ToArray(), CreatePlayerMsg);

            if (PlayerObjects == null)
                PlayerObjects = new List<NetworkPlayerObject>();

            if (m_PlayerObjectCache == null)
                m_PlayerObjectCache = new Dictionary<int, NetworkPlayerObject>();

            NetworkPlayerObject netPlayer = new NetworkPlayerObject(connectionID, inst);
            PlayerObjects.Add(netPlayer);
            m_PlayerObjectCache[connectionID] = netPlayer;

            return inst;
        }

        private GameObject ClientInstantiateServerObject(int netID, Vector3 position, Quaternion rotation, GameObject prefab, NetworkConnection conn)
        {
            GameObject inst = Object.Instantiate(prefab, position, rotation);

            NetworkIdentity netIdentity = inst.GetComponent<NetworkIdentity>();

            NetworkIdentityManager.Instance.RegisterNetworkIdentityManually(netIdentity, conn, netID);

            if (SpawnedObjects == null)
                SpawnedObjects = new List<GameObject>();

            SpawnedObjects.Add(inst);

            return inst;
        }

        private void CreateRegisteredObject(GameObject prefab, Vector3 position, Vector3 eulerAngles, NetworkConnection conn, out GameObject inst, out NetworkIdentity instIdentity)
        {
            inst = Object.Instantiate(prefab, position, Quaternion.Euler(eulerAngles));

            instIdentity = inst.GetComponent<NetworkIdentity>();

            NetworkIdentityManager.Instance.RegisterNetworkIdentity(instIdentity, conn);

            if (SpawnedObjects == null)
                SpawnedObjects = new List<GameObject>();

            SpawnedObjects.Add(inst);
        }

        private NetworkWriter GetCreatePlayerWriter(int connectionID, GameObject instance, NetworkIdentity identity)
        {
            NetworkWriter writer = GetInstantiationWriter(Settings.m_PlayerObject, instance, identity);

            writer.Write((short)connectionID);

            return writer;
        }

        private NetworkWriter GetInstantiationWriter(GameObject prefab, GameObject instance, NetworkIdentity identity)
        {
            NetworkWriter info = new NetworkWriter();

            info.Write((short)Settings.m_RegisteredObjects.IndexOf(prefab));

            info.Write((short)identity.InstanceID);

            info.Write(instance.transform.position);

            info.Write(instance.transform.rotation.eulerAngles);

            return info;
        }

        private void ReadInstantiationMessage(NetworkWriter message, out int netID, out Vector3 position, out Quaternion rotation, out GameObject prefab)
        {
            int objectID = message.ReadInt16();

            netID = message.ReadInt16();

            position = message.ReadVector3();

            rotation = Quaternion.Euler(message.ReadVector3());

            prefab = Settings.m_RegisteredObjects[objectID];
        }

        private void OnNetworkInstantiate(NetworkWriter writer)
        {
            if (IsServer)
                return;

            ReadInstantiationMessage(writer, out int netID, out Vector3 position, out Quaternion rotation, out GameObject prefab);

            if (NetworkIdentityManager.Instance.Exists(netID))
            {
                return;
            }

            ClientInstantiateServerObject(netID, position, rotation, prefab, null);
        }

        private void OnNetworkSpawnPlayer(NetworkWriter writer)
        {
            if (IsServer)
                return;

            ReadInstantiationMessage(writer, out int netID, out Vector3 position, out Quaternion rotation, out GameObject prefab);

            int connectionID = writer.ReadInt16();

            if (NetworkIdentityManager.Instance.Exists(netID))
            {
                return;
            }

            NetworkConnection conn = NetworkController.Instance.GetConnection(connectionID);

            // If we're not ready for some reason we need
            // to buffer the player spawn.
            if (conn == null || !NetworkController.Instance.IsOnlineScene)
            {
                if (m_BufferedPlayersCreations == null)
                    m_BufferedPlayersCreations = new Dictionary<int, NetworkWriter>();

                m_BufferedPlayersCreations[connectionID] = new NetworkWriter(writer.ToArray());
                return;
            }

            GameObject inst = ClientInstantiateServerObject(netID, position, rotation, prefab, conn);

            conn.SetConnectionObject(inst);

            inst.name += " [Connection " + connectionID + "]";

            if (PlayerObjects == null)
                PlayerObjects = new List<NetworkPlayerObject>();

            if (m_PlayerObjectCache == null)
                m_PlayerObjectCache = new Dictionary<int, NetworkPlayerObject>();

            NetworkPlayerObject netPlayer = new NetworkPlayerObject(connectionID, inst);
            PlayerObjects.Add(netPlayer);
            m_PlayerObjectCache[connectionID] = netPlayer;
        }

        private void OnCreatePlayer(NetworkConnection conn)
        {
            if (!IsServer)
                return;

            if (PlayerObjects != null)
            {
                foreach (NetworkPlayerObject playerObject in PlayerObjects)
                {
                    NetworkWriter writer = GetCreatePlayerWriter(playerObject.ConnectionID, playerObject.GameObject, playerObject.NetworkIdentity);

                    conn.Send(0, CreatePlayerMsg, writer.ToArray());
                }
            }

            GameObject player = NetworkCreatePlayer(conn.ConnectionID, Vector3.zero, Vector3.zero);

            NetworkController.Instance.GetConnection(conn.ConnectionID).SetConnectionObject(player);
        }

        private void OnDestroyPlayer(NetworkConnection conn)
        {
            NetworkPlayerObject playerObject = PlayerObjects?.Find(x => x.ConnectionID == conn.ConnectionID);

            if (playerObject != null)
            {
                if (SpawnedObjects?.Contains(playerObject.GameObject) ?? false)
                {
                    SpawnedObjects.Remove(playerObject.GameObject);
                }

                Object.Destroy(playerObject.GameObject);

                PlayerObjects.Remove(playerObject);

                m_PlayerObjectCache?.Remove(conn.ConnectionID);
            }

            if (m_BufferedPlayersCreations?.ContainsKey(conn.ConnectionID) ?? false)
            {
                m_BufferedPlayersCreations.Remove(conn.ConnectionID);
            }

            m_BufferedPlayerConnections?.Remove(conn);
        }

        private void RegisterEvents()
        {
            NetworkController.RemoteConnectionEstablished += OnRemoteConnectionEstablished;
            NetworkController.RemoteDisconnected += OnRemoteDisconnected;
            NetworkController.ServerClientConnected += OnClientConnectedToServer;
            NetworkController.ServerClientDisconnected += OnClientDisconnectedFromServer;
            NetworkController.ClientConnectionEstablished += OnClientConnected;
            NetworkController.OnOnlineSceneLoaded += OnlineSceneLoaded;
        }

        private void UnregisterEvents()
        {
            NetworkController.RemoteConnectionEstablished -= OnRemoteConnectionEstablished;
            NetworkController.RemoteDisconnected -= OnRemoteDisconnected;
            NetworkController.ServerClientConnected -= OnClientConnectedToServer;
            NetworkController.ServerClientDisconnected -= OnClientDisconnectedFromServer;
        }

        #endregion

        #region PUBLIC

        /// <summary>
        /// Instantiate a registered network gameObject on the server.
        /// </summary>
        /// <param name="registeredGameObject">The registered object.</param>
        /// <param name="position">The position of the object.</param>
        /// <param name="eulerAngles">The rotation of the object.</param>
        /// <returns>The instantiated gameObject.</returns>
        public GameObject NetworkInstantiate(GameObject registeredGameObject, Vector3 position, Vector3 eulerAngles)
        {
            if (!ServerValidateInstantiate(registeredGameObject))
                return null;

            CreateRegisteredObject(registeredGameObject, position, eulerAngles, null, out GameObject inst, out NetworkIdentity instIdentity);

            NetworkWriter info = GetInstantiationWriter(registeredGameObject, inst, instIdentity);

            NetworkController.Instance.SendToAll(info.ToArray(), InstantiateMsg);

            return inst;
        }

        /// <summary>
        /// Get the player object with the given connectionID.
        /// </summary>
        /// <param name="connectionID"></param>
        /// <returns></returns>
        public NetworkPlayerObject GetPlayer(int connectionID)
        {
            if (m_PlayerObjectCache.TryGetValue(connectionID, out NetworkPlayerObject player))
            {
                return player;
            }

            return null;
        }

        /// <summary>
        /// Cleanup all data.
        /// </summary>
        public void Clear()
        {
            if (SpawnedObjects != null)
            {
                foreach (GameObject obj in SpawnedObjects)
                    Object.Destroy(obj);

                SpawnedObjects.Clear();
            }

            if (PlayerObjects != null)
            {
                foreach (NetworkPlayerObject obj in PlayerObjects)
                    Object.Destroy(obj.GameObject);

                PlayerObjects.Clear();
            }

            m_BufferedPlayersCreations?.Clear();
            m_BufferedPlayerConnections?.Clear();
            m_PlayerObjectCache?.Clear();
            UnregisterEvents();
        }

        #endregion
    }
}