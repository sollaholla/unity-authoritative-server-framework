using UnityEditor;
using UnityEngine;
using UnityEditorInternal;
using static UnityEditor.EditorGUI;
using System;

namespace AuthoritativeServer.Editor
{
    [CustomEditor(typeof(NetworkSettings))]
    public class NetworkSettingsEditor : UnityEditor.Editor
    {
        private NetworkSettings m_Settings;
        private ReorderableList m_RegisteredObjectsList;
        private SerializedProperty m_RegisteredObjectsProperty;
        private SerializedProperty m_PlayerObjectProperty;

        private bool m_IsPlayerSelected;

        private void OnEnable()
        {
            m_Settings = (NetworkSettings)target;

            m_RegisteredObjectsProperty = serializedObject.FindProperty(nameof(NetworkSettings.m_RegisteredObjects));

            m_PlayerObjectProperty = serializedObject.FindProperty(nameof(NetworkSettings.m_PlayerObject));

            m_RegisteredObjectsList = new ReorderableList(serializedObject, m_RegisteredObjectsProperty)
            {
                drawElementCallback = OnDrawElement,
                elementHeightCallback = OnElementHeight,
                onCanRemoveCallback = OnCanRemove,
                onAddCallback = OnAddElement,
                drawElementBackgroundCallback = OnDrawBackground,
                drawHeaderCallback = OnDrawHeader,
                onSelectCallback = OnElementSelected,
                onChangedCallback = OnElementSelected,
                draggable = false
            };
        }

        private void OnDrawHeader(Rect rect)
        {
            LabelField(rect, "Registered Objects");
        }

        private void OnElementSelected(ReorderableList list)
        {
            int registeredPlayerIndex = m_Settings.m_RegisteredObjects.IndexOf(m_Settings.m_PlayerObject);

            if (registeredPlayerIndex == -1 || m_Settings.m_PlayerObject == null)
                return;

            if (list.index == registeredPlayerIndex)
            {
                if (list.index + 1 < list.count)
                {
                    list.index++;
                }
                else if (list.index - 1 >= 0)
                {
                    list.index--;
                }
                else
                {
                    list.index = -1;
                }
            }
        }

        private void OnDrawBackground(Rect rect, int index, bool isActive, bool isFocused)
        { }

        public override void OnInspectorGUI()
        {
            using (ChangeCheckScope check = new ChangeCheckScope())
            {
                DrawPropertiesExcluding(serializedObject, "m_Script", nameof(NetworkSettings.m_PlayerObject), nameof(NetworkSettings.m_RegisteredObjects));

                EditorGUILayout.Space();

                EditorGUILayout.LabelField("Objects", EditorStyles.boldLabel);

                UpdatePlayerObject();

                m_RegisteredObjectsList.DoLayoutList();

                if (check.changed)
                { 
                    serializedObject.ApplyModifiedProperties();

                    EditorUtility.SetDirty(target);
                }
            }
        }

        private float OnElementHeight(int index)
        {
            SerializedProperty arrayElement = m_RegisteredObjectsProperty.GetArrayElementAtIndex(index);

            if (arrayElement.objectReferenceValue != null)
            {
                if (arrayElement.objectReferenceValue == m_Settings.m_PlayerObject)
                {
                    if (m_Settings.m_RegisteredObjects.Count == 1 && m_Settings.m_RegisteredObjects[0] == m_Settings.m_PlayerObject)
                    {
                        return m_RegisteredObjectsList.elementHeight;
                    }
                    else return 0f;
                }
            }

            return m_RegisteredObjectsList.elementHeight;
        }

        private void OnAddElement(ReorderableList list)
        {
            m_RegisteredObjectsProperty.InsertArrayElementAtIndex(m_RegisteredObjectsProperty.arraySize);

            SerializedProperty p = m_RegisteredObjectsProperty.GetArrayElementAtIndex(m_RegisteredObjectsProperty.arraySize - 1);

            p.objectReferenceValue = null;

            serializedObject.ApplyModifiedProperties();

            m_RegisteredObjectsList.index = m_RegisteredObjectsProperty.arraySize - 1;
        }

        private void OnDrawElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            SerializedProperty arrayElement = m_RegisteredObjectsProperty.GetArrayElementAtIndex(index);

            if (arrayElement.objectReferenceValue != null)
            {
                if (arrayElement.objectReferenceValue == m_Settings.m_PlayerObject)
                {
                    m_IsPlayerSelected = isFocused || isActive;

                    if (m_Settings.m_RegisteredObjects.Count == 1 && m_Settings.m_RegisteredObjects[0] == m_Settings.m_PlayerObject)
                    {
                        LabelField(rect, "List is Empty");
                    }

                    return;
                }
            }

            rect.height -= 5;

            int registeredPlayerIndex = m_Settings.m_RegisteredObjects.IndexOf(m_Settings.m_PlayerObject);

            if (m_Settings.m_PlayerObject != null && registeredPlayerIndex >= 0 && index > registeredPlayerIndex)
                index--;

            ObjectField(rect, arrayElement, new GUIContent(string.Format("Element {0}", index)));

            m_IsPlayerSelected = false;
        }

        private bool OnCanRemove(ReorderableList list)
        {
            return !m_IsPlayerSelected;
        }

        private void UpdatePlayerObject()
        {
            using (var check = new ChangeCheckScope())
            {
                GameObject initialPlayerObject = (GameObject)m_PlayerObjectProperty.objectReferenceValue;

                EditorGUILayout.ObjectField(m_PlayerObjectProperty);

                if (check.changed)
                {
                    GameObject newPlayerObject = (GameObject)m_PlayerObjectProperty.objectReferenceValue;

                    if (newPlayerObject == null)
                    {
                        if (initialPlayerObject != null && m_Settings.m_RegisteredObjects.Contains(initialPlayerObject))
                        {
                            int index = m_Settings.m_RegisteredObjects.IndexOf(initialPlayerObject);

                            m_RegisteredObjectsProperty.DeleteArrayElementAtIndex(index);

                            m_RegisteredObjectsProperty.DeleteArrayElementAtIndex(index);
                        }
                    }
                    else if (!m_Settings.m_RegisteredObjects.Contains(newPlayerObject))
                    {
                        m_RegisteredObjectsProperty.InsertArrayElementAtIndex(m_RegisteredObjectsProperty.arraySize);

                        SerializedProperty prop = m_RegisteredObjectsProperty.GetArrayElementAtIndex(m_RegisteredObjectsProperty.arraySize - 1);

                        prop.objectReferenceValue = m_PlayerObjectProperty.objectReferenceValue;
                    }
                }
            }
        }
    }
}
