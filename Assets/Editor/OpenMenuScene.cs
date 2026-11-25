using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Tự mở scene Menu khi mở project (tránh trường hợp Unity mở scene "Untitled" trống
// nên bấm Play chỉ thấy bầu trời).
[InitializeOnLoad]
static class OpenMenuScene
{
    const string MenuScene = "Assets/Scenes/01_MenuChinh.unity";

    static OpenMenuScene()
    {
        EditorApplication.delayCall += AutoOpen;
    }

    static void AutoOpen()
    {
        if (Application.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;

        var active = EditorSceneManager.GetActiveScene();
        // Chỉ tự mở khi scene hiện tại là scene mới, chưa lưu (Untitled)
        if (!string.IsNullOrEmpty(active.path)) return;
        if (System.IO.File.Exists(MenuScene) == false) return;

        EditorSceneManager.OpenScene(MenuScene);
    }

    [MenuItem("Game/Open Menu Scene")]
    public static void OpenMenu()
    {
        if (System.IO.File.Exists(MenuScene)) EditorSceneManager.OpenScene(MenuScene);
    }
}
