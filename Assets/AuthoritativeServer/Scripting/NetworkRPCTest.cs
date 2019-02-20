using AuthoritativeServer.Attributes;

using UnityEngine;
using Random = UnityEngine.Random;

namespace AuthoritativeServer
{
    public class NetworkRPCTest : NetworkBehaviour
    {
        public GameObject m_Prefab;

        private void Awake()
        {
            RegisterRPC(nameof(SpawnPrefab), (args) => SpawnPrefab((Vector3)args[0], (float)args[1]));
            RegisterRPC(nameof(BufferedServerHello), (args) => BufferedServerHello());
            RegisterRPC(nameof(SupGuys), (args) => SupGuys((int)args[0]));
            RegisterRPC(nameof(ServerSays), (args) => ServerSays((NetworkConnection)args[0]));
        }

        private void Update()
        {
            if (IsOwner)
            {
                if (Input.GetKeyDown(KeyCode.P))
                {
                    Vector3 pos = new Vector3(Random.Range(-5, 5), Random.Range(-5, 5), Random.Range(-5, 5));

                    NetworkController.Instance.RemoteProcedures.Call(Identity, RPCType.ServerOnly, nameof(SpawnPrefab), pos, 90f);

                    NetworkController.Instance.RemoteProcedures.Call(Identity, RPCType.OthersBuffered, nameof(SupGuys), Identity.OwnerConnection.ConnectionID);
                }
            }
        }

        [NetworkRPC]
        private void SpawnPrefab(Vector3 position, float rotation)
        {
            NetworkController.Instance.Scene.Create(m_Prefab, position, Quaternion.Euler(0, rotation, 0));

            NetworkController.Instance.RemoteProcedures.Call(Identity, RPCType.AllBuffered, nameof(BufferedServerHello));

            NetworkController.Instance.RemoteProcedures.Call(Identity, RPCType.Target, nameof(ServerSays), Identity.OwnerConnection);
        }

        [NetworkRPC]
        private void BufferedServerHello()
        {
            Debug.LogError("Hello world!");
        }

        [NetworkRPC]
        private void SupGuys(int connection)
        {
            Debug.LogError(connection + " said what's up to us.");
        }

        [NetworkRPC]
        private void ServerSays(NetworkConnection connection)
        {
            Debug.LogError("Server says he got our message!");
        }
    }
}
