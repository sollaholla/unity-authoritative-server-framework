using AuthoritativeServer.Attributes;

using System;
using System.Collections.Generic;
using System.Reflection;

using UnityEngine;

namespace AuthoritativeServer
{
    public enum RPCType
    {
        /// <summary>
        /// An RPC that will be executed on the server only.
        /// </summary>
        ServerOnly,
        /// <summary>
        /// An RPC that will be executed on all clients.
        /// </summary>
        All,
        /// <summary>
        /// An RPC that will be executed on all clients, and late joiners will receive it.
        /// </summary>
        AllBuffered,
        /// <summary>
        /// An RPC that will be executed on other clients.
        /// </summary>
        Others,
        /// <summary>
        /// An RPC that will be executed on other clients, and late joiners will receive it.
        /// </summary>
        OthersBuffered,
        /// <summary>
        /// An RPC that will be executed on a specific client, first argument of the RPC must be the connection.
        /// </summary>
        Target
    }

    public enum RPCParameter : byte
    {
        Short,
        Long,
        Float,
        Int,
        String,
        Vector3,
        Vector2,
        Quaternion,
        Connection
    }

    [System.Serializable]
    public class RPCMethodInfo
    {
        public string m_TypeName;
        public string m_MethodName;
        public int m_ArgumentCount;

        public RPCMethodInfo(string typeName, string name, int argumentCount)
        {
            m_TypeName = typeName;
            m_MethodName = name;
            m_ArgumentCount = argumentCount;
        }
    }

    /// <summary>
    /// A class that handles remote procedure calls for server and clients.
    /// </summary>
    [System.Serializable]
    public class NetworkRemoteProcedures
    {
        /// <summary>
        /// The message ID used for executing a server only RPC.
        /// </summary>
        public const short RPCMsg = -7;

        #region FIELDS

        [SerializeField]
        private List<RPCMethodInfo> m_Methods;

        private Dictionary<int, HashSet<NetworkWriter>> m_BufferedMessages;

        #endregion

        public NetworkRemoteProcedures()
        {
            NetworkController.InitializeHandlers += OnInitializeHandlers;
            NetworkController.ServerClientConnected += OnClientConnectedToServer;
            NetworkScene.DestroyedGameObject += OnDestroyedObject;
            NetworkScene.CreatedGameObject += OnCreatedObject;
        }

        /// <summary>
        /// Executed in edit mode when scripts are reloaded.
        /// </summary>
        public void InitializeRPCs()
        {
#if UNITY_EDITOR
            if (m_Methods == null)
                m_Methods = new List<RPCMethodInfo>(); 

            m_Methods.Clear();

            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (Assembly assembly in assemblies)
            {
                Type[] types = assembly.GetTypes();

                foreach (Type type in types) 
                {
                    if (!type.IsSubclassOf(typeof(NetworkBehaviour)) || !type.IsPublic || type.IsAbstract)
                        continue;

                    MethodInfo[] methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                    foreach (MethodInfo method in methods)
                    {
                        IEnumerable<Attribute> attributes = method.GetCustomAttributes();

                        foreach (Attribute att in attributes)
                        {
                            if (att is NetworkRPCAttribute rpc)
                            {
                                RPCMethodInfo rpcMethod = new RPCMethodInfo(type.Name, method.Name, method.GetParameters()?.Length ?? 0);
                                m_Methods.Add(rpcMethod);
                                break;
                            }
                        }
                    }
                }
            }
#endif
        }

        #region PROPERTIES

        /// <summary>
        /// The RPC methods.
        /// </summary>
        public List<RPCMethodInfo> Methods { get { return m_Methods; } }

        #endregion

        #region PRIVATE

        private void OnInitializeHandlers()
        {
            NetworkController.Instance.RegisterReceiveHandler(RPCMsg, OnRPC);
        }

        private void OnCreatedObject(NetworkIdentity identity)
        {
            if (NetworkController.Instance.IsServer)
                return;

            if (m_BufferedMessages == null)
                return;

            if (m_BufferedMessages.TryGetValue(identity.InstanceID, out HashSet<NetworkWriter> bufferedRPCs))
            {
                foreach (NetworkWriter bufferedRPC in bufferedRPCs)
                {
                    OnRPC(bufferedRPC);
                }
            }
        }

        private void OnDestroyedObject(NetworkIdentity identity)
        {
            m_BufferedMessages?.Remove(identity.InstanceID);
        }

        private void OnClientConnectedToServer(NetworkConnection conn)
        {
            if (m_BufferedMessages == null)
                return;

            foreach (HashSet<NetworkWriter> bufferedRPCs in m_BufferedMessages.Values)
            {
                foreach (NetworkWriter bufferedRPC in bufferedRPCs)
                {
                    conn.Send(0, RPCMsg, bufferedRPC.ToArray());
                }
            }
        }

