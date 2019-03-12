using UnityEditor;

using UnityEngine;

namespace AuthoritativeServer.Demo.Editors
{
    [CustomPropertyDrawer(typeof(ItemCollection))]
    public class ItemCollectionPropertyDrawer : PropertyDrawer
    {
        private bool m_Init;
        private GUIContent[] m_SlotContents;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return 0f;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            Init(property);

            EditorGUILayout.Space();

            EditorGUI.indentLevel--;

            SerializedProperty nameProperty = property.FindPropertyRelative("m_Name");

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            GUIStyle toolbarStyle = new GUIStyle(EditorStyles.toolbarDropDown)
            {
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                fontSize = 8
            };

            SerializedProperty expandedProperty = property.FindPropertyRelative("m_Expanded");

            GUIContent toolbarContent = new GUIContent(string.IsNullOrEmpty(nameProperty.stringValue) ? "Item Collection" : nameProperty.stringValue + " Collection");

            Rect collectionExpandRect = GUILayoutUtility.GetRect(toolbarContent, toolbarStyle, GUILayout.ExpandWidth(true));

            SerializedProperty collectionsProp = property.serializedObject.FindProperty("m_Collections");

            SerializedProperty defaultCollectionIndexProp = property.serializedObject.FindProperty("m_DefaultCollectionIndex");

            SerializedProperty craftCollectionIndexProp = property.serializedObject.FindProperty("m_CraftingCollectionIndex");

            if (defaultCollectionIndexProp.intValue > collectionsProp.arraySize)
            {
                defaultCollectionIndexProp.intValue = collectionsProp.arraySize - 1;
            }

            if (craftCollectionIndexProp.intValue > collectionsProp.arraySize)
            {
                craftCollectionIndexProp.intValue = collectionsProp.arraySize - 1;
            }

            SerializedProperty defaultCollectionProp = defaultCollectionIndexProp.intValue == -1 ? null : collectionsProp.GetArrayElementAtIndex(defaultCollectionIndexProp.intValue);

            SerializedProperty craftCollectionProp = craftCollectionIndexProp.intValue == -1 ? null : collectionsProp.GetArrayElementAtIndex(craftCollectionIndexProp.intValue);

            bool isDefaultCollection = false;
            bool isDefaultCraftCollection = false;

            if (defaultCollectionProp?.propertyPath == property.propertyPath)
            {
                isDefaultCollection = true;
            }

            if (craftCollectionProp?.propertyPath == property.propertyPath)
            {
                isDefaultCraftCollection = true;
            }

            GUI.Label(collectionExpandRect, toolbarContent, toolbarStyle);

            GUIContent defaultCollectionIcon = EditorGUIUtility.IconContent(isDefaultCollection ? "d_winbtn_mac_max" : "d_winbtn_mac_inact");
            defaultCollectionIcon.tooltip = "Will be highlighted if this is the default item collection";
            bool setDefaultCollection = GUI.Button(new Rect(collectionExpandRect) { width = 20 }, defaultCollectionIcon, EditorStyles.label);

            Rect defaultCraftRect = new Rect(collectionExpandRect) { width = 20 };
            defaultCraftRect.x += 20;
            GUIContent craftIcon = EditorGUIUtility.IconContent(isDefaultCraftCollection ? "CylinderTargetBehaviour Icon" : "d_CylinderTargetBehaviour Icon");
            craftIcon.tooltip = "Will be highlighted if this is the crafting collection";
            bool setDefaultCraftCollection = GUI.Button(defaultCraftRect, craftIcon, EditorStyles.label);

            bool expandProperty = GUI.Button(collectionExpandRect, "", GUIStyle.none);

            if (setDefaultCollection)
            {
                int ourCollectionIndex = GetCollectionIndex(property, collectionsProp);

                if (ourCollectionIndex != -1)
                    defaultCollectionIndexProp.intValue = ourCollectionIndex;
            }
            else if (setDefaultCraftCollection)
            {
                int ourCollectionIndex = GetCollectionIndex(property, collectionsProp);

                if (craftCollectionIndexProp.intValue == ourCollectionIndex)
                    craftCollectionIndexProp.intValue = -1;
                else if (ourCollectionIndex != -1)
                    craftCollectionIndexProp.intValue = ourCollectionIndex;
            }
            else if (expandProperty)
            {
                expandedProperty.boolValue = !expandedProperty.boolValue;
            }

