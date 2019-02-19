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

        [UnityEditor.Callbacks.DidReloadScripts]
        private static void OnScriptsReload()
        {
            InitializeRPCs();
        }

        private static void InitializeRPCs()
        {
            string[] guids = AssetDatabase.FindAssets("t:" + nameof(NetworkSettings), null);

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);

                NetworkSettings settings = AssetDatabase.LoadAssetAtPath<NetworkSettings>(path);

                settings.m_RPCManager.InitializeRPCs();
            }
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
