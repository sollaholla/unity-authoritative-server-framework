#pragma warning disable CS0618 // Type or member is obsolete

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

namespace AuthoritativeServer
{
    /// <summary>
    /// A LLAPI wrapper that allows you to connect to or create a dedicated server.
    /// </summary>
    [AddComponentMenu("Autho Server/Network Controller")]
    [ExecuteInEditMode]
    [DisallowMultipleComponent]
    public class NetworkController : MonoBehaviour
    {
        /// <summary>
        /// Receive messages are handlers for messages received from the network transport.
        /// </summary>
        /// <param name="writer"></param>
        public delegate void NetworkReceiveDelegate(NetworkWriter writer);

        /// <summary>
        /// The message ID used for callbacks from the server when a remote client connects.
        /// </summary>
        public const short RemoteConnectMsg = -1;

        /// <summary>
        /// The message ID used for callbacks from the server when a remote client disconnects.
        /// </summary>
        public const short RemoteDisconnectMsg = -2;

        /// <summary>
        /// The message ID used for callbacks when a client sends a "received connection" message from the server.
        /// </summary>
        public const short ClientReadyMsg = -3;

        #region EVENTS

        /// <summary>
        /// Invoked on the client when connection to the server has been established.
        /// </summary>
        public static event Action<NetworkConnection> ConnectionEstablished;

        /// <summary>
        /// Invoked on the client when the local client disconnected from the server.
        /// </summary>
        public static event Action ConnectionTerminated;

        /// <summary>
        /// Invoked on the client when the server notifies us of a remote connection.
        /// </summary>
        public static event Action<NetworkConnection> RemoteConnected;

        /// <summary>
        /// Invoked on the client when the server notifies us of a remote disconnection.
        /// </summary>
        public static event Action<NetworkConnection> RemoteDisconnected;

        /// <summary>
        /// Invoked when the online scene is loaded.
        /// </summary>
        public static event Action<Scene> OnlineSceneLoaded;

        /// <summary>
        /// Invoked on the server when the server has been initialized.
        /// </summary>
        public static event Action ServerStarted;

        /// <summary>
        /// Invoked on the server when the server has been stopped.
        /// </summary>
        public static event Action ServerStopped;

        /// <summary>
        /// Invoked on the server when a client connects.
        /// </summary>
        public static event Action<NetworkConnection> ServerClientConnected;

        /// <summary>
        /// Invoked on the server when a client disconnects.
        /// </summary>
        public static event Action<NetworkConnection> ServerClientDisconnected;

        /// <summary>
        /// Invoked when the server or client initializes its message handlers.
        /// </summary>
        public static event Action InitializeHandlers;

        /// <summary>
        /// Invoked when something has been logged.
        /// </summary>
        public static event Action<string, LogLevel> Log;

        #endregion

        #region INSPECTOR

        [SerializeField] private bool m_DrawStats;
        [SerializeField] private NetworkSettings m_Settings;

        #endregion

        #region FIELDS

        public static byte ReliableChannel { get; private set; } = 0;
        public static byte UnReliableChannel { get; private set; } = 0;
        public static byte ReliableSequencedChannel { get; private set; } = 0;
        private int m_HostID = -1;
        private byte[] m_ReceiveBuffer;
        private int m_MessageSize = 1024;
        private int m_LastSendSize;

        private Dictionary<int, NetworkConnection> m_Connections;
        private Dictionary<short, NetworkReceiveDelegate> m_ReceiveHandlers;

        #endregion

        #region PROPERTIES

        /// <summary>
        /// The instance of the network controller.
        /// </summary>
        public static NetworkController Instance { get; private set; }

        /// <summary>
        /// The network settings for this network controller.
        /// </summary>
        public NetworkSettings Settings { get { return m_Settings; } }

        /// <summary>
        /// This is true if we're connected.
        /// </summary>
        public bool IsConnected { get; private set; }

        /// <summary>
        /// This is true if the <see cref="NetworkController"/> has been initialized.
        /// </summary>
        public bool IsStarted { get; private set; }

        /// <summary>
        /// True if this <see cref="NetworkController"/> was initialized as the server.
        /// </summary>
        public bool IsServer { get; private set; }

