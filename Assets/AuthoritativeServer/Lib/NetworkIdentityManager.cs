using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

namespace AuthoritativeServer
{
    /// <summary>
    /// A class that manages all scene <see cref="NetworkIdentity"/> objects.
    /// </summary>
    [ExecuteInEditMode]
    public class NetworkIdentityManager : MonoBehaviour
    {
        #region FIELDS

        [SerializeField]
        private List<SceneIdentity> m_SceneIdentities;

        private static NetworkIdentityManager m_Instance;

        private Dictionary<int, NetworkIdentity> m_NetworkIdentities;

        #endregion

        public NetworkIdentityManager()
        {
            m_NetworkIdentities = new Dictionary<int, NetworkIdentity>();
        }

        #region PROPERTIES

        /// <summary>
        /// This scene's instance of the network identity manager.
        /// </summary>
        public static NetworkIdentityManager Instance {
            get {
                if (m_Instance == null)
                {
                    m_Instance = FindObjectOfType<NetworkIdentityManager>() ?? new GameObject("Network Identity Manager").AddComponent<NetworkIdentityManager>();
                }
                return m_Instance;
            }
        }

        #endregion

        #region UNITY

        private void Awake()
        {
            Initialize();
        }

        private void Update()
        {
            EditorUpdate();
        }

        #endregion

        #region PRIVATE

        private void Initialize()
        {
            if (Application.isPlaying)
            {
                if (m_SceneIdentities == null)
                    return;

                if (NetworkController.Instance.IsServer)
                {
                    for (int i = 0; i < m_SceneIdentities.Count; i++)
                    {
                        NetworkIdentity identity = m_SceneIdentities[i];
                        identity.gameObject.name = m_SceneIdentities[i].PrefabName;
                        m_NetworkIdentities[i] = identity;
                        identity.OnInitialize(i, null);
                        NetworkController.Instance.Scene.RegisterObjectAsSpawned(identity.gameObject);
                    }
                }
                else
                {
                    for (int i = 0; i < m_SceneIdentities.Count; i++)
                        Destroy(m_SceneIdentities[i].NetworkIdentity.gameObject);
                    m_SceneIdentities?.Clear();
                    m_SceneIdentities = null;
                }
            }
        }

        private void EditorUpdate()
        {
#if UNITY_EDITOR
            if (Application.isEditor && !Application.isBatchMode && !Application.isPlaying)
            {
                NetworkIdentity[] identities = FindObjectsOfType<NetworkIdentity>();
                for (int i = 0; i < identities.Length; i++)
                {
                    NetworkIdentity id = identities[i];
                    GameObject prefab = UnityEditor.PrefabUtility.GetCorrespondingObjectFromSource(id.gameObject);
                    if (prefab == null)
                        continue;

                    if (m_SceneIdentities == null)
                        m_SceneIdentities = new List<SceneIdentity>();

                    if (m_SceneIdentities.Any(x => x.NetworkIdentity == id))
                        continue;

                    m_SceneIdentities.Add(new SceneIdentity(id, prefab.name));
                }

                m_SceneIdentities = m_SceneIdentities.Where(x => x.NetworkIdentity != null).ToList();

                if (m_SceneIdentities.Count <= 0)
                    DestroyImmediate(gameObject);
            }
#endif
        }

        #endregion

        #region PUBLIC

        /// <summary>
        /// This function will internally register this <see cref="NetworkIdentity"/> and initialize it.
        /// </summary>
        /// <param name="identity">The network identity.</param>
        public void RegisterNetworkIdentity(NetworkIdentity identity, NetworkConnection owner)
        {
            if (identity == null)
                return;

            if (m_NetworkIdentities.ContainsValue(identity))
                return;

            int instanceID = -1;
            foreach (var kvp in m_NetworkIdentities)
            {
                if (kvp.Value == null)
                {
                    instanceID = kvp.Key;
                    break;
                }
            }
            
            if (instanceID == -1)
                instanceID = m_NetworkIdentities.Count;

            m_NetworkIdentities[instanceID] = identity;
            identity.OnInitialize(instanceID, owner);
        }

        /// <summary>
        /// Manually register a network identity with the id given.
        /// </summary>
        /// <param name="identity">The network identity.</param>
        /// <param name="instanceID">The instance ID of the network identity.</param>
        public void RegisterNetworkIdentityManually(NetworkIdentity identity, NetworkConnection owner, int instanceID)
        {
            if (identity == null)
                return;

            if (m_NetworkIdentities.ContainsKey(instanceID) && m_NetworkIdentities[instanceID] != null)
                Destroy(m_NetworkIdentities[instanceID].gameObject);

            m_NetworkIdentities[instanceID] = identity;
            identity.OnInitialize(instanceID, owner);
        }

        /// <summary>
        /// Checks if the specified <see cref="NetworkIdentity"/> exists and is registered.
        /// </summary>
        /// <param name="identity"></param>
        /// <returns></returns>
        public bool Exists(NetworkIdentity identity)
        {
            return m_NetworkIdentities.ContainsValue(identity);
        }

        /// <summary>
        /// Checks if the specified instance ID exists as a <see cref="NetworkIdentity"/>.
        /// </summary>
        /// <param name="instanceID"></param>
        /// <returns></returns>
        public bool Exists(int instanceID)
        {
            return m_NetworkIdentities.ContainsKey(instanceID) && m_NetworkIdentities[instanceID] != null;
        }

        /// <summary>
        /// Get the <see cref="NetworkIdentity"/> with the specified instance ID.
        /// </summary>
        /// <param name="instanceID"></param>
        /// <returns></returns>
        public NetworkIdentity Get(int instanceID)
        {
            if (m_NetworkIdentities.TryGetValue(instanceID, out NetworkIdentity value))
            {
                return value;
            }

            return null;
        }

        #endregion
    }

    /// <summary>
    /// Used by the server to rename objects to their prefab names so that they can be loaded from resources when 
    /// spawning the objects for other clients. Each scene identity will also be initialzed at the start of the game
    /// on the server.
    /// </summary>
    [System.Serializable]
    public class SceneIdentity
    {
        [SerializeField]
        private NetworkIdentity m_NetworkIdentity;
        [SerializeField]
        private string m_PrefabName;

        public SceneIdentity(NetworkIdentity netIdentity, string prefabName)
        {
            m_NetworkIdentity = netIdentity;
            m_PrefabName = prefabName;
        }

        /// <summary>
        /// The networked identity object.
        /// </summary>
        public NetworkIdentity NetworkIdentity { get { return m_NetworkIdentity; } }

        /// <summary>
        /// The name of the prefab used for this identity object.
        /// </summary>
        public string PrefabName { get { return m_PrefabName; } }

        public static implicit operator NetworkIdentity(SceneIdentity identity)
        {
            return identity.NetworkIdentity;
        }
    }
}
