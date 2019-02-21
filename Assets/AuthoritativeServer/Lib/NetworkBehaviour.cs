using System;
using System.Collections.Generic;
using UnityEngine;

namespace AuthoritativeServer
{
    [RequireComponent(typeof(NetworkIdentity))]
    public class NetworkBehaviour : MonoBehaviour
    {
        #region FIELDS

        private NetworkIdentity m_Identity;

        private Dictionary<string, Action<object[]>> m_RPCs;

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

        /// <summary>
        /// Register an RPC call with the given name.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="rpcMethod"></param>
        public void RegisterRPC(string name, Action<object[]> rpcMethod)
        {
            if (m_RPCs == null)
                m_RPCs = new Dictionary<string, Action<object[]>>();

            m_RPCs[name] = rpcMethod;
        }

        /// <summary>
        /// Invokes an RPC on this object with the given name.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="arguments"></param>
        public void InvokeRPC(string name, object[] arguments)
        {
            if (m_RPCs == null)
                return;

            if (m_RPCs.TryGetValue(name, out var value))
            {
                value.Invoke(arguments);
            }
        }

        #endregion
    }
}
