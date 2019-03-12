using UnityEditor;
using UnityEngine;
using UnityEditorInternal;
using static UnityEditor.EditorGUI;
using System;

namespace AuthoritativeServer.Editors
{
    [CustomEditor(typeof(NetworkSettings))]
    public class NetworkSettingsEditor : UnityEditor.Editor
    {
        private NetworkSettings m_Settings;
        private SerializedProperty m_PlayerObjectProperty;

        private bool m_IsPlayerSelected;

        private void OnEnable()
        {
            m_Settings = (NetworkSettings)target;

            m_PlayerObjectProperty = serializedObject.FindProperty(nameof(NetworkSettings.m_PlayerObject));
        }

        private void OnDrawHeader(Rect rect)
        {
            LabelField(rect, "Registered Objects");
        }

        public override void OnInspectorGUI()
        {
            using (ChangeCheckScope check = new ChangeCheckScope())
            {
                DrawPropertiesExcluding(serializedObject, "m_Script", nameof(NetworkSettings.m_PlayerObject));

                EditorGUILayout.Space();

                EditorGUILayout.LabelField("Objects", EditorStyles.boldLabel);

                UpdatePlayerObject();

                if (check.changed)
                { 
                    serializedObject.ApplyModifiedProperties();

                    EditorUtility.SetDirty(target);
                }
            }
        }

        private void UpdatePlayerObject()
        {
            EditorGUILayout.ObjectField(m_PlayerObjectProperty);
        }
    }
}
