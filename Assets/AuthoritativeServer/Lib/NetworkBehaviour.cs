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

        private Dictionary<string, Delegate> m_RPCs;

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

        public void RegisterRPC(Action method)
        {
            if (m_RPCs == null)
                m_RPCs = new Dictionary<string, Delegate>();

            Delegate d = Delegate.CreateDelegate(typeof(Action), this, method.Method.Name);
            m_RPCs[method.Method.Name] = d;
        }

        public void RegisterRPC<T1>(Action<T1> method)
        {
            if (m_RPCs == null)
                m_RPCs = new Dictionary<string, Delegate>();

            Delegate d = Delegate.CreateDelegate(typeof(Action<T1>), this, method.Method.Name);
            m_RPCs[method.Method.Name] = d;
        }

        public void RegisterRPC<T1, T2>(Action<T1, T2> method)
        {
            if (m_RPCs == null)
                m_RPCs = new Dictionary<string, Delegate>();

            Delegate d = Delegate.CreateDelegate(typeof(Action<T1, T2>), this, method.Method.Name);
            m_RPCs[method.Method.Name] = d;
        }

        public void RegisterRPC<T1, T2, T3>(Action<T1, T2, T3> method)
        {
            if (m_RPCs == null)
                m_RPCs = new Dictionary<string, Delegate>();

            Delegate d = Delegate.CreateDelegate(typeof(Action<T1, T2, T3>), this, method.Method.Name);
            m_RPCs[method.Method.Name] = d;
        }

        public void RegisterRPC<T1, T2, T3, T4>(Action<T1, T2, T3, T4> method)
        {
            if (m_RPCs == null)
                m_RPCs = new Dictionary<string, Delegate>();

            Delegate d = Delegate.CreateDelegate(typeof(Action<T1, T2, T3, T4>), this, method.Method.Name);
            m_RPCs[method.Method.Name] = d;
        }

        /// <summary>
        /// Executed when the server needs to serialize custom data for this object.
        /// </summary>
        /// <returns></returns>
        public virtual byte[] OnSerialize() { return null; }

        /// <summary>
        /// Executed when the client should deserialize custom data for this object.
        /// </summary>
        /// <param name="data"></param>
        public virtual void OnDeserialize(byte[] data) { }

        public void RegisterRPC<T1, T2, T3, T4, T5>(Action<T1, T2, T3, T4, T5> method)
        {
            if (m_RPCs == null)
                m_RPCs = new Dictionary<string, Delegate>();

            Delegate d = Delegate.CreateDelegate(typeof(Action<T1, T2, T3, T4, T5>), this, method.Method.Name);
            m_RPCs[method.Method.Name] = d;
        }

        public void RegisterRPC<T1, T2, T3, T4, T5, T6>(Action<T1, T2, T3, T4, T5, T6> method)
        {
            if (m_RPCs == null)
                m_RPCs = new Dictionary<string, Delegate>();

            Delegate d = Delegate.CreateDelegate(typeof(Action<T1, T2, T3, T4, T5, T6>), this, method.Method.Name);
            m_RPCs[method.Method.Name] = d;
        }

        public void RegisterRPC<T1, T2, T3, T4, T5, T6, T7>(Action<T1, T2, T3, T4, T5, T6, T7> method)
        {
            if (m_RPCs == null)
                m_RPCs = new Dictionary<string, Delegate>();

            Delegate d = Delegate.CreateDelegate(typeof(Action<T1, T2, T3, T4, T5, T6, T7>), this, method.Method.Name);
            m_RPCs[method.Method.Name] = d;
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
                value.DynamicInvoke(arguments);
            }
        }

        #endregion
    }
}