        private void OnRPC(NetworkWriter writer)
        {
            ReadRPC(writer, out int connection, out int instanceID, out byte rpcType, out int rpcIndex, out int argumentCount, out object[] arguments);

            NetworkIdentity identity = NetworkIdentityManager.Instance.Get(instanceID);

            if (identity == null)
            {
                if (!NetworkController.Instance.IsServer)
                {
                    InitBuffer(instanceID);
                    m_BufferedMessages[instanceID].Add(new NetworkWriter(writer.ToArray()));
                }
                return;
            }

            if (rpcIndex == -1 || rpcIndex >= m_Methods.Count)
                return;

            if (NetworkController.Instance.IsServer)
            {
                RPCType type = (RPCType)rpcType;

                if (type != RPCType.ServerOnly)
                {
                    if (identity.OwnerConnection == null || identity.OwnerConnection.ConnectionID != connection)
                        return;

                    if (NetworkController.Instance.GetConnection(connection) == null)
                        return;
                }

                switch (type)
                {
                    case RPCType.All:
                        NetworkController.Instance.SendToAll(0, RPCMsg, writer.ToArray());
                        return;
                    case RPCType.AllBuffered:
                        NetworkController.Instance.SendToAll(0, RPCMsg, writer.ToArray());
                        InitBuffer(instanceID);
                        m_BufferedMessages[instanceID].Add(new NetworkWriter(writer.ToArray()));
                        return;
                    case RPCType.Others:
                        NetworkController.Instance.SendToAllExcluding(writer.ToArray(), RPCMsg, connection);
                        return;
                    case RPCType.OthersBuffered:
                        NetworkController.Instance.SendToAllExcluding(writer.ToArray(), RPCMsg, connection);
                        InitBuffer(instanceID);
                        m_BufferedMessages[instanceID].Add(new NetworkWriter(writer.ToArray()));
                        return;
                }
            }

            RPCMethodInfo method = m_Methods[rpcIndex];

            NetworkBehaviour behaviour = (NetworkBehaviour)identity.GetComponent(method.m_TypeName);

            behaviour.InvokeRPC(method.m_MethodName, arguments);
        }

        private void ReadRPC(NetworkWriter writer, out int connectionID, out int instanceID, out byte rpcType, out int rpcIndex, out int argumentCount, out object[] arguments)
        {
            connectionID = writer.ReadInt16();
            instanceID = writer.ReadInt16();
            rpcType = writer.ReadByte();
            rpcIndex = writer.ReadInt16();
            argumentCount = writer.ReadInt16();

            List<object> args = new List<object>();
            for (int i = 0; i < argumentCount; i++)
            {
                RPCParameter param = (RPCParameter)writer.ReadByte();
                switch (param)
                {
                    case RPCParameter.Short:
                        args.Add(writer.ReadInt16());
                        break;
                    case RPCParameter.Long:
                        args.Add(writer.ReadInt64());
                        break;
                    case RPCParameter.Float:
                        args.Add(writer.ReadSingle());
                        break;
                    case RPCParameter.Int:
                        args.Add(writer.ReadInt32());
                        break;
                    case RPCParameter.Vector3:
                        args.Add(writer.ReadVector3());
                        break;
                    case RPCParameter.Vector2:
                        args.Add(writer.ReadVector2());
                        break;
                    case RPCParameter.Quaternion:
                        Vector3 xyz = writer.ReadVector3();
                        float w = writer.ReadSingle();
                        args.Add(new Quaternion(xyz.x, xyz.y, xyz.z, w));
                        break;
                    case RPCParameter.Connection:
                        NetworkConnection connection = new NetworkConnection(writer.ReadInt16());
                        args.Add(connection);
                        break;
                }
            }
            arguments = args.ToArray();
        }

        private NetworkWriter GetRPCWriter(int connID, int instID, byte rpcType, int rpcIndex, int argumentCount, object[] args)
        {
            NetworkWriter writer = new NetworkWriter();
            writer.Write((short)connID);
            writer.Write((short)instID);
            writer.Write(rpcType);
            writer.Write((short)rpcIndex);
            writer.Write((short)argumentCount);

            if (args != null)
            {
                for (int i = 0; i < argumentCount; i++)
                {
                    object arg = args[i];
                    if (arg is short s)
                    {
                        writer.Write((byte)RPCParameter.Short);
                        writer.Write(s);
                    }
                    else if (arg is int n)
                    {
                        writer.Write((byte)RPCParameter.Int);
                        writer.Write(n);
                    }
                    else if (arg is long l)
                    {
                        writer.Write((byte)RPCParameter.Long);
                        writer.Write(l);
                    }
                    else if (arg is float f)
                    {
                        writer.Write((byte)RPCParameter.Float);
                        writer.Write(f);
                    }
                    else if (arg is Vector3 v)
                    {
                        writer.Write((byte)RPCParameter.Vector3);
                        writer.Write(v);
                    }
                    else if (arg is Vector2 v2)
                    {
                        writer.Write((byte)RPCParameter.Vector2);
                        writer.Write(v2);
                    }
                    else if (arg is Quaternion q)
                    {
                        writer.Write((byte)RPCParameter.Quaternion);
                        writer.Write(new Vector3(q.x, q.y, q.z));
                        writer.Write(q.w);
                    }
                    else if (arg is NetworkConnection c)
                    {
                        writer.Write((byte)RPCParameter.Connection);
                        writer.Write((short)c.ConnectionID);
                    }
                }
            }

            return writer;
        }

