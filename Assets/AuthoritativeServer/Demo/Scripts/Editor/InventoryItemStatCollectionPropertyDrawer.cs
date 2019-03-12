using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace AuthoritativeServer.Demo.Editors
{
    [CustomPropertyDrawer(typeof(InventoryItemStatCollection))]
    public class InventoryItemStatCollectionPropertyDrawer : PropertyDrawer
    {
        private bool m_Init;

        private ReorderableList m_List;
        private SerializedProperty m_ListProperty;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            Init(property);
            return m_List.GetHeight();
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            Init(property);
            m_List.DoList(position);
        }

        private void Init(SerializedProperty property)
        {
            if (m_Init)
                return;

            m_List = new ReorderableList(property.serializedObject, m_ListProperty = property.FindPropertyRelative("m_StatInstances"));
            m_List.drawHeaderCallback = OnDrawHeader;
            m_List.drawElementCallback = OnDrawElement;
            m_List.onAddCallback = OnAddElement;

            m_Init = true;
        }

        private void OnAddElement(ReorderableList list)
        {
            list.serializedProperty.InsertArrayElementAtIndex(list.serializedProperty.arraySize);
            list.serializedProperty.GetArrayElementAtIndex(list.serializedProperty.arraySize - 1).FindPropertyRelative("m_Stat").objectReferenceValue = null;
        }

        private void OnDrawElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            SerializedProperty statInstanceProperty = m_ListProperty.GetArrayElementAtIndex(index);
            SerializedProperty statProperty = statInstanceProperty.FindPropertyRelative("m_Stat");

            rect.y += 2.5f;
            rect.height -= 5;

            if (statProperty.objectReferenceValue == null)
            {
                EditorGUI.PropertyField(rect, statProperty);
            }
            else
            {
                EditorGUI.PropertyField(new Rect(0, -100, 0, 0), statProperty);

                SerializedProperty valueProperty = statInstanceProperty.FindPropertyRelative("m_Value");
                SerializedObject statObject = new SerializedObject(statProperty.objectReferenceValue);

                SerializedProperty statName = statObject.FindProperty("m_StatName");
                SerializedProperty statType = statObject.FindProperty("m_StatType");

                GUIStyle style = new GUIStyle
                {
                    richText = true
                };

                switch ((StatType)statType.enumValueIndex)
                {
                    case StatType.Fixed:
                        SerializedProperty maxStat = statObject.FindProperty("m_MaxValue");

                        rect = EditorGUI.PrefixLabel(rect, new GUIContent(string.Format("{0}", statName.stringValue)), style);
                        rect.width -= 50;

                        valueProperty.floatValue = Mathf.Clamp(EditorGUI.FloatField(rect, valueProperty.floatValue), 0, maxStat.floatValue);

                        rect.x += rect.width;
                        EditorGUI.LabelField(rect, "/" + maxStat.floatValue);
                        break;
                    case StatType.Add:
                        string prefixString = isActive ? valueProperty.floatValue >= 0 ? "Add" : "Deplete" : valueProperty.floatValue >= 0 ? "<color=green>Add</color>" : "<color=red>Deplete</color>";
                        rect = EditorGUI.PrefixLabel(rect, new GUIContent(string.Format("{0} {1}", prefixString, statName.stringValue)), style);
                        valueProperty.floatValue = EditorGUI.FloatField(rect, valueProperty.floatValue);
                        break;
                }
            }
        }

        private void OnDrawHeader(Rect rect)
        {
            EditorGUI.LabelField(rect, "Stats");
        }
    }
}
