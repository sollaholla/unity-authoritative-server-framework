using UnityEditor;

namespace AuthoritativeServer.Editors
{
    [CustomEditor(typeof(NetworkController))]
    public class NetworkControllerEditor : UnityEditor.Editor
    {
        private NetworkController m_NetworkController;
        private UnityEditor.Editor m_NetworkSettingsEditor;

        private void OnEnable()
        {
            m_NetworkController = (NetworkController)target;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (m_NetworkController.Settings != null)
            {
                EditorGUILayout.BeginVertical("box");

                EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

                EditorGUILayout.LabelField("Network Settings");

                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space();

                EditorGUI.indentLevel++;

                CreateCachedEditor(m_NetworkController.Settings, null, ref m_NetworkSettingsEditor);

                if (m_NetworkSettingsEditor == null)
                {
                    EditorGUILayout.HelpBox("Failed to create editor...", MessageType.Error);
                }
                else
                {
                    m_NetworkSettingsEditor.OnInspectorGUI();
                }

                EditorGUI.indentLevel--;

                EditorGUILayout.Space();

                EditorGUILayout.EndVertical();
            }
            else
            {
                EditorGUILayout.HelpBox("Please assign network settings.", MessageType.Info);
            }
        }
    }
}