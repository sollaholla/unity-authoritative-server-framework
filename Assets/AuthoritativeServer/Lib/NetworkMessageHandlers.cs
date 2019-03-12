namespace AuthoritativeServer
{
    public static class NetworkMessageHandlers
    {
        /// <summary>
        /// The message ID used for network instantiation.
        /// </summary>
        public const short InstantiateMsg = -4;

        /// <summary>
        /// The message ID used for network destroys.
        /// </summary>
        public const short DestroyMsg = -5;

        /// <summary>
        /// The message ID used for player creation.
        /// </summary>
        public const short CreatePlayerMsg = -6;

        /// <summary>
        /// The message ID used for executing a server only RPC.
        /// </summary>
        public const short RPCMsg = -7;

        /// <summary>
        /// The message ID used for an input message sent from the server.
        /// </summary>
        public const short ServerSendInputMsg = -8;

        /// <summary>
        /// The message ID used for an input message sent from the client.
        /// </summary>
        public const short ClientSendInputMsg = -9;

        /// <summary>
        /// The message ID used for network entity synchronization.
        /// </summary>
        public const short NetworkEntityState = -10;
    }
}
