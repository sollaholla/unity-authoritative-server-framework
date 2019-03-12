using System;
using System.Collections.Generic;
using AuthoritativeServer.Attributes;
using UnityEngine;

namespace AuthoritativeServer
{
    [AddComponentMenu("Autho Server/Network Identity")]
    [DisallowMultipleComponent]
    public class NetworkIdentity : MonoBehaviour
    {
        #region FIELDS

        private NetworkBehaviour[] m_NetworkBehaviours;

        #endregion

        #region PROPERTIES

        /// <summary>
        /// This identities instance ID.
        /// </summary>
        public int InstanceID { get; private set; } = -1;

        /// <summary>
        /// The owner connection. Will be null if owned by the server.
        /// </summary>
        public NetworkConnection OwnerConnection { get; private set; }

        /// <summary>
        /// True if we're the server.
        /// </summary>
        public bool IsServer { get { return NetworkController.Instance?.IsServer ?? false; } }

        /// <summary>
        /// True if we're the owner of this object.
        /// </summary>
        public bool IsOwner { get { return NetworkController.Instance != null && NetworkController.Instance?.LocalConnectionID == OwnerConnection?.ConnectionID; } }

        /// <summary>
        /// The network behaviours attached to this object.
        /// </summary>
        public NetworkBehaviour[] NetworkBehaviours {
            get {
                if (m_NetworkBehaviours == null)
                    m_NetworkBehaviours = GetComponents<NetworkBehaviour>();

                return m_NetworkBehaviours;
            }
        }

        #endregion

        #region PUBLIC

        /// <summary>
        /// The owner connection.
        /// </summary>
        /// <param name="connection"></param>
        public void SetOwner(NetworkConnection connection)
        {
            OwnerConnection = connection;
        }

        /// <summary>
        /// Executed when this object is intialized by the <see cref="NetworkController"/>. Sets the owner connection, instance ID, and calls initialization functions on <see cref="NetworkBehaviour"/>s.
        /// </summary>
        /// <param name="instanceId">The object's instance ID.</param>
        /// <param name="owner">The owner connection.</param>
        public void OnInitialize(int instanceId, NetworkConnection owner)
        {
            InstanceID = instanceId;
            OwnerConnection = owner;

            foreach (NetworkBehaviour b in NetworkBehaviours)
            {
                if (IsServer)
                {
                    b.OnServerInitialize();
                }
                else
                {
                    b.OnClientInitialize();
                }

                if (IsOwner)
                {
                    b.OnOwnerInitialize();
                }
            }
        }

        public byte[] OnSerialize(NetworkWriter info)
        {
            List<byte> allBytes = new List<byte>();
            foreach (NetworkBehaviour behaviour in NetworkBehaviours)
            {
                byte[] data = behaviour.OnSerialize();
                if (data != null)
                    allBytes.AddRange(data);
            }
            return allBytes.ToArray();
        }

        public void OnDeserialize(byte[] data)
        {
            foreach (NetworkBehaviour behaviour in NetworkBehaviours)
            {
                behaviour.OnDeserialize(data);
            }
        }

        #endregion
    }
}
