using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace AuthoritativeServer.Demo
{
    public static class Utilities
    {
        /// <summary>
        /// Finds all components of the specified type in any active scene, even if the object with the component is disabled.
        /// </summary>
        /// <typeparam name="T">The component type.</typeparam>
        /// <returns></returns>
        public static T[] FindAllObjectsOfType<T>() where T : Component
        {
            List<T> tList = new List<T>();

            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);

                GameObject[] rootObjects = scene.GetRootGameObjects();

                for (int j = 0; j < rootObjects.Length; j++)
                {
                    GameObject root = rootObjects[j];

                    tList.AddRange(root.GetComponentsInChildren<T>(true));
                }
            }

            return tList.ToArray();
        }

        /// <summary>
        /// True if any input element is active.
        /// </summary>
        /// <returns></returns>
        public static bool IsInputInUse()
        {
            GameObject selected = EventSystem.current.currentSelectedGameObject;
            if (selected != null)
            {
                UnityEngine.UI.InputField inputField = selected.GetComponent<UnityEngine.UI.InputField>();
                if (inputField != null)
                {
                    return true;
                }

                TMPro.TMP_InputField tmpInputField = selected.GetComponent<TMPro.TMP_InputField>();
                if (tmpInputField != null)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
