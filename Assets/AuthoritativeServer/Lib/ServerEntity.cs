using System;
using System.Collections.Generic;
using UnityEngine;

using static AuthoritativeServer.NetworkMessageHandlers;

namespace AuthoritativeServer.Entities
{
    [System.Serializable]
    public class EntityState
    {
        public EntityState(Vector3 position, Quaternion rotation)
        {
            Position = position;
            Rotation = rotation;
        }

        public Vector3 Position { get; }

        public Quaternion Rotation { get; }
    }

    /// <summary>
    /// An entity that's controlled and synchronized on the server.
    /// </summary>
    public class ServerEntity : NetworkBehaviour
    {
        [SerializeField]
        private bool m_SynchronizePosition = true;
        [SerializeField]
        private bool m_SynchronizeRotation = true;
        [SerializeField]
        private bool m_SynchronizeAnimation = true;

        private Queue<EntityState> m_EntityStateQueue;

        private bool m_IsInitialized;
        private Vector3 m_LastPosition;
        private Quaternion m_LastRotation;

        /// <summary>
        /// The entities animator component.
        /// </summary>
        public Animator animator { get; private set; }

        /// <summary>
        /// The entities rigidbody component.
        /// </summary>
        public Rigidbody rigidBody { get; private set; }

        protected virtual void Awake()
        {
            animator = GetComponent<Animator>();
            rigidBody = GetComponent<Rigidbody>();
        }

        protected virtual void FixedUpdate()
        {
            if (!IsServer)
            {
                if (m_EntityStateQueue == null)
                    return;

                while (m_EntityStateQueue.Count > 0)
                {
                    EntityState state = m_EntityStateQueue.Dequeue();
                    transform.position = state.Position;
                    transform.rotation = state.Rotation;
                }
            }
            else if (m_IsInitialized)
            {
                bool hasChanged = false;

                if (m_SynchronizePosition)
                {
                    if (transform.position != m_LastPosition)
                    {
                        hasChanged = true;
                        m_LastPosition = transform.position;
                    }
                }

                if (m_SynchronizeRotation)
                {
                    if (transform.rotation != m_LastRotation)
                    {
                        hasChanged = true;
                        m_LastRotation = transform.rotation;
                    }
                }

                if (hasChanged)
                {
                    NetworkWriter writer = new NetworkWriter();
                    writer.Write((short)InstanceID);

                    if (m_SynchronizePosition)
                        writer.Write(transform.position);

                    if (m_SynchronizeRotation)
                    {
                        Quaternion rotation = transform.rotation;
                        writer.Write(new Vector3(rotation.x, rotation.y, rotation.z));
                        writer.Write(rotation.w);
                    }

                    NetworkController.Instance.SendToAll(NetworkController.UnReliableChannel, NetworkEntityState, writer.ToArray());
                }
            }
        }

        public override void OnServerInitialize()
        {
            m_IsInitialized = true;
        }

        public override void OnClientInitialize()
        {
            NetworkController.Instance.RegisterReceiveHandler(NetworkEntityState, OnReceiveEntityState);

            if (rigidBody != null)
            {
                if (m_SynchronizePosition)
                    rigidBody.isKinematic = true;

                if (m_SynchronizeRotation)
                    rigidBody.freezeRotation = true;
            }
        }

        private void ReceiveState(NetworkWriter writer)
        {
            if (m_EntityStateQueue == null)
                m_EntityStateQueue = new Queue<EntityState>();

            Vector3 pos = Vector3.zero;
            Quaternion rot = Quaternion.identity;

            if (m_SynchronizePosition)
                pos = writer.ReadVector3();

            if (m_SynchronizeRotation)
            {
                Vector3 xyz = writer.ReadVector3();
                float w = writer.ReadSingle();
                rot = new Quaternion(xyz.x, xyz.y, xyz.z, w);
            }

            m_EntityStateQueue.Enqueue(new EntityState(pos, rot));
        }

        private static void OnReceiveEntityState(NetworkWriter writer)
        {
            int instanceID = writer.ReadInt16();
            NetworkIdentity identity = NetworkIdentityManager.Instance.Get(instanceID);
            if (identity != null)
            {
                ServerEntity ent = identity.GetComponent<ServerEntity>();
                if (ent != null)
                {
                    ent.ReceiveState(writer);
                }
            }
        }
    }
}