        /// <summary>
        /// The connection ID of the initializer.
        /// </summary>
        public int ConnectionID { get; private set; } = -1;

        /// <summary>
        /// The local player connection ID as received from the server.
        /// </summary>
        public int LocalConnectionID { get; private set; } = -1;

        /// <summary>
        /// True if the active scene is the online scene.
        /// </summary>
        public bool IsOnlineScene { get { return SceneManager.GetActiveScene().name == m_Settings?.m_OnlineScene.m_SceneName; } }

        /// <summary>
        /// The network scene data.
        /// </summary>
        public NetworkScene Scene { get; private set; }

        /// <summary>
        /// The network statistics.
        /// </summary>
        public NetworkStats NetworkStats { get; private set; }

        /// <summary>
        /// The remote procedure controller which handles all remote procedure calls.
        /// </summary>
        public NetworkRemoteProcedures RemoteProcedures { get { return m_Settings.m_RemoteProcedures; } }

        #endregion

        #region UNITY

        protected virtual void Awake()
        {
            Singleton();
        }

        protected virtual void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        protected virtual void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        protected virtual void Update()
        {
            if (IsOnlineScene)
            {
                if (NetworkStats == null)
                    NetworkStats = new NetworkStats();

                NetworkStats.Update(Time.deltaTime);
            }
        }

        protected virtual void OnGUI()
        {
            if (!IsServer && IsOnlineScene)
            {
                GUILayout.Label("Ping: " + Ping());
            }

            if (!m_DrawStats)
                return;

            if (IsOnlineScene && NetworkStats != null)
            {
                GUILayout.Label("Incoming Packets Per second: " + NetworkStats.IncomingPacketsPerSecond);
                GUILayout.Label("Outgoing packets Per second: " + NetworkStats.OutgoingPacketsPerSecond);
                GUILayout.Label("Outgoing bytes Per second: " + NetworkStats.OutgoingBytesPerSecond);
                GUILayout.Label("Outgoing messages Per second: " + NetworkStats.OutgoingMessagesPerSecond);
                GUILayout.Label("Last send size: " + m_LastSendSize);
            }
        }

        protected virtual void FixedUpdate()
        {
            Receive();
        }

        #endregion

        #region PRIVATE

        private void Singleton()
        {
            if (Instance == null)
            {
                Instance = this;

                if (Application.isBatchMode)
                {
                    Log += BatchmodeLog;

                    SceneManager.sceneLoaded += OnSceneLoaded;

                    DebugLog("Batchmode: Connecting as server...");

                    ConnectAsServer();
                }
            }
            else if (Instance != null)
            {
                Destroy(gameObject);
            }

            if (!Application.isEditor || Application.isBatchMode || Application.isPlaying)
            {
                DontDestroyOnLoad(gameObject);
            }
        }

        private void DebugLog(object value)
        {
            if (m_Settings.m_LogLevel == LogLevel.Debug || m_Settings.m_LogLevel == LogLevel.All)
            {
                Debug.Log(value);
                Log?.Invoke(value.ToString(), LogLevel.Debug);
            }
        }

        private void DebugLogError(object value)
        {
            if (m_Settings.m_LogLevel == LogLevel.Error || m_Settings.m_LogLevel == LogLevel.All)
            {
                Debug.LogError(value);
                Log?.Invoke(value.ToString(), LogLevel.Error);
            }
        }

        private void BatchmodeLog(string value, LogLevel level)
        {
            string lines = string.Empty;

            string path = Application.dataPath + "/ServerFramework.log";

            if (File.Exists(path))
            {
                lines = File.ReadAllText(path) + Environment.NewLine;
            }

            lines += string.Format("[{0}] [{1}] {2}", DateTime.Now.ToString("HH:mm:ss"), level, value);

            File.WriteAllText(path, lines);
        }

