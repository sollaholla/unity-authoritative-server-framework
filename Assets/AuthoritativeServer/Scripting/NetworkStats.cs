using UnityEngine.Networking;

namespace AuthoritativeServer
{
    public class NetworkStats
    {
        private int m_LastOutgoingPackets;
        private int m_LastOutgoingBytes;
        private int m_LastIncomingPackets;
        private int m_LastOutgoingMessages;

        private float m_Timer;

        public float OutgoingPacketsPerSecond { get; private set; }
        public float OutgoingBytesPerSecond { get; private set; }
        public float OutgoingMessagesPerSecond { get; private set; }
        public float IncomingPacketsPerSecond { get; private set; }

        public void Update(float deltaTime)
        {
            m_Timer += deltaTime;

            if (m_Timer >= 1)
            {
                int ogBytes = NetworkTransport.GetOutgoingFullBytesCount();
                int ogBytesDelta = ogBytes - m_LastOutgoingBytes;
                OutgoingBytesPerSecond = ogBytesDelta;
                m_LastOutgoingBytes = ogBytes;

                int ogPackets = NetworkTransport.GetOutgoingPacketCount();
                int ogPacketsDelta = ogPackets - m_LastOutgoingPackets;
                OutgoingPacketsPerSecond = ogPacketsDelta;
                m_LastOutgoingPackets = ogPackets;

                int icPackets = NetworkTransport.GetIncomingPacketCountForAllHosts();
                int icPacketsDelta = icPackets - m_LastIncomingPackets;
                IncomingPacketsPerSecond = icPacketsDelta;
                m_LastIncomingPackets = icPackets;

                int ogMessages = NetworkTransport.GetOutgoingMessageCount();
                int ogMessagesDelta = ogMessages - m_LastOutgoingMessages;
                OutgoingMessagesPerSecond = ogMessagesDelta;
                m_LastOutgoingMessages = ogMessages;

                m_Timer = 0;
            }
        }
    }
}
