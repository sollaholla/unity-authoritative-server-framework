using AuthoritativeServer.Attributes;
using RPC = AuthoritativeServer.Attributes;

using System;

using UnityEngine;
using Random = UnityEngine.Random;

namespace AuthoritativeServer
{
    public class NetworkRPCTest : NetworkBehaviour
    {
        public GameObject m_Prefab;

        private void Awake()
        {
            RegisterRPC(nameof(SpawnCube), (args) => SpawnCube((Vector3)args[0], (float)args[1]));

            RegisterRPC(nameof(ServerHello), (args) => ServerHello());
        }

        private void Update()
        {
            if (IsOwner)
            {
                if (Input.GetKeyDown(KeyCode.P))
                {
                    Vector3 pos = new Vector3(Random.Range(-5, 5), Random.Range(-5, 5), Random.Range(-5, 5));

                    NetworkController.Instance.RemoteProcedures.Call(Identity, RPCType.ServerOnly, nameof(SpawnCube), pos, 90f);
                }
            }
        }

        [RPC]
        private void SpawnCube(Vector3 position, float rotation)
        {
            NetworkController.Instance.Scene.Create(m_Prefab, position, Quaternion.Euler(0, rotation, 0));

            NetworkController.Instance.RemoteProcedures.Call(Identity, RPCType.All, nameof(ServerHello));
        }

        [RPC]
        private void ServerHello()
        {
            Debug.LogError("Hello world!");
        }
    }
}