        private void Initialize(string serverIP, bool client)
        {
            if (IsStarted)
                return;

            m_ReceiveBuffer = new byte[m_MessageSize];

            NetworkTransport.Init();
            IsStarted = true;

            ConnectionConfig cc = new ConnectionConfig
            {
                AcksType = ConnectionAcksType.Acks128,
                SendDelay = m_Settings.m_SendDelay,
                PacketSize = m_Settings.m_PacketSize,
                FragmentSize = m_Settings.m_FragmentSize
            };

            ReliableChannel = cc.AddChannel(QosType.ReliableFragmented);
            UnReliableChannel = cc.AddChannel(QosType.UnreliableFragmented);
            ReliableSequencedChannel = cc.AddChannel(QosType.ReliableFragmentedSequenced);

            HostTopology topology = new HostTopology(cc, m_Settings.m_MaxPlayers);
            switch (client)
            {
                case true:
                    InitializeClient(serverIP, topology);
                    break;
                case false:
                    InitializeServer(topology);
                    break;
            }
        }

        private void InitializeServer(HostTopology topology)
        {
            if (m_Settings.m_SimulateNetworking)
            {
                m_HostID = NetworkTransport.AddHostWithSimulator(topology, m_Settings.m_MinLatency, m_Settings.m_MaxLatency, m_Settings.m_Port);
            }
            else
            {
                m_HostID = NetworkTransport.AddHost(topology, m_Settings.m_Port);
            }

            IsConnected = true;
            IsServer = true;

            ServerStarted?.Invoke();

            RegisterHandlers();
            LoadOnlineScene();

            DebugLog("Server started...");
        }

        private void InitializeClient(string serverIP, HostTopology topology)
        {
            if (m_Settings.m_SimulateNetworking)
            {
                m_HostID = NetworkTransport.AddHostWithSimulator(topology, m_Settings.m_MinLatency, m_Settings.m_MaxLatency, 0);
            }
            else
            {
                m_HostID = NetworkTransport.AddHost(topology, 0);
            }

            byte error;

            if (m_Settings.m_SimulateNetworking)
            {
                int min = m_Settings.m_MinLatency;

                int avg = (m_Settings.m_MinLatency + m_Settings.m_MaxLatency) / 2;

                ConnectionSimulatorConfig conf = new ConnectionSimulatorConfig(min, avg, min, avg, m_Settings.m_PacketLoss);

                ConnectionID = NetworkTransport.ConnectWithSimulator(m_HostID, serverIP, m_Settings.m_Port, 0, out error, conf);
            }
            else
            {
                ConnectionID = NetworkTransport.Connect(m_HostID, serverIP, m_Settings.m_Port, 0, out error);
            }

            DebugLog(string.Format("Connecting to {0}...", serverIP));

            if ((NetworkError)error != NetworkError.Ok)
            {
                DebugLog("Connection failed: " + (NetworkError)error);
                return;
            }

            RegisterHandlers();
        }

        private void Receive()
        {
            if (!IsStarted)
                return;

            NetworkEventType type;
            do
            {
                type = NetworkTransport.Receive(
                    out int hostID,
                    out int connectionID,
                    out int channelID,
                    m_ReceiveBuffer,
                    m_MessageSize,
                    out int receivedSize,
                    out byte error);

                try
                {
                    switch (type)
                    {
                        case NetworkEventType.DataEvent:
                            OnReceiveData(hostID, connectionID, channelID, m_ReceiveBuffer, receivedSize);
                            break;
                        case NetworkEventType.ConnectEvent:
                            OnClientConnected(connectionID);
                            break;
                        case NetworkEventType.DisconnectEvent:
                            OnClientDisconnected(connectionID);
                            break;
                    }

                    if ((NetworkError)error != NetworkError.Ok)
                    {
                        DebugLogError("Receive Error: " + (NetworkError)error);
                        if (!IsServer) Disconnect();
                        break;
                    }
                }
                catch(Exception e)
                {
                    DebugLogError(e);
                }
            }
            while (type != NetworkEventType.Nothing);
        }

        private void BroadcastConnection(int connectionID)
        {
            if (!IsServer)
                return;

            NetworkConnection targetConnection = m_Connections[connectionID];

            // Send the new connection to all currently connected clients.
            foreach (NetworkConnection remote in m_Connections.Values)
            {
                if (remote == targetConnection)
                    continue;

                NetworkWriter writer = GetRemoteConnectWriter(connectionID, false);
                byte[] data = writer.ToArray();

                remote.Send(ReliableSequencedChannel, RemoteConnectMsg, data);
            }

            // Send all connected clients to the new connection.
            foreach (NetworkConnection remote in m_Connections.Values)
            {
                if (remote == targetConnection)
                    continue;

                NetworkWriter writer = GetRemoteConnectWriter(remote.ConnectionID, false);
                byte[] data = writer.ToArray();

                targetConnection.Send(ReliableSequencedChannel, RemoteConnectMsg, data);
            }
        }

