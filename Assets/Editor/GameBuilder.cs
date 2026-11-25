#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Tự động tạo cấu trúc thư mục + 6 scene + Build Settings.
// Chạy tự động lần đầu khi import dự án, hoặc thủ công: menu  Game > Build All (Folders + Scenes)
[InitializeOnLoad]
public static class GameBuilder
{
    static readonly string[] Folders =
    {
        "Assets/Scenes", "Assets/Scripts", "Assets/Scripts/Core", "Assets/Scripts/UI", "Assets/Scripts/Car", "Assets/Scripts/Race", "Assets/Scripts/Audio",
        "Assets/Prefabs", "Assets/Materials", "Assets/Textures", "Assets/Terrain", "Assets/UI",
        "Assets/Models", "Assets/Models/Cars", "Assets/Models/Characters", "Assets/Models/Environment",
        "Assets/Audio", "Assets/Audio/Music", "Assets/Audio/SFX",
        "Assets/Resources", "Assets/Resources/Images", "Assets/Resources/Images/Cars", "Assets/Resources/Images/Characters",
        "Assets/Resources/Images/Terrains", "Assets/Resources/Audio"
    };

    static GameBuilder() { EditorApplication.delayCall += AutoBuild; }

    static void AutoBuild()
    {
        if (Application.isPlaying || EditorApplication.isCompiling) return;
        if (File.Exists("Assets/Scenes/" + SceneNames.MenuChinh + ".unity")) return;
        BuildAll();
    }

    [MenuItem("Game/Build All (Folders + Scenes)")]
    public static void BuildAll()
    {
        foreach (var f in Folders) EnsureFolder(f);

        MakeScene(SceneNames.MenuChinh, true, go => go.AddComponent<MenuController>().Build());
        MakeScene(SceneNames.ChonXe, true, go => { var s = go.AddComponent<SelectionMenu>(); s.kind = SelectionMenu.Kind.Xe; s.Build(); });
        MakeScene(SceneNames.ChonNhanVat, true, go => { var s = go.AddComponent<SelectionMenu>(); s.kind = SelectionMenu.Kind.NhanVat; s.Build(); });
        MakeScene(SceneNames.ChonDiaHinh, true, go => { var s = go.AddComponent<SelectionMenu>(); s.kind = SelectionMenu.Kind.DiaHinh; s.Build(); });
        MakeScene(SceneNames.DuaXe, false, go => { go.name = "RaceManager"; go.AddComponent<RaceManager>(); });
        MakeScene(SceneNames.KetQua, true, go => go.AddComponent<ResultController>().Build());

        string[] order = { SceneNames.MenuChinh, SceneNames.ChonXe, SceneNames.ChonNhanVat, SceneNames.ChonDiaHinh, SceneNames.DuaXe, SceneNames.KetQua };
        var list = new EditorBuildSettingsScene[order.Length];
        for (int i = 0; i < order.Length; i++) list[i] = new EditorBuildSettingsScene("Assets/Scenes/" + order[i] + ".unity", true);
        EditorBuildSettings.scenes = list;

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorSceneManager.OpenScene("Assets/Scenes/" + SceneNames.MenuChinh + ".unity");
        Debug.Log("[GameBuilder] Đã tạo thư mục + 6 scene. Mở 01_MenuChinh và bấm Play!");
    }

    static void MakeScene(string name, bool uiScene, System.Action<GameObject> setup)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        if (uiScene && Camera.main != null)
        {
            Camera.main.clearFlags = CameraClearFlags.SolidColor;
            Camera.main.backgroundColor = new Color(0.07f, 0.09f, 0.15f);
        }
        var go = new GameObject("Controller");
        setup(go);
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/" + name + ".unity");
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace("\\", "/");
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }
}
#endif
