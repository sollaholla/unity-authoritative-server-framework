using UnityEngine;

namespace AuthoritativeServer
{
    /// <summary>
    /// Represents a connection to a client on the network.
    /// </summary>
    public class NetworkConnection
    {
        public NetworkConnection(int connectionID)
        {
            ConnectionID = connectionID;
            IsReady = true;
        }

        public NetworkConnection(string ipv4Address, int connectionID) : this(connectionID)
        {
            IPv4Address = ipv4Address;
        }

        /// <summary>
        /// The connection ID of the client.
        /// </summary>
        public int ConnectionID { get; }

        /// <summary>
        /// True if this connected client is ready.
        /// </summary>
        public bool IsReady { get; }

        /// <summary>
        /// The IPv4 address of the client. Only set on the server.
        /// </summary>
        public string IPv4Address { get; }

        /// <summary>
        /// The object that represents this connection, likely the player object.
        /// </summary>
        public GameObject ConnectionObject { get; private set; }

        /// <summary>
        /// Send a message to this connection.
        /// </summary>
        public virtual void Send(byte channelID, short messageID, byte[] data)
        {
            if (!NetworkController.Instance.IsServer)
                return;

            NetworkController.Instance.Send(ConnectionID, channelID, messageID, data);
        }

        /// <summary>
        /// Set the gameObject that represents this connection.
        /// </summary>
        /// <param name="connectionObject">The connection object.</param>
        public void SetConnectionObject(GameObject connectionObject)
        {
            ConnectionObject = connectionObject;
        }
    }
}