        private void InitBuffer(int instanceID)
        {
            if (m_BufferedMessages == null)
                m_BufferedMessages = new Dictionary<int, HashSet<NetworkWriter>>();

            if (!m_BufferedMessages.TryGetValue(instanceID, out HashSet<NetworkWriter> writer))
            {
                m_BufferedMessages[instanceID] = new HashSet<NetworkWriter>();
            }
        }

        #endregion

        #region PUBLIC

        /// <summary>
        /// Execute a remote procedure.
        /// </summary>
        /// <param name="netInstanceID">The <see cref="NetworkIdentity.InstanceID"/>.</param>
        /// <param name="function">The name of the function.</param>
        /// <param name="parameters">The function parameters.</param>
        public void Call(NetworkIdentity identity, RPCType type, string function, params object[] parameters)
        {
            int connectionID = NetworkController.Instance.IsServer ? identity.OwnerConnection?.ConnectionID ?? -1 : NetworkController.Instance.ConnectionID;

            if (!NetworkController.Instance.IsServer)
            {
                if (type == RPCType.Target)
                    throw new InvalidOperationException("You cannot send a targetted RPC because you are not the server.");

                if (identity.OwnerConnection == null)
                    throw new InvalidOperationException("The network identity has no owner and cannot execute any RPCs");

                if (identity == null)
                    throw new InvalidOperationException("The network identity specified does not exist on this client: " + identity.InstanceID);

                if (identity.OwnerConnection.ConnectionID != NetworkController.Instance.LocalConnectionID)
                    throw new InvalidOperationException("You are not the owner of this object and cannot execute RPCs on it.");
            }

            int index = m_Methods?.FindIndex(x => x.m_MethodName == function) ?? -1;
            if (index == -1)
                return;

            RPCMethodInfo rpc = m_Methods[index];

            if (rpc.m_ArgumentCount != parameters.Length)
                throw new InvalidOperationException("Given argument count for " + function + " does not match the argument count specified.");

            NetworkWriter writer = GetRPCWriter(NetworkController.Instance.LocalConnectionID, identity.InstanceID, (byte)type, index, rpc.m_ArgumentCount, parameters);

            byte[] data = writer.ToArray();

            switch (type)
            {
                case RPCType.All:
                    {
                        if (NetworkController.Instance.IsServer) NetworkController.Instance.SendToAll(0, RPCMsg, data);
                        else NetworkController.Instance.Send(connectionID, 0, RPCMsg, data);
                        break;
                    }
                case RPCType.Others:
                    {
                        if (NetworkController.Instance.IsServer) NetworkController.Instance.SendToAll(0, RPCMsg, data);
                        else NetworkController.Instance.Send(connectionID, 0, RPCMsg, data);
                        break;
                    }
                case RPCType.ServerOnly:
                    {
                        if (NetworkController.Instance.IsServer)
                        {
                            NetworkBehaviour networkBehaviour = identity.GetComponent<NetworkBehaviour>();
                            networkBehaviour.InvokeRPC(function, parameters);
                        }
                        else NetworkController.Instance.Send(connectionID, 0, RPCMsg, data);
                        break;
                    }
                case RPCType.AllBuffered:
                    {
                        if (NetworkController.Instance.IsServer)
                        {
                            InitBuffer(identity.InstanceID);
                            m_BufferedMessages[identity.InstanceID].Add(new NetworkWriter(writer.ToArray()));
                            NetworkController.Instance.SendToAll(0, RPCMsg, data);
                        }
                        else NetworkController.Instance.Send(connectionID, 0, RPCMsg, data);
                        break;
                    }
                case RPCType.OthersBuffered:
                    {
                        if (NetworkController.Instance.IsServer)
                        {
                            InitBuffer(identity.InstanceID);
                            m_BufferedMessages[identity.InstanceID].Add(new NetworkWriter(writer.ToArray()));
                            NetworkController.Instance.SendToAll(0, RPCMsg, data);
                        }
                        else NetworkController.Instance.Send(connectionID, 0, RPCMsg, data);
                        break;
                    }
                case RPCType.Target:
                    {
                        if (parameters.Length > 0)
                        {
                            NetworkConnection connection = (NetworkConnection)parameters[0];
                            if (connection != null)
                                connection.Send(0, RPCMsg, data);
                        }
                        break;
                    }
            }
        }

        /// <summary>
        /// Clears the runtime RPC buffer.
        /// </summary>
        public void ClearBuffer()
        {
            m_BufferedMessages?.Clear();
        }

        #endregion
    }
}
