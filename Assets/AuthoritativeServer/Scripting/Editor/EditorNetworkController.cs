using UnityEditor;
using UnityEngine;

namespace AuthoritativeServer.Editor
{
    [InitializeOnLoad]
    public static class EditorNetworkController
    {
        static EditorNetworkController()
        {
            EditorApplication.update += EditorUpdate;
        }

        private static void EditorUpdate()
        {
            UpdateNetworkIdentityManagement();
        }

        private static void UpdateNetworkIdentityManagement()
        {
            NetworkIdentity identity = Object.FindObjectOfType<NetworkIdentity>();
            if (identity == null)
            {
                return;
            }

            NetworkIdentityManager identityManager = Object.FindObjectOfType<NetworkIdentityManager>();
            if (identityManager != null)
            {
                return;
            }

            SpawnNetworkIdentityManager();
        }

        private static void SpawnNetworkIdentityManager()
        {
            GameObject networkIdentityObj = new GameObject("Network Identity Manager");
            networkIdentityObj.AddComponent<NetworkIdentityManager>();
            networkIdentityObj.hideFlags = HideFlags.HideInHierarchy;
        }
    }
}
