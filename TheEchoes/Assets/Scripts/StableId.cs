using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

[ExecuteAlways]
[DefaultExecutionOrder(-1000)]
public class StableId : MonoBehaviour
{
    private static readonly HashSet<string> registry = new HashSet<string>();

    [SerializeField, HideInInspector]
    private string id;

    public string Id => id;

    private void OnEnable()
    {
        EnsureId();
        Register(id);
    }

    private void OnDisable()
    {
        Unregister(id);
    }

    private void OnValidate()
    {
        if (string.IsNullOrEmpty(id) || registry.Contains(id))
        {
            GenerateNew();
        }
        Register(id);
    }

    private void EnsureId()
    {
        if (string.IsNullOrEmpty(id))
            GenerateNew();
    }

    private void GenerateNew()
    {
        string newId;
        do { newId = Guid.NewGuid().ToString("N"); }
        while (registry.Contains(newId));

        Unregister(id);
        id = newId;

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            EditorUtility.SetDirty(this);
            if (gameObject.scene.IsValid())
                EditorSceneManager.MarkSceneDirty(gameObject.scene);
        }
#endif
    }

    private static void Register(string key)
    {
        if (string.IsNullOrEmpty(key)) return;
        registry.Add(key);
    }

    private static void Unregister(string key)
    {
        if (string.IsNullOrEmpty(key)) return;
        registry.Remove(key);
    }
}