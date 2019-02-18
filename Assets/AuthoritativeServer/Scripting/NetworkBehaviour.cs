using UnityEngine;

namespace AuthoritativeServer
{
    [RequireComponent(typeof(NetworkIdentity))]
    public class NetworkBehaviour : MonoBehaviour
    {
        #region FIELDS

        private NetworkIdentity m_Identity;

        #endregion

        #region PROPERTIES

        /// <summary>
        /// The <see cref="NetworkIdentity"/> attached to this object.
        /// </summary>
        public NetworkIdentity Identity {
            get {
                if (m_Identity == null)
                    m_Identity = GetComponent<NetworkIdentity>();
                return m_Identity;
            }
        }

        /// <inheritdoc cref="NetworkIdentity.IsOwner" />
        public bool IsOwner {
            get { return Identity.IsOwner; }
        }

        /// <inheritdoc cref="NetworkIdentity.IsServer" />
        public bool IsServer {
            get { return Identity.IsServer; }
        }

        /// <inheritdoc cref="NetworkIdentity.InstanceID" />
        public int InstanceID {
            get { return Identity.InstanceID; }
        }

        #endregion

        #region PUBLIC

        /// <summary>
        /// Executed on the client after this object is intialized.
        /// </summary>
        public virtual void OnClientInitialize()
        { }

        /// <summary>
        /// Execute on the server after this object is initialized.
        /// </summary>
        public virtual void OnServerInitialize()
        { }

        /// <summary>
        /// Executed on the owner connection after this object is initialized. You can use this
        /// to execute authoritative only logic.
        /// </summary>
        public virtual void OnOwnerInitialize()
        { }

        #endregion
    }
}
