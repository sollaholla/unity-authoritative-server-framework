using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

using UnityEngine;

using UnityEditor;
using UnityEditorInternal;

namespace AuthoritativeServer.Demo.Editors
{
    [CustomEditor(typeof(InventoryItemDatabase))]
    public class ItemDatabaseEditor : Editor
    {
        #region ITEM FIELDS

        private int m_ItemsGridSelected = -1;
        private bool m_ItemsDropDown = true;
        private GUIContent[] m_ItemsGridContents;
        private SerializedProperty m_ItemsProperty;
        private Editor m_ItemEditor;
        private GameObject m_EditedItemContents;
        private string m_LastItemAssetPath;
        private string m_LastItemAssetName;

        #endregion

        private StandardListAssetManager<InventoryItemStat> Stats;
        private StandardListAssetManager<ItemCategory> m_Categories;
        private StandardListAssetManager<ItemBlueprint> m_Blueprints;

        private void OnEnable()
        {
            m_ItemsProperty = serializedObject.FindProperty("m_Items");
            Stats = new StandardListAssetManager<InventoryItemStat>(serializedObject, "Stats", "Stats/Item Stats", "New Item Stat", "m_Stats", "m_StatName");
            m_Categories = new StandardListAssetManager<ItemCategory>(serializedObject, "Categories", "Item Categories", "New Item Category", "m_Categories", "m_CategoryName");
            m_Blueprints = new StandardListAssetManager<ItemBlueprint>(serializedObject, "Blueprints", "Item Blueprints", "New Item Blueprint", "m_Blueprints", "m_BlueprintName");
        }

        private void OnDisable()
        {
            ApplyLastEditedItem();
            Stats.Dispose();
            m_Categories.Dispose();
            m_Blueprints.Dispose();
        }

        public override void OnInspectorGUI()
        {
            DrawItems();
            Stats.Draw();
            m_Categories.Draw();
            m_Blueprints.Draw();
        }

        #region ITEMS

        private void DrawItems()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            if (GUILayout.Button("Items", EditorStyles.toolbarDropDown))
            {
                m_ItemsDropDown = !m_ItemsDropDown;
            }

            if (m_ItemsDropDown)
            {
                for (int i = 0; i < m_ItemsProperty.arraySize; i++)
                {
                    SerializedProperty element = m_ItemsProperty.GetArrayElementAtIndex(i);

                    if (element.objectReferenceValue != null)
                        continue;

                    m_ItemsProperty.DeleteArrayElementAtIndex(i);
                }

                serializedObject.ApplyModifiedProperties();

                m_ItemsGridContents = new GUIContent[m_ItemsProperty.arraySize + 1];

                for (int i = 0; i < m_ItemsProperty.arraySize; i++)
                {
                    SerializedProperty element = m_ItemsProperty.GetArrayElementAtIndex(i);

                    if (element.objectReferenceValue == null)
                        continue;

                    string path = AssetDatabase.GetAssetPath(element.objectReferenceValue);

                    var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);

                    Texture2D preview = AssetPreview.GetAssetPreview(asset);

                    m_ItemsGridContents[i] = new GUIContent(asset.name, preview, asset.name);
                }

                m_ItemsGridContents[m_ItemsProperty.arraySize] = EditorGUIUtility.IconContent("CollabCreate Icon");
                m_ItemsGridContents[m_ItemsProperty.arraySize].text = "Add New";

                GUIStyle itemContentStyle = new GUIStyle(GUI.skin.button);
                itemContentStyle.imagePosition = ImagePosition.ImageAbove;
                itemContentStyle.fixedWidth = 70;
                itemContentStyle.stretchWidth = true;
                itemContentStyle.fixedHeight = 70;
                itemContentStyle.stretchHeight = true;

                int xCount = (int)(EditorGUIUtility.currentViewWidth / 80);
                int initialSelected = m_ItemsGridSelected;

                Event currentEvent = Event.current;

                using (var check = new EditorGUI.ChangeCheckScope())
                {
                    m_ItemsGridSelected = GUILayout.SelectionGrid(m_ItemsGridSelected, m_ItemsGridContents, xCount, itemContentStyle);

                    if (check.changed)
                    {
                        m_ItemEditor = null;

                        ApplyLastEditedItem();

                        switch (currentEvent.button)
                        {
                            case 0:
                                if (m_ItemsGridSelected == m_ItemsProperty.arraySize)
                                {
                                    m_ItemsGridSelected = initialSelected == m_ItemsProperty.arraySize ? -1 : initialSelected;

                                    Vector2 mousePos = currentEvent.mousePosition;

                                    string[] typeNames = GetItemTypeNames(out var types);

                                    EditorUtility.DisplayCustomMenu(new Rect(mousePos, Vector2.zero), typeNames.Select(x => new GUIContent(x)).ToArray(), -1, OnCreateItemTypeSelected, types);
                                }
                                else if (initialSelected == m_ItemsGridSelected)
                                {
                                    string path = AssetDatabase.GetAssetPath(m_ItemsProperty.GetArrayElementAtIndex(initialSelected).objectReferenceValue);

                                    GameObject obj = AssetDatabase.LoadAssetAtPath<GameObject>(path);

                                    Selection.SetActiveObjectWithContext(obj, obj);
                                }
                                break;
                            case 1:
                                if (m_ItemsGridSelected != m_ItemsProperty.arraySize)
                                {
                                    Vector2 mousePos = currentEvent.mousePosition;

                                    EditorUtility.DisplayCustomMenu(new Rect(mousePos, Vector2.zero), new GUIContent[] { new GUIContent("Delete") }, -1, OnItemDeleteSelected, m_ItemsGridSelected);
                                }
                                break;
                        }
                    }

                    if (m_ItemsGridSelected == m_ItemsProperty.arraySize)
                    {
                        m_ItemsGridSelected = initialSelected == m_ItemsProperty.arraySize ? -1 : initialSelected;
                    }
                }

