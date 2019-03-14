using UnityEngine;

namespace AuthoritativeServer
{
    /// <summary>
    /// Identified by the network scene to spawn players at this point.
    /// </summary>
    [AddComponentMenu("Autho Server/Spawn Point")]
    [DisallowMultipleComponent]
    public class NetworkSpawnPoint : MonoBehaviour
    {
        private void OnDrawGizmos()
        {
            Gizmos.DrawSphere(transform.position, 1f);
        }
    }
}
