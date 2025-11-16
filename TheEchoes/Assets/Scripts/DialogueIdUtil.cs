using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public static class DialogueIdUtil
{
    public static string GetStableId(GameObject go)
    {
        if (!go) return null;
        var sceneName = go.scene.IsValid() ? go.scene.name : "NoScene";
        return $"{sceneName}:{GetHierarchyPath(go.transform)}";
    }

    public static string GetHierarchyPath(Transform t)
    {
        if (t == null) return "";
        var stack = new Stack<string>();
        while (t != null)
        {
            stack.Push(t.name);
            t = t.parent;
        }
        return string.Join("/", stack);
    }

    public static Dictionary<string, GameObject> BuildScenePathIndex()
    {
        var map = new Dictionary<string, GameObject>();
        var scene = SceneManager.GetActiveScene();
        var roots = scene.GetRootGameObjects();
        foreach (var root in roots)
            IndexRecursive(scene.name, root.transform, map);
        return map;
    }

    private static void IndexRecursive(string sceneName, Transform t, Dictionary<string, GameObject> map)
    {
        if (t == null) return;
        var id = $"{sceneName}:{GetHierarchyPath(t)}";
        if (!map.ContainsKey(id)) map[id] = t.gameObject;

        for (int i = 0; i < t.childCount; i++)
            IndexRecursive(sceneName, t.GetChild(i), map);
    }
}