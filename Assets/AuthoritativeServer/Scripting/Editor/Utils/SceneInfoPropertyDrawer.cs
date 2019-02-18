using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(SceneInfo))]
public class SceneInfoPropertyDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        SerializedProperty scenePathProp = property.FindPropertyRelative(nameof(SceneInfo.m_ScenePath));

        SceneAsset previousAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePathProp.stringValue);

        EditorGUILayout.BeginHorizontal();

        position = EditorGUI.PrefixLabel(position, label);

        SceneAsset sceneAsset = (SceneAsset)EditorGUI.ObjectField(position, previousAsset, typeof(SceneAsset), false);

        string sceneAssetPath = string.Empty;

        if (sceneAsset != null)
        {
            sceneAssetPath = AssetDatabase.GetAssetPath(sceneAsset);

            if (!EditorBuildSettings.scenes.Any(x => x.path == sceneAssetPath))
            {
                Debug.LogError("Scene " + sceneAsset.name + " not contained in build settings.");
                sceneAsset = null;
            }
        }

        EditorGUILayout.EndHorizontal();

        if (sceneAsset != previousAsset)
        {
            SerializedProperty sceneNameProp = property.FindPropertyRelative(nameof(SceneInfo.m_SceneName));

            if (sceneAsset == null)
            {
                sceneNameProp.stringValue = string.Empty;
                scenePathProp.stringValue = string.Empty;

                property.serializedObject.ApplyModifiedProperties();
                return;
            }

            sceneNameProp.stringValue = sceneAsset.name;
            scenePathProp.stringValue = sceneAssetPath;

            property.serializedObject.ApplyModifiedProperties();
        }
    }
}
