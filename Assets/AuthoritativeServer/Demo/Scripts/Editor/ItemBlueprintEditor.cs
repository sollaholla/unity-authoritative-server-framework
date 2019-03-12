using System;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using static UnityEditor.EditorGUI;

namespace AuthoritativeServer.Demo.Editors
{
    [CustomEditor(typeof(ItemBlueprint))]
    public class ItemBlueprintEditor : Editor
    {
        private SerializedProperty m_NameProperty;
        private SerializedProperty m_InputsProperty;

        private void OnEnable()
        {
        }

        public override void OnInspectorGUI()
        {
            m_NameProperty = serializedObject.FindProperty("m_BlueprintName");
            m_InputsProperty = serializedObject.FindProperty("m_Inputs");

            float lWidth;

            using (var check = new ChangeCheckScope())
            {
                for (int i = 0; i < m_InputsProperty.arraySize; i++)
                {
                    SerializedProperty element = m_InputsProperty.GetArrayElementAtIndex(i);
                    SerializedProperty objProperty = element.FindPropertyRelative("m_Item");
                    SerializedProperty amountProperty = element.FindPropertyRelative("m_RequiredAmount");

                    EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

                    amountProperty.intValue = EditorGUILayout.IntField(amountProperty.intValue);

                    lWidth = EditorGUIUtility.labelWidth;
                    EditorGUIUtility.labelWidth = 1;
                    EditorGUILayout.LabelField("of");
                    EditorGUIUtility.labelWidth = lWidth;

                    objProperty.objectReferenceValue = EditorGUILayout.ObjectField(objProperty.objectReferenceValue, typeof(InventoryItem), false);

                    if (GUILayout.Button(EditorGUIUtility.IconContent("d_TreeEditor.Trash"), GUIStyle.none))
                    {
                        m_InputsProperty.DeleteArrayElementAtIndex(i);
                        break;
                    }

                    EditorGUILayout.EndVertical();

                    if (i != m_InputsProperty.arraySize - 1)
                    {
                        EditorGUILayout.LabelField("+");
                    }
                }

                if (GUILayout.Button("Add", EditorStyles.miniButton))
                {
                    m_InputsProperty.InsertArrayElementAtIndex(m_InputsProperty.arraySize);
                }

                EditorGUILayout.LabelField("=");

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                lWidth = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = 70;
                SerializedProperty outputProp = serializedObject.FindProperty("m_Output");
                outputProp.objectReferenceValue = EditorGUILayout.ObjectField("1 of", outputProp.objectReferenceValue, typeof(InventoryItem), false);
                EditorGUIUtility.labelWidth = lWidth;

                EditorGUILayout.EndVertical();

                EditorGUILayout.Space();

                if (check.changed)
                {
                    serializedObject.ApplyModifiedProperties();
                }
            }
        }
    }
}