        private NetworkWriter GetRemoteConnectWriter(int remoteConnection, bool isLocalPlayer)
        {
            NetworkWriter writer = new NetworkWriter();
            writer.Write((short)remoteConnection);
            writer.Write(isLocalPlayer);
            return writer;
        }

        private void BroadcastDisconnection(int connectionID)
        {
            if (!IsServer)
                return;

            foreach (NetworkConnection connection in m_Connections.Values)
            {
                connection.Send(ReliableSequencedChannel, RemoteDisconnectMsg, BitConverter.GetBytes((short)connectionID));
            }
        }

        private void RegisterHandlers()
        {
            if (!IsStarted)
                return;

            RegisterReceiveHandler(RemoteConnectMsg, OnRemoteConnected);
            RegisterReceiveHandler(RemoteDisconnectMsg, OnRemoteDisconnected);
            RegisterReceiveHandler(ClientReadyMsg, OnClientReady);
            Scene = new NetworkScene();
            RemoteProcedures.InitRuntime();
            InitializeHandlers?.Invoke();
        }

        private void LoadOnlineScene()
        {
            if (string.IsNullOrEmpty(m_Settings.m_OnlineScene.m_SceneName))
                return;

            if (!IsServer)
            {
                SceneManager.LoadSceneAsync(m_Settings.m_OnlineScene.m_SceneName);
            }
            else
            {
                SceneManager.LoadScene(m_Settings.m_OnlineScene.m_SceneName);

                DebugLog("Loading online scene as server...");

                return;
            }

            DebugLog("Loading online scene...");
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
        {
            if (!IsConnected)
                return;

            if (m_Settings == null)
                return;

            if (scene == m_Settings.m_OnlineScene)
            {
                OnOnlineSceneLoaded(scene);

                DebugLog("Online scene loaded...");
            }
        }

        private void OnOnlineSceneLoaded(Scene scene)
        {
            OnlineSceneLoaded?.Invoke(scene);

            if (!IsServer)
            {
                SendReadyToServer();
            }
        }

        private void SendReadyToServer()
        {
            byte[] data = BitConverter.GetBytes((short)LocalConnectionID);
            Send(ConnectionID, ReliableChannel, ClientReadyMsg, data);
            ConnectionEstablished?.Invoke(m_Connections[LocalConnectionID]);
        }

        #endregion

        #region PROTECTED

        /// <summary>
        /// Called when we receive information over the network.
        /// </summary>
        /// <param name="hostID"></param>
        /// <param name="connectionID"></param>
        /// <param name="channelID"></param>
        /// <param name="receivedBuffer"></param>
        /// <param name="receivedSize"></param>
        protected virtual void OnReceiveData(int hostID, int connectionID, int channelID, byte[] receivedBuffer, int receivedSize)
        {
            NetworkWriter msg = new NetworkWriter(receivedBuffer);
            short messageID = msg.ReadInt16();

            if (m_ReceiveHandlers != null)
            {
                if (m_ReceiveHandlers.TryGetValue(messageID, out NetworkReceiveDelegate receiver))
                {
                    short count = msg.ReadInt16();
                    byte[] data = msg.ReadBytes(count);
                    NetworkWriter message = new NetworkWriter(data);
                    receiver?.Invoke(message);
                }
            }
        }

        /// <summary>
        /// Used by the server to add connections.
        /// </summary>
        /// <param name="connectionID"></param>
        protected virtual void OnClientConnected(int connectionID)
        {
            if (m_Connections == null)
                m_Connections = new Dictionary<int, NetworkConnection>();

            if (IsServer)
            {
                NetworkTransport.GetConnectionInfo(m_HostID, connectionID, out string addr, out _, out _, out _, out byte err);
                if ((NetworkError)err != NetworkError.Ok)
                {
                    DebugLogError(string.Format("Unable to retrieve connection info from client {0}. Error: {1}", connectionID, (NetworkError)err));
                    return;
                }

                DebugLog(string.Format("Client {0} connected from {1}.", connectionID, addr));

                m_Connections[connectionID] = new NetworkConnection(addr, connectionID);
                m_Connections[connectionID].Send(ReliableSequencedChannel, RemoteConnectMsg, GetRemoteConnectWriter(connectionID, true).ToArray());
            }
        }

        /// <summary>
        /// Called on the local client when the server sends our remote connection.
        /// </summary>
        /// <param name="connectionID"></param>
        protected virtual void OnLocalClientConnected(int connectionID)
        {
            if (m_Connections == null)
                m_Connections = new Dictionary<int, NetworkConnection>();

            IsConnected = true;
            DebugLog(string.Format("Successfully connected to server!"));

            m_Connections[connectionID] = new NetworkConnection(connectionID);
            LocalConnectionID = connectionID;

            LoadOnlineScene();
        }

        /// <summary>
        /// Used to disconnect ourselves.
        /// </summary>
        /// <param name="connectionID"></param>
        protected virtual void OnClientDisconnected(int connectionID)
        {
            if (IsServer)
            {
                DebugLog(string.Format("Client {0} disconnected.", connectionID));

                if (m_Connections == null)
                    return;

                try
                {
                    Scene.NotifyClientDisconnect(m_Connections[connectionID]);
                    ServerClientDisconnected?.Invoke(m_Connections[connectionID]);
                }
                catch (Exception e)
                {
                    DebugLogError(e);
                }

                m_Connections.Remove(connectionID);
                BroadcastDisconnection(connectionID);
            }
            else
            {
                DebugLog(string.Format("Disconnected from server..."));
                Disconnect();
            }
        }

        /// <summary>
        /// Called on the server when the client becomes ready.
        /// </summary>
        /// <param name="writer"></param>
        protected virtual void OnClientReady(NetworkWriter writer)
        {
            int connectionID = writer.ReadInt16();
            Scene.NotifyClientConnect(m_Connections[connectionID]);
            ServerClientConnected?.Invoke(m_Connections[connectionID]);
            BroadcastConnection(connectionID);
        }

        /// <summary>
        /// Called on the client when a remote connects (could also be our own connection)
        /// </summary>
        /// <param name="writer"></param>
        protected virtual void OnRemoteConnected(NetworkWriter writer)
        {
            if (m_Connections == null)
                m_Connections = new Dictionary<int, NetworkConnection>();

            int connectionID = writer.ReadInt16();
            bool isLocalPlayer = writer.ReadBool();

            DebugLog(string.Format("Remote {0} connected to the server. Is Mine: {1}", connectionID, isLocalPlayer));

            if (isLocalPlayer)
            {
                OnLocalClientConnected(connectionID);
                return;
            }

            m_Connections[connectionID] = new NetworkConnection(connectionID);

            RemoteConnected?.Invoke(m_Connections[connectionID]);
        }

        /// <summary>
        /// Called on the client when a remote disconnects.
        /// </summary>
        /// <param name="writer"></param>
        protected virtual void OnRemoteDisconnected(NetworkWriter writer)
        {
            if (m_Connections == null)
                return;

            int connectionID = writer.ReadInt16();
            RemoteDisconnected?.Invoke(m_Connections[connectionID]);
            DebugLog(string.Format("Remote {0} disconnected from the server.", connectionID));
            m_Connections.Remove(connectionID);
        }

        #endregion

        #region PUBLIC

        /// <summary>
        /// Gets a connection object with the specified connection ID.
        /// </summary>
        /// <param name="connectionID"></param>
        /// <returns></returns>
        public NetworkConnection GetConnection(int connectionID)
        {
            if (m_Connections.TryGetValue(connectionID, out NetworkConnection val))
            {
                return val;
            }

            return null;
        }

        /// <summary>
        /// Register a network message handler to handle receiving of data over the network transport.
        /// </summary>
        /// <param name="handlerID">The unique message handler ID.</param>
        /// <param name="handler">The handler delegate.</param>
        public void RegisterReceiveHandler(short handlerID, NetworkReceiveDelegate handler)
        {
            if (m_ReceiveHandlers == null)
                m_ReceiveHandlers = new Dictionary<short, NetworkReceiveDelegate>();

            if (m_ReceiveHandlers.ContainsKey(handlerID))
                return;

            m_ReceiveHandlers[handlerID] = handler;
        }

        /// <summary>
        /// Send a message over the network.
        /// </summary>
        /// <param name="channelID">The send channel.</param>
        /// <param name="message">The message to send.</param>
        public virtual void Send(int connectionID, byte channelID, short messageID, byte[] data)
        {
            if (!IsStarted)
                return;

            NetworkWriter nm = new NetworkWriter();
            nm.Write(messageID);
            nm.WriteBytesAndSize(data);

            byte[] allData = nm.ToArray();
            byte err = (byte)NetworkError.Ok;

            NetworkTransport.Send(m_HostID, connectionID, channelID, allData, allData.Length, out err);

            m_LastSendSize = allData.Length;

            if ((NetworkError)err != NetworkError.Ok)
            {
                DebugLogError("Send error: " + (NetworkError)err);
            }
        }

        /// <summary>
        /// Send a message to all clients from the server.
        /// </summary>
        /// <param name="data"></param>
        public void SendToAll(byte channelID, short messageID, byte[] data)
        {
            if (!IsServer)
                return;

            if (m_Connections == null)
                return;

            foreach (NetworkConnection connection in m_Connections.Values)
            {
                connection.Send(channelID, messageID, data);
            }
        }

        /// <summary>
        /// Sends a message to all connections excluding the ignored connection.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="channel"></param>
        /// <param name="messageID"></param>
        /// <param name="connectionToIgnore"></param>
        public void SendToAllExcluding(byte[] data, byte channel, short messageID, int connectionToIgnore)
        {
            if (!IsServer)
                return;

            foreach (NetworkConnection connection in m_Connections.Values)
            {
                if (connection.ConnectionID == connectionToIgnore)
                    continue;

                connection.Send(channel, messageID, data);
            }
        }

        /// <summary>
        /// Connect as a server.
        /// </summary>
        public virtual void ConnectAsServer()
        {
            Initialize(null, false);
        }

        /// <summary>
        /// Connect as a client to the server using the server IP.
        /// </summary>
        /// <param name="serverIP">The server IP.</param>
        public virtual void ConnectAsClient(string serverIP)
        {
            Initialize(serverIP, true);
        }

        /// <summary>
        /// Disconnect from the network.
        /// </summary>
        public virtual void Disconnect()
        {
            if (!IsStarted)
                return;

            if (IsServer) ServerStopped?.Invoke();
            else ConnectionTerminated?.Invoke();

            m_LastSendSize = 0;
            m_ReceiveBuffer = null;
            IsConnected = false;
            IsServer = false;
            IsStarted = false;
            m_HostID = -1;
            ConnectionID = -1;
            LocalConnectionID = -1;
            ReliableChannel = 0;
            UnReliableChannel = 0;
            ReliableSequencedChannel = 0;
            m_ReceiveHandlers?.Clear();
            m_ReceiveHandlers = null;
            m_Connections?.Clear();
            m_Connections = null;
            Scene?.Clear();
            Scene = null;
            RemoteProcedures?.Clear();

            ConnectionEstablished = null;
            ConnectionTerminated = null;
            ServerClientConnected = null;
            ServerClientDisconnected = null;
            RemoteConnected = null;
            RemoteDisconnected = null;
            OnlineSceneLoaded = null;
            ServerStarted = null;
            ServerStopped = null;
            InitializeHandlers = null;
            Log = null;

            NetworkTransport.Shutdown();
            SceneManager.LoadScene(m_Settings.m_OfflineScene.m_SceneName);
        }

        /// <summary>
        /// Get the RTT to the server.
        /// </summary>
        /// <returns></returns>
        public int Ping()
        {
            if (!IsConnected)
                return 0;

            if (IsServer)
                return 0;

            int rtt = NetworkTransport.GetCurrentRTT(m_HostID, ConnectionID, out byte err);
            if ((NetworkError)err != NetworkError.Ok)
            {
                DebugLogError("Get RTT Error: " + (NetworkError)err);
                return 0;
            }
            return rtt;
        }

        #endregion
    }
}

#pragma warning restore CS0618 // Type or member is obsolete