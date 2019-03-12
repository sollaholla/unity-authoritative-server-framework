using System;
using System.Collections.Generic;
using System.Reflection;

using UnityEditor;
using static UnityEditor.EditorGUI;

using UnityEngine;
using System.Linq;

namespace AuthoritativeServer.Demo.Editors
{
    [CustomPropertyDrawer(typeof(ItemCategoryTypeMask))]
    public class ItemCategoryTypeMaskPropertyDrawer : PropertyDrawer
    {
        private static List<ItemCategory> m_Cats;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            InitializeItemTypes();

            if (m_Cats == null || m_Cats.Count <= 0)
                return;

            using (var check = new ChangeCheckScope())
            {
                int mask = property.FindPropertyRelative("m_Mask").intValue;
                mask = MaskField(position, label, mask, m_Cats.Select(x => x.Name).ToArray());

                if (check.changed)
                {
                    property.FindPropertyRelative("m_Mask").intValue = mask;
                    SerializedProperty types = property.FindPropertyRelative("m_Categories");
                    types.ClearArray();

                    for (int i = 0; i < m_Cats.Count; i++)
                    {
                        if ((mask & (1 << i)) != 0)
                        {
                            types.InsertArrayElementAtIndex(types.arraySize);
                            types.GetArrayElementAtIndex(types.arraySize - 1).objectReferenceValue = m_Cats[i];
                        }
                    }

                    property.serializedObject.ApplyModifiedProperties();
                }
            }
        }

        private static void InitializeItemTypes()
        {
            if (m_Cats != null)
                return;

            string[] cats = AssetDatabase.FindAssets("t: " + nameof(ItemCategory));
            foreach (string guid in cats)
            {
                if (m_Cats == null)
                    m_Cats = new List<ItemCategory>();

                string path = AssetDatabase.GUIDToAssetPath(guid);
                m_Cats.Add(AssetDatabase.LoadAssetAtPath<ItemCategory>(path));
            }
        }
    }

    [CustomPropertyDrawer(typeof(InventoryItemTypeMask))]
    public class InventoryItemTypeMaskPropertyDrawer : PropertyDrawer
    {
        private static List<string> m_Types;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            InitializeItemTypes();

            using (var check = new ChangeCheckScope())
            {
                int mask = property.FindPropertyRelative("m_Mask").intValue;
                mask = MaskField(position, label, mask, m_Types.ToArray());

                if (check.changed)
                {
                    property.FindPropertyRelative("m_Mask").intValue = mask;
                    SerializedProperty types = property.FindPropertyRelative("m_Types");
                    types.ClearArray();

                    for (int i = 0; i < m_Types.Count; i++)
                    {
                        if ((mask & (1 << i)) != 0)
                        {
                            types.InsertArrayElementAtIndex(types.arraySize);
                            types.GetArrayElementAtIndex(types.arraySize - 1).stringValue = m_Types[i];
                        }
                    }

                    property.serializedObject.ApplyModifiedProperties();
                }
            }
        }

        private static void InitializeItemTypes()
        {
            if (m_Types != null)
                return;

            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (Assembly assembly in assemblies)
            {
                Type[] types = assembly.GetTypes();
                foreach (Type type in types)
                {
                    if (typeof(InventoryItem).IsAssignableFrom(type))
                    {
                        if (m_Types == null)
                            m_Types = new List<string>();

                        m_Types.Add(type.Name);
                    }
                }
            }
        }
    }
}