                if (m_ItemsGridSelected != -1)
                {
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                    SerializedProperty element = m_ItemsProperty.GetArrayElementAtIndex(m_ItemsGridSelected);

                    string assetPath = AssetDatabase.GetAssetPath(element.objectReferenceValue);

                    if (m_EditedItemContents == null)
                    {
                        m_EditedItemContents = PrefabUtility.LoadPrefabContents(assetPath);
                    }

                    CreateCachedEditor(m_EditedItemContents.GetComponent<InventoryItem>(), null, ref m_ItemEditor);

                    using (var check = new EditorGUI.ChangeCheckScope())
                    {
                        m_ItemEditor.OnInspectorGUI();
                        
                        if (check.changed)
                        {
                            string name = m_EditedItemContents.name;
                            string newName = m_EditedItemContents.GetComponent<InventoryItem>().Name;

                            m_LastItemAssetPath = assetPath;
                            m_LastItemAssetName = newName;
                        }
                    }

                    EditorGUILayout.EndVertical();
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void ApplyLastEditedItem()
        {
            if (m_EditedItemContents != null)
            {
                bool rename = false;

                if (!string.IsNullOrEmpty(m_LastItemAssetPath) && !string.IsNullOrEmpty(m_LastItemAssetName))
                {
                    PrefabUtility.SaveAsPrefabAsset(m_EditedItemContents, m_LastItemAssetPath);

                    rename = true;
                }

                PrefabUtility.UnloadPrefabContents(m_EditedItemContents);

                if (rename)
                {
                    AssetDatabase.RenameAsset(m_LastItemAssetPath, m_LastItemAssetName);
                }
            }

            m_LastItemAssetPath = string.Empty;
            m_LastItemAssetName = string.Empty;
        }

        private string[] GetItemTypeNames(out Type[] typeData)
        {
            List<string> typeNames = new List<string>();

            List<Type> typeList = new List<Type>();

            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (Assembly assembly in assemblies)
            {
                Type[] types = assembly.GetTypes();

                foreach (Type t in types)
                {
                    if (typeof(InventoryItem).IsAssignableFrom(t))
                    {
                        typeNames.Add(t.Name);
                        typeList.Add(t);
                    }
                }
            }

            typeData = typeList.ToArray();
            return typeNames.ToArray();
        }

        private void OnItemDeleteSelected(object userData, string[] options, int selected)
        {
            int itemIndex = (int)userData;

            SerializedProperty element = m_ItemsProperty.GetArrayElementAtIndex(itemIndex);

            AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(element.objectReferenceValue));

            element.objectReferenceValue = null;

            m_ItemsProperty.DeleteArrayElementAtIndex(itemIndex);

            serializedObject.ApplyModifiedProperties();
        }

        private void OnCreateItemTypeSelected(object userData, string[] options, int selected)
        {
            Type[] data = (Type[])userData;

            Type selectedType = data[selected];

            GameObject primitiveCube = GameObject.CreatePrimitive(PrimitiveType.Cube);

            primitiveCube.AddComponent(selectedType);
            
            InventoryItem invItem = primitiveCube.GetComponent<InventoryItem>();

            invItem.GenerateID();

            string path = AssetDatabase.GetAssetPath(serializedObject.targetObject);

            if (!Directory.Exists(Path.GetDirectoryName(path) + "/Items/Resources"))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path) + "/Items/Resources");
            }

            string file = Path.GetDirectoryName(path) + "/Items/Resources/New Item #" + (m_ItemsProperty.arraySize + 1) + ".prefab";

            file = AssetDatabase.GenerateUniqueAssetPath(file);

            SerializedObject obj = new SerializedObject(invItem);

            obj.FindProperty("m_ItemName").stringValue = Path.GetFileNameWithoutExtension(file);

            obj.ApplyModifiedProperties();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(primitiveCube, file);

            m_ItemsProperty.InsertArrayElementAtIndex(m_ItemsProperty.arraySize);

            m_ItemsProperty.GetArrayElementAtIndex(m_ItemsProperty.arraySize - 1).objectReferenceValue = prefab.GetComponent(selectedType);

            serializedObject.ApplyModifiedProperties();

            DestroyImmediate(primitiveCube);
        }

        #endregion
    }
}
