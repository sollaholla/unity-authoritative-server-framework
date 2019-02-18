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
            if (Application.isPlaying)
            {
                for (int i = 0; i < m_NetworkIdentities.Count; i++)
                {
                    NetworkIdentity identity = m_NetworkIdentities[i];
                    identity.OnInitialize(i, null);
                }
            }
        }

        private void Update()
        {
            if (Application.isEditor && !Application.isBatchMode && !Application.isPlaying)
            {
                NetworkIdentity[] identities = FindObjectsOfType<NetworkIdentity>();
                for (int i = 0; i < identities.Length; i++)
                {
                    NetworkIdentity id = (NetworkIdentity)identities[i];
                    if (m_NetworkIdentities.ContainsKey(i))
                        continue;
                    m_NetworkIdentities[i] = id;
                }

                m_NetworkIdentities = m_NetworkIdentities.Where(x => x.Value != null).ToDictionary(x => x.Key, x => x.Value);

                if (m_NetworkIdentities.Count <= 0)
                    DestroyImmediate(gameObject);
            }
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

        #endregion
    }
}
