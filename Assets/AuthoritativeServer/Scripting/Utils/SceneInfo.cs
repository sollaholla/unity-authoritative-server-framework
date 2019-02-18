using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[System.Serializable]
public class SceneInfo
{
    public string m_SceneName;

    [HideInInspector]
    public string m_ScenePath;

    public static bool operator ==(Scene v1, SceneInfo v2)
    {
        return v1.name == v2.m_SceneName;
    }

    public static bool operator !=(Scene v1, SceneInfo v2)
    {
        return v1.name != v2.m_SceneName;
    }

    public override bool Equals(object obj)
    {
        return base.Equals(obj);
    }

    public override int GetHashCode()
    {
        return -199676232 + EqualityComparer<string>.Default.GetHashCode(m_SceneName);
    }
}
