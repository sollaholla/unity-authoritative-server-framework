using System;
using System.Collections.Generic;
using System.IO;

using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace AuthoritativeServer.Demo.Editors
{
    public class StandardListAssetManager<T> where T : UnityEngine.ScriptableObject
    {
        private bool m_Dropdown = true;
        private ReorderableList m_List;
        private SerializedProperty m_Property;
        private Dictionary<T, Editor> m_Editors;
        private string m_LastAssetPath;
        private string m_LastAssetName;
        private int m_LastAssetSelected = -1;

        private SerializedObject m_SerializedTarget;
        private string m_HeaderText;
        private string m_NewItemPath;
        private string m_NewItemFileName;
        private string m_NamePropertyPath;

        public StandardListAssetManager(SerializedObject serializedTargetReference, string headerText, string newItemPath, string newItemFileName, string listPropertyPath, string namePropertyPath)
        {
            m_SerializedTarget = serializedTargetReference;
            m_HeaderText = headerText;
            m_NewItemPath = newItemPath;
            m_NewItemFileName = newItemFileName;
            m_NamePropertyPath = namePropertyPath;

            m_List = new ReorderableList(serializedTargetReference, m_Property = serializedTargetReference.FindProperty(listPropertyPath))
            {
                drawElementCallback = OnDrawElement,
                drawHeaderCallback = OnDrawHeader,
                onAddCallback = OnAddCallback,
                onRemoveCallback = OnRemoveCallback
            };
        }

        public void Dispose()
        {
            ApplyLastEdited();
        }

        public void Draw()
        {
            DrawList();
        }

        private void OnDrawHeader(Rect rect)
        {
            EditorGUI.LabelField(rect, m_HeaderText + " List");
        }

        private void OnDrawElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            SerializedProperty element = m_Property.GetArrayElementAtIndex(index);

            EditorGUI.LabelField(rect, element.objectReferenceValue.name);

            if (isActive)
            {
                if (m_LastAssetSelected != index)
                {
                    ApplyLastEdited();
                }

                m_LastAssetSelected = index;

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                using (var check = new EditorGUI.ChangeCheckScope())
                {
                    m_LastAssetPath = AssetDatabase.GetAssetPath(element.objectReferenceValue);

                    T item = AssetDatabase.LoadAssetAtPath<T>(m_LastAssetPath);

                    if (m_Editors == null)
                    {
                        m_Editors = new Dictionary<T, Editor>();
                    }

                    if (!m_Editors.ContainsKey(item))
                    {
                        m_Editors[item] = null;
                    }

                    Editor editor = m_Editors[item];

                    Editor.CreateCachedEditor(item, null, ref editor);

                    m_Editors[item] = editor;

                    editor.OnInspectorGUI();

                    if (check.changed)
                    {
                        m_LastAssetName = new SerializedObject(item).FindProperty(m_NamePropertyPath).stringValue;

                        element.serializedObject.ApplyModifiedProperties();
                    }
                }

                EditorGUILayout.EndVertical();
            }
        }

        private void DrawList()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            if (GUILayout.Button(m_HeaderText, EditorStyles.toolbarDropDown))
            {
                m_Dropdown = !m_Dropdown;
            }

            if (m_Dropdown)
            {
                m_List.DoLayoutList();
            }

            EditorGUILayout.EndVertical();
        }

        private void OnAddCallback(ReorderableList list)
        {
            m_Property.InsertArrayElementAtIndex(list.count);

            SerializedProperty element = m_Property.GetArrayElementAtIndex(list.count - 1);

            T item = ScriptableObject.CreateInstance<T>();

            string path = AssetDatabase.GetAssetPath(m_SerializedTarget.targetObject);
            path = Path.GetDirectoryName(path) + "/" + m_NewItemPath;

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            path = AssetDatabase.GenerateUniqueAssetPath(path + "/" + m_NewItemFileName + ".asset");
            AssetDatabase.CreateAsset(item, path);

            element.objectReferenceValue = AssetDatabase.LoadAssetAtPath<T>(path);
        }

        private void OnRemoveCallback(ReorderableList list)
        {
            SerializedProperty element = m_Property.GetArrayElementAtIndex(list.index);

            string path = AssetDatabase.GetAssetPath(element.objectReferenceValue);

            element.objectReferenceValue = null;

            m_Property.DeleteArrayElementAtIndex(list.index);

            AssetDatabase.DeleteAsset(path);
        }

        private void ApplyLastEdited()
        {
            if (!string.IsNullOrEmpty(m_LastAssetName) && !string.IsNullOrEmpty(m_LastAssetPath))
            {
                AssetDatabase.RenameAsset(m_LastAssetPath, m_LastAssetName);
            }

            m_LastAssetName = string.Empty;
            m_LastAssetPath = string.Empty;
        }
    }
}
