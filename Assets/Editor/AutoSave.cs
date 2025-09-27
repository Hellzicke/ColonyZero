#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System;

[InitializeOnLoad]
public static class AutoSave
{
    const float IntervalSeconds = 120f; // save every 2 minutes
    static double nextRun;

    static AutoSave()
    {
        nextRun = EditorApplication.timeSinceStartup + IntervalSeconds;
        EditorApplication.update += OnUpdate;
        EditorApplication.playModeStateChanged += OnPlayMode;
        EditorApplication.focusChanged += OnFocus;
    }

    static void OnUpdate()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
        if (EditorApplication.timeSinceStartup < nextRun) return;
        SaveAll(); nextRun = EditorApplication.timeSinceStartup + IntervalSeconds;
    }

    static void OnPlayMode(PlayModeStateChange s)
    {
        if (s == PlayModeStateChange.ExitingEditMode) SaveAll(); // before Play
    }

    static void OnFocus(bool focused)
    {
        if (!focused) SaveAll(); // alt-tab or editor loses focus
    }

    static void SaveAll()
    {
        try { EditorSceneManager.SaveOpenScenes(); AssetDatabase.SaveAssets(); }
        catch (Exception e) { Debug.LogWarning("[AutoSave] Failed: " + e.Message); }
        // Debug.Log("[AutoSave] Saved at " + DateTime.Now.ToLongTimeString());
    }
}
#endif

