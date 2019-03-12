using UnityEditor;

using UnityEngine;

namespace AuthoritativeServer.Demo.Editors
{
    [CustomPropertyDrawer(typeof(NotEditableAttribute))]
    public class NotEditablePropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            GUI.enabled = false;

            EditorGUI.PropertyField(position, property);

            GUI.enabled = true;
        }
    }
}