            if (expandedProperty.boolValue)
            {

                EditorGUILayout.Space();

                GUIContent nameContent = GetNameContent();

                EditorGUI.indentLevel++;

                EditorGUILayout.PropertyField(nameProperty, nameContent);

                EditorGUILayout.PropertyField(property.FindPropertyRelative("m_AllowedItemTypes"), new GUIContent("Allow Types", "Allow only specific types of items."));

                SerializedProperty slotExpandedProp = property.FindPropertyRelative("m_SlotsExpanded");

                SerializedProperty slotProp = property.FindPropertyRelative("m_Slots");

                slotProp.arraySize = EditorGUILayout.IntField("Slot Count", slotProp.arraySize);

                SerializedProperty refProp = property.FindPropertyRelative("m_IsReferenceCollection");

                EditorGUILayout.PropertyField(refProp);

                SerializedProperty stackProp = property.FindPropertyRelative("m_CanStackInCollection");

                EditorGUILayout.PropertyField(stackProp);

                if (GUILayout.Button("Edit Slots", EditorStyles.toolbarButton, GUILayout.ExpandWidth(true)))
                {
                    slotExpandedProp.boolValue = !slotExpandedProp.boolValue;
                }

                if (slotExpandedProp.boolValue)
                {
                    EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

                    SerializedProperty categoryOverrideProp = property.FindPropertyRelative("m_CategoryOverride");

                    EditorGUILayout.PropertyField(categoryOverrideProp, new GUIContent("Category Override", "Allows you to mass edit the category mask property for every slot."));

                    SerializedProperty categoryOverrideArrayProp = categoryOverrideProp.FindPropertyRelative("m_Categories");

                    if (GUILayout.Button("Apply To All", EditorStyles.miniButton))
                    {
                        for (int i = 0; i < slotProp.arraySize; i++)
                        {
                            SerializedProperty element = slotProp.GetArrayElementAtIndex(i);

                            SerializedProperty catMaskProp = element.FindPropertyRelative("m_CategoryMask");

                            SerializedProperty categoryArrayProp = catMaskProp.FindPropertyRelative("m_Categories");

                            catMaskProp.FindPropertyRelative("m_Mask").intValue = categoryOverrideProp.FindPropertyRelative("m_Mask").intValue;

                            categoryArrayProp.arraySize = categoryOverrideArrayProp.arraySize;

                            for (int j = 0; j < categoryOverrideArrayProp.arraySize; j++)
                            {
                                categoryArrayProp.GetArrayElementAtIndex(j).objectReferenceValue = categoryOverrideArrayProp.GetArrayElementAtIndex(j).objectReferenceValue;
                            }
                        }
                    }

                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                    EditorGUILayout.LabelField("Slots", EditorStyles.centeredGreyMiniLabel);

                    SerializedProperty selectedSlotProp = property.FindPropertyRelative("m_Selected");

                    m_SlotContents = new GUIContent[slotProp.arraySize];

                    for (int i = 0; i < slotProp.arraySize; i++)
                    {
                        m_SlotContents[i] = new GUIContent("[" + i + "]");
                    }

                    selectedSlotProp.intValue = GUILayout.SelectionGrid(selectedSlotProp.intValue, m_SlotContents, 5, EditorStyles.miniButton);

                    SerializedProperty selectedSlot = slotProp.GetArrayElementAtIndex(selectedSlotProp.intValue);

                    selectedSlot.isExpanded = true;

                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                    EditorGUILayout.PropertyField(selectedSlot, new GUIContent("[" + selectedSlotProp.intValue + "]"), true);

                    EditorGUILayout.EndVertical();

                    EditorGUILayout.EndVertical();
                }
            }

            EditorGUI.indentLevel--;

            EditorGUILayout.EndVertical();

            EditorGUI.indentLevel++;
        }

        private static int GetCollectionIndex(SerializedProperty property, SerializedProperty collectionsProp)
        {
            int ourCollectionIndex = -1;

            for (int i = 0; i < collectionsProp.arraySize; i++)
            {
                SerializedProperty element = collectionsProp.GetArrayElementAtIndex(i);

                if (element.propertyPath == property.propertyPath)
                {
                    ourCollectionIndex = i;
                    break;
                }
            }

            return ourCollectionIndex;
        }

        private static GUIContent GetNameContent()
        {
            GUIContent nameContent = new GUIContent("Name", "Interfaces with the same collection name will link to this collection at runtime.");
            return nameContent;
        }

        private void Init(SerializedProperty property)
        {
            if (m_Init)
                return;

            SerializedProperty slotProp = property.FindPropertyRelative("m_Slots");

            m_Init = true;
        }
    }
}
