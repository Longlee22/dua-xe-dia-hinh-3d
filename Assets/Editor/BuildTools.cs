using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class BuildTools
{
    private const string ProductName = "Đua Xe Địa Hình 3D";
    private const string CompanyName = "Nhom Do An";
    private const string AppId = "com.nhomdoan.duaxediahinh3d";
    private const string WinDir = "Build/Windows";
    private const string WinExe = "Build/Windows/DuaXeDiaHinh3D.exe";
    private const string AndroidDir = "Build/Android";

    [MenuItem("Game/Build Windows x64")]
    public static void BuildWindows()
    {
        ConfigureCommon();
        ConfigureIcons(NamedBuildTarget.Standalone);
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Standalone, AppId + ".windows");
        if (Directory.Exists(WinDir)) Directory.Delete(WinDir, true);
        Directory.CreateDirectory(WinDir);
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = EnabledScenes(),
            locationPathName = WinExe,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        });
        Finish(report, WinExe);
    }

    [MenuItem("Game/Build Smoke Race Scene")]
    public static void BuildSmoke()
    {
        ConfigureCommon();
        const string dir = "Build/Smoke";
        if (Directory.Exists(dir)) Directory.Delete(dir, true);
        Directory.CreateDirectory(dir);
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { "Assets/Scenes/05_DuaXe.unity" },
            locationPathName = dir + "/DuaXeSmoke.exe",
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        });
        Finish(report, dir + "/DuaXeSmoke.exe");
    }
    [MenuItem("Game/Build Android APK")]
    public static void BuildAndroidApk()
    {
        ConfigureCommon();
        ConfigureIcons(NamedBuildTarget.Android);
        EditorUserBuildSettings.buildAppBundle = false;
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, AppId);
        PlayerSettings.Android.bundleVersionCode = 1;
        Directory.CreateDirectory(AndroidDir);
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = EnabledScenes(),
            locationPathName = AndroidDir + "/DuaXeDiaHinh3D.apk",
            target = BuildTarget.Android,
            options = BuildOptions.None
        });
        Finish(report, AndroidDir + "/DuaXeDiaHinh3D.apk");
    }

    [MenuItem("Game/Build Android AAB")]
    public static void BuildAndroidAab()
    {
        ConfigureCommon();
        ConfigureIcons(NamedBuildTarget.Android);
        EditorUserBuildSettings.buildAppBundle = true;
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, AppId);
        PlayerSettings.Android.bundleVersionCode = 1;
        Directory.CreateDirectory(AndroidDir);
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = EnabledScenes(),
            locationPathName = AndroidDir + "/DuaXeDiaHinh3D.aab",
            target = BuildTarget.Android,
            options = BuildOptions.None
        });
        Finish(report, AndroidDir + "/DuaXeDiaHinh3D.aab");
    }

    private static string[] EnabledScenes()
    {
        var list = new System.Collections.Generic.List<string>();
        foreach (var s in EditorBuildSettings.scenes)
            if (s.enabled) list.Add(s.path);
        return list.ToArray();
    }

    private static void ConfigureCommon()
    {
        PlayerSettings.companyName = CompanyName;
        PlayerSettings.productName = ProductName;
        PlayerSettings.bundleVersion = "1.0.0";
        PlayerSettings.defaultScreenWidth = 1920;
        PlayerSettings.defaultScreenHeight = 1080;
        PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
        PlayerSettings.resizableWindow = true;
        PlayerSettings.runInBackground = true;
        RemoveForcedShaders();
        EnsureBuildMaterials();
        AssetDatabase.SaveAssets();
    }

    private static void RemoveForcedShaders()
    {
        var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset");
        if (assets == null || assets.Length == 0) return;
        var so = new SerializedObject(assets[0]);
        var arr = so.FindProperty("m_AlwaysIncludedShaders");
        if (arr == null) return;
        var remove = new System.Collections.Generic.HashSet<string> {
            "Standard", "Standard (Specular setup)",
            "Nature/Terrain/Diffuse", "Nature/Terrain/Standard",
            "Hidden/Internal-Colored"
        };
        for (int i = arr.arraySize - 1; i >= 0; i--)
        {
            var sh = arr.GetArrayElementAtIndex(i).objectReferenceValue as Shader;
            if (sh != null && remove.Contains(sh.name))
            {
                arr.DeleteArrayElementAtIndex(i);
                Debug.Log("[BUILD] Removed forced shader: " + sh.name);
            }
        }
        so.ApplyModifiedProperties();
    }
    private static void EnsureBuildMaterials()
    {
        const string dir = "Assets/Resources/BuildShaders";
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder(dir))
            AssetDatabase.CreateFolder("Assets/Resources", "BuildShaders");
        CreateMaterialAsset(dir + "/Standard.mat", "Standard");
        CreateMaterialAsset(dir + "/Terrain.mat", "Nature/Terrain/Diffuse");
        CreateMaterialAsset(dir + "/TerrainStandard.mat", "Nature/Terrain/Standard");
        CreateMaterialAsset(dir + "/Sprite.mat", "Sprites/Default");
        AssetDatabase.SaveAssets();
    }

    private static void CreateMaterialAsset(string path, string shaderName)
    {
        if (AssetDatabase.LoadAssetAtPath<Material>(path) != null) return;
        var sh = Shader.Find(shaderName);
        if (sh == null) { Debug.LogWarning("[BUILD] Shader not found: " + shaderName); return; }
        var m = new Material(sh);
        AssetDatabase.CreateAsset(m, path);
        Debug.Log("[BUILD] Created material " + path + " (" + shaderName + ")");
    }
    private static void ConfigureIcons(NamedBuildTarget target)
    {
        const string path = "Assets/Art/Icon.png";
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null && (!importer.isReadable || importer.textureCompression != TextureImporterCompression.Uncompressed))
        {
            importer.textureType = TextureImporterType.Default;
            importer.isReadable = true;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 2048;
            importer.SaveAndReimport();
        }
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (tex == null)
        {
            Debug.LogWarning("[BUILD] Icon not found at " + path);
            return;
        }
        PlayerSettings.SetIcons(target, new[] { tex }, IconKind.Application);
        Debug.Log("[BUILD] Icon set for " + target + " (" + tex.width + "x" + tex.height + ")");
    }

    private static void Finish(BuildReport report, string path)
    {
        var s = report.summary;
        Debug.Log("[BUILD] " + s.result + " | " + path + " | " +
                  (s.totalSize / (1024f * 1024f)).ToString("0.0") + " MB | " + s.totalTime);
        if (Application.isBatchMode)
            EditorApplication.Exit(s.result == BuildResult.Succeeded ? 0 : 1);
    }
}
