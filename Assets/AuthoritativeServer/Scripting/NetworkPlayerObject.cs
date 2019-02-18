using UnityEngine;

namespace AuthoritativeServer
{
    public class NetworkPlayerObject
    {
        public NetworkPlayerObject(int connectionID, GameObject gameObject)
        {
            ConnectionID = connectionID;
            GameObject = gameObject;
            NetworkIdentity = GameObject.GetComponent<NetworkIdentity>();
        }

        /// <summary>
        /// The connection ID of the player.
        /// </summary>
        public int ConnectionID { get; }

        /// <summary>
        /// The player object.
        /// </summary>
        public GameObject GameObject { get; }

        /// <summary>
        /// The network identity attached to this gameObject.
        /// </summary>
        public NetworkIdentity NetworkIdentity { get; }
    }
}
