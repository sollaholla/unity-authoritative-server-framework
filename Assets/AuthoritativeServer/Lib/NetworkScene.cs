using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

using static AuthoritativeServer.NetworkMessageHandlers;

namespace AuthoritativeServer
{
    /// <summary>
    /// A class that allows network instantiations and manages networked objects.
    /// </summary>
    public class NetworkScene
    {
        /// <summary>
        /// An event called after create a networked object.
        /// </summary>
        public static event Action<NetworkIdentity> CreatedGameObject;

        /// <summary>
        /// An event called prior to a networked object being destroyed.
        /// </summary>
        public static event Action<NetworkIdentity> DestroyedGameObject;

        #region FIELDS

        private Dictionary<int, NetworkWriter> m_BufferedPlayersCreations;
        private Dictionary<int, List<NetworkWriter>> m_BufferedOwnerCreations;
        private List<NetworkConnection> m_BufferedPlayerConnections;

        private Dictionary<int, NetworkPlayerObject> m_PlayerObjectCache;
        private Dictionary<NetworkConnection, List<NetworkIdentity>> m_AuthroityObjects;

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
            if (m_BufferedPlayerConnections != null)
            {
                foreach (NetworkConnection conn in m_BufferedPlayerConnections)
                {
                    DebufferPlayerCreations(conn);
                }
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

        private void RegisterHandlers()
        {
            NetworkController.Instance.RegisterReceiveHandler(InstantiateMsg, OnNetworkInstantiate);
            NetworkController.Instance.RegisterReceiveHandler(CreatePlayerMsg, OnNetworkSpawnPlayer);
            NetworkController.Instance.RegisterReceiveHandler(DestroyMsg, OnNetworkDestroy);
        }

        private bool ServerValidateInstantiate(GameObject obj)
        {
            if (!IsServer)
            {
                return false;
            }

            if (!obj.GetComponent<NetworkIdentity>())
            {
                return false;
            }

            return true;
        }

        private bool ServerValidateDestroy(GameObject obj, out NetworkIdentity identity)
        {
            identity = null;

            if (!IsServer)
            {
                return false;
            }

            identity = obj.GetComponent<NetworkIdentity>();

            if (identity == null)
            {
                return false;
            }

            if (!NetworkIdentityManager.Instance.Exists(identity.InstanceID))
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

            if (m_BufferedOwnerCreations != null)
            {
                if (m_BufferedOwnerCreations.TryGetValue(conn.ConnectionID, out List<NetworkWriter> writers))
                {
                    if (writers != null)
                    {
                        foreach (NetworkWriter writer in writers)
                            OnNetworkInstantiate(writer);
                    }

                    m_BufferedOwnerCreations.Remove(conn.ConnectionID);
                }
            }

            if (m_BufferedPlayersCreations != null)
            {
                if (m_BufferedPlayersCreations.TryGetValue(conn.ConnectionID, out NetworkWriter writer))
                {
                    OnNetworkSpawnPlayer(writer);

                    m_BufferedPlayersCreations.Remove(conn.ConnectionID);
                }
            }

            m_BufferedPlayerConnections?.Remove(conn);
        }

        private void DebufferObjectSpawns(NetworkConnection conn)
        {
            foreach (GameObject obj in SpawnedObjects)
            {
                if (PlayerObjects.Any(x => x.GameObject == obj))
                    continue;

                NetworkIdentity netID = obj.GetComponent<NetworkIdentity>();
                NetworkWriter writer = GetInstantiationWriter(netID.OwnerConnection?.ConnectionID ?? -1, obj.name.Replace("(Clone)", string.Empty), obj, netID);
                NetworkController.Instance.Send(conn.ConnectionID, NetworkController.ReliableSequencedChannel, InstantiateMsg, writer.ToArray());
            }
        }

        private GameObject NetworkCreatePlayer(int connectionID, Vector3 position, Vector3 eulerAngles)
        {
            if (!ServerValidateInstantiate(Settings.m_PlayerObject))
                return null;

            CreateRegisteredObject(Settings.m_PlayerObject, position, eulerAngles, NetworkController.Instance.GetConnection(connectionID), out GameObject inst, out NetworkIdentity instIdentity);

            NetworkWriter writer = GetCreatePlayerWriter(connectionID, inst, instIdentity);

            NetworkController.Instance.SendToAll(NetworkController.ReliableSequencedChannel, CreatePlayerMsg, writer.ToArray());

            if (PlayerObjects == null)
                PlayerObjects = new List<NetworkPlayerObject>();

            if (m_PlayerObjectCache == null)
                m_PlayerObjectCache = new Dictionary<int, NetworkPlayerObject>();

            NetworkPlayerObject netPlayer = new NetworkPlayerObject(connectionID, inst);

            PlayerObjects.Add(netPlayer);

            m_PlayerObjectCache[connectionID] = netPlayer;

            return inst;
        }

        private GameObject ClientInstantiateServerObject(int netID, Vector3 position, Quaternion rotation, GameObject prefab, NetworkConnection conn, byte[] customData)
        {
            GameObject inst = Object.Instantiate(prefab, position, rotation);
            NetworkIdentity netIdentity = inst.GetComponent<NetworkIdentity>();
            NetworkIdentityManager.Instance.RegisterNetworkIdentityManually(netIdentity, conn, netID);

            CreatedGameObject?.Invoke(netIdentity);
            RegisterObjectAsSpawned(inst);

            if (customData != null)
                netIdentity.OnDeserialize(customData);

            return inst;
        }

        private void CreateRegisteredObject(GameObject prefab, Vector3 position, Vector3 eulerAngles, NetworkConnection conn, out GameObject inst, out NetworkIdentity instIdentity)
        {
            inst = Object.Instantiate(prefab, position, Quaternion.Euler(eulerAngles));

            instIdentity = inst.GetComponent<NetworkIdentity>();

            NetworkIdentityManager.Instance.RegisterNetworkIdentity(instIdentity, conn);

            CreatedGameObject?.Invoke(instIdentity);

            RegisterObjectAsSpawned(inst);
        }

        public void RegisterObjectAsSpawned(GameObject inst)
        {
            if (SpawnedObjects == null)
                SpawnedObjects = new List<GameObject>();

            SpawnedObjects.Add(inst);
        }

        private NetworkWriter GetCreatePlayerWriter(int connectionID, GameObject instance, NetworkIdentity identity)
        {
            NetworkWriter writer = GetInstantiationWriter(connectionID, Settings.m_PlayerObject.name, instance, identity);

            return writer;
        }

        private NetworkWriter GetInstantiationWriter(int conn, string prefab, GameObject instance, NetworkIdentity identity)
        {
            NetworkWriter info = new NetworkWriter();
            info.Write((short)conn);
            info.Write(prefab);
            info.Write((short)identity.InstanceID);
            info.Write(instance.transform.position);
            info.Write(instance.transform.rotation.eulerAngles);
            info.Write(instance.gameObject.activeInHierarchy);
            byte[] serBytes = identity.OnSerialize(info);
            info.WriteBytesAndSize(serBytes);

            return info;
        }

        private void ReadInstantiationMessage(NetworkWriter message, out int connectionID, out int netID, out Vector3 position, out Quaternion rotation, out GameObject prefab, out bool enabled, out byte[] deserializedBytes)
        {
            connectionID = message.ReadInt16();
            string objectID = message.ReadString();
            netID = message.ReadInt16();
            position = message.ReadVector3();
            rotation = Quaternion.Euler(message.ReadVector3());
            prefab = Resources.Load<GameObject>(objectID);
            enabled = message.ReadBool();
            int count = message.ReadInt16();
            deserializedBytes = message.ReadBytes(count);
        }

        private void OnNetworkDestroy(NetworkWriter writer)
        {
            if (IsServer)
                return;

            int instanceID = writer.ReadInt16();

            NetworkIdentity identity = NetworkIdentityManager.Instance.Get(instanceID);

            SpawnedObjects?.Remove(identity.gameObject);

            DestroyedGameObject?.Invoke(identity);

            Object.Destroy(identity.gameObject);
        }

        private void OnNetworkInstantiate(NetworkWriter writer)
        {
            if (IsServer)
                return;

            ReadInstantiationMessage(writer, out int connectionID, out int netID, out Vector3 position, out Quaternion rotation, out GameObject prefab, out bool enabled, out byte[] customData);

            NetworkConnection conn = NetworkController.Instance.GetConnection(connectionID);

            if ((connectionID != -1 && conn == null) || !NetworkController.Instance.IsOnlineScene)
            {
                if (m_BufferedOwnerCreations == null)
                    m_BufferedOwnerCreations = new Dictionary<int, List<NetworkWriter>>();

                if (!m_BufferedOwnerCreations.ContainsKey(connectionID))
                    m_BufferedOwnerCreations[connectionID] = new List<NetworkWriter>();

                m_BufferedOwnerCreations[connectionID].Add(new NetworkWriter(writer.ToArray()));
                return;
            }

            GameObject obj = ClientInstantiateServerObject(netID, position, rotation, prefab, NetworkController.Instance.GetConnection(connectionID), customData);
            obj.SetActive(enabled);
        }

        private void OnNetworkSpawnPlayer(NetworkWriter writer)
        {
            if (IsServer)
                return;

            ReadInstantiationMessage(writer, out int connectionID, out int netID, out Vector3 position, out Quaternion rotation, out GameObject prefab, out _, out _);

            NetworkConnection conn = NetworkController.Instance.GetConnection(connectionID);

            if (conn == null || !NetworkController.Instance.IsOnlineScene)
            {
                if (m_BufferedPlayersCreations == null)
                    m_BufferedPlayersCreations = new Dictionary<int, NetworkWriter>();

                m_BufferedPlayersCreations[connectionID] = new NetworkWriter(writer.ToArray());
                return;
            }

            GameObject inst = ClientInstantiateServerObject(netID, position, rotation, prefab, conn, null);

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

            CreatePlayers(conn);
            DebufferObjectSpawns(conn);
        }

        private void CreatePlayers(NetworkConnection conn)
        {
            if (PlayerObjects != null)
            {
                try
                {
                    foreach (NetworkPlayerObject playerObject in PlayerObjects)
                    {
                        NetworkWriter writer = GetCreatePlayerWriter(playerObject.ConnectionID, playerObject.GameObject, playerObject.NetworkIdentity);
                        conn.Send(NetworkController.ReliableSequencedChannel, CreatePlayerMsg, writer.ToArray());
                    }
                }
                catch(Exception ex)
                {
                    Debug.LogError(ex);
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
                    SpawnedObjects.Remove(playerObject.GameObject);

                DestroyedGameObject?.Invoke(playerObject.NetworkIdentity);
                Object.Destroy(playerObject.GameObject);

                PlayerObjects.Remove(playerObject);
                m_PlayerObjectCache?.Remove(conn.ConnectionID);
            }

            if (m_BufferedPlayersCreations?.ContainsKey(conn.ConnectionID) ?? false)
                m_BufferedPlayersCreations.Remove(conn.ConnectionID);

            if (m_BufferedOwnerCreations?.ContainsKey(conn.ConnectionID) ?? false)
                m_BufferedOwnerCreations.Remove(conn.ConnectionID);

            if (m_AuthroityObjects?.ContainsKey(conn) ?? false)
            {
                foreach (NetworkIdentity identity in m_AuthroityObjects[conn])
                    if (identity != null)
                        Destroy(identity.gameObject);
                m_AuthroityObjects.Remove(conn);
            }

            m_BufferedPlayerConnections?.Remove(conn);
        }

        private void RegisterEvents()
        {
            NetworkController.RemoteConnected += OnRemoteConnectionEstablished;
            NetworkController.RemoteDisconnected += OnRemoteDisconnected;
            NetworkController.ConnectionEstablished += OnClientConnected;
            NetworkController.OnlineSceneLoaded += OnlineSceneLoaded;
        }

        private void UnregisterEvents()
        {
            NetworkController.RemoteConnected -= OnRemoteConnectionEstablished;
            NetworkController.RemoteDisconnected -= OnRemoteDisconnected;
            NetworkController.ConnectionEstablished -= OnClientConnected;
            NetworkController.OnlineSceneLoaded -= OnlineSceneLoaded;
        }

        #endregion

        #region PUBLIC

        /// <summary>
        /// Executed on the server when a client connects. This will be called before any event
        /// for <see cref="NetworkController.ServerClientConnected"/> is invoked for initialization purposes.
        /// </summary>
        /// <param name="conn"></param>
        public void NotifyClientConnect(NetworkConnection conn)
        {
            OnCreatePlayer(conn);
        }

        /// <summary>
        /// Executed on the server when a client disconnects. This will be called before any event
        /// for <see cref="NetworkController.ServerClientDisconnected"/> is invoked for de-initialization purposes.
        /// </summary>
        /// <param name="conn"></param>
        public void NotifyClientDisconnect(NetworkConnection conn)
        {
            OnDestroyPlayer(conn);
        }

        /// <summary>
        /// Unregisters the specified objects authority from any connection.
        /// </summary>
        /// <param name="identity"></param>
        public void UnregisterObjectAuthority(NetworkIdentity identity)
        {
            if (!IsServer)
            {
                throw new InvalidOperationException(nameof(UnregisterObjectAuthority) + ": This operation can only be run on the server!");
            }

            if (m_AuthroityObjects == null)
                return;

            NetworkConnection conn = identity.OwnerConnection;

            if (conn == null)
                return;

            if (!m_AuthroityObjects.ContainsKey(conn))
                return;

            m_AuthroityObjects[conn]?.Remove(identity);
            identity.SetOwner(null);
        }

        /// <summary>
        /// Set the identities authority to the specified connection.
        /// </summary>
        /// <param name="connection">The network connection.</param>
        /// <param name="identity">The network identity object.</param>
        /// <param name="propagate">True if we wish to propagate the ownership across all clients.</param>
        public void RegisterObjectAuthority(NetworkConnection connection, NetworkIdentity identity)
        {
            if (!IsServer)
            {
                throw new InvalidOperationException(nameof(RegisterObjectAuthority) + ": This operation can only be run on the server!");
            }

            if (connection != null)
            {
                if (m_AuthroityObjects == null)
                {
                    m_AuthroityObjects = new Dictionary<NetworkConnection, List<NetworkIdentity>>();
                }

                if (!m_AuthroityObjects.ContainsKey(connection))
                {
                    m_AuthroityObjects[connection] = new List<NetworkIdentity>();
                }

                if (!m_AuthroityObjects[connection].Contains(identity))
                {
                    m_AuthroityObjects[connection].Add(identity);
                    identity.SetOwner(connection);
                }
            }
        }

        /// <summary>
        /// Instantiate a registered network gameObject on the server.
        /// </summary>
        /// <param name="prefab">The registered object.</param>
        /// <param name="position">The position of the object spawn.</param>
        /// <param name="rotation">The rotation of the object spawn.</param>
        /// <returns>The instantiated gameObject.</returns>
        public GameObject Create(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            return CreateForClient(null, prefab, position, rotation);
        }

        /// <summary>
        /// Instantiate a registered network gameObject on the server owned by a specific client.
        /// </summary>
        /// <param name="connection">The owner connection.</param>
        /// <param name="prefab">The registered object.</param>
        /// <param name="position">The position of the object spawn.</param>
        /// <param name="rotation">The rotation of the object spawn.</param>
        /// <returns>The instantiated gameObject.</returns>
        public GameObject CreateForClient(NetworkConnection connection, GameObject prefab, Vector3 position, Quaternion rotation)
        {
            if (!ServerValidateInstantiate(prefab))
                return null;

            CreateRegisteredObject(prefab, position, rotation.eulerAngles, connection, out GameObject inst, out NetworkIdentity instIdentity);

            RegisterObjectAuthority(connection, instIdentity);

            NetworkWriter info = GetInstantiationWriter(connection?.ConnectionID ?? -1, prefab.name, inst, instIdentity);

            NetworkController.Instance.SendToAll(NetworkController.ReliableSequencedChannel, InstantiateMsg, info.ToArray());

            return inst;
        }

        /// <summary>
        /// Destroy a networked object.
        /// </summary>
        /// <param name="gameObject"></param>
        public void Destroy(GameObject gameObject)
        {
            if (!IsServer)
                return;

            if (!ServerValidateDestroy(gameObject, out NetworkIdentity identity))
                return;

            byte[] data = BitConverter.GetBytes((short)identity.InstanceID);

            NetworkController.Instance.SendToAll(NetworkController.ReliableSequencedChannel, DestroyMsg, data);

            DestroyedGameObject?.Invoke(identity);

            SpawnedObjects.Remove(identity.gameObject);

            Object.Destroy(gameObject);
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
            UnregisterEvents();

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
            m_BufferedOwnerCreations?.Clear();
            m_BufferedPlayerConnections?.Clear();
            m_PlayerObjectCache?.Clear();
            m_AuthroityObjects?.Clear();
            DestroyedGameObject = null;
            CreatedGameObject = null;
        }

        #endregion
    }
}
