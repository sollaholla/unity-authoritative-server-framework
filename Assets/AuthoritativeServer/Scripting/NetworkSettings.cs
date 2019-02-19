using System.Collections.Generic;

using UnityEngine;

namespace AuthoritativeServer
{
    public enum LogLevel
    {
        Debug,
        Warning,
        Error,
        All
    }

    [CreateAssetMenu(menuName = "Autho Server/Create Network Settings")]
    public class NetworkSettings : ScriptableObject
    {
        [Header("Networking")]
        public LogLevel m_LogLevel = LogLevel.All;
        public int m_MaxPlayers = 100;
        public int m_Port = 5500;
        public uint m_SendDelay = 10;
        public ushort m_PacketSize = 1440;
        public ushort m_FragmentSize = 900;

        [Header("Scenes")]
        public SceneInfo m_OfflineScene;
        public SceneInfo m_OnlineScene;

        [Header("Objects")]
        public GameObject m_PlayerObject;
        public List<GameObject> m_RegisteredObjects;

        public NetworkRemoteProcedures m_RPCManager;
    }
}
