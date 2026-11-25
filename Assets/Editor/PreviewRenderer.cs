#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Chụp ảnh preview 3D (xe, tay đua, địa hình) rồi lưu vào Assets/Resources/Images/... để
// các màn chọn hiển thị ảnh thật thay cho ô màu.
public static class PreviewRenderer
{
    const int Size = 1024;

    [MenuItem("Game/1. Render anh chon xe va dia hinh")]
    public static void RenderAll()
    {
        EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        var cam = Camera.main;
        if (cam == null)
        {
            var g = new GameObject("Main Camera"); g.tag = "MainCamera";
            cam = g.AddComponent<Camera>();
        }
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 500f;

        SetupLights();

        Directory.CreateDirectory("Assets/Resources/Images/Cars");
        Directory.CreateDirectory("Assets/Resources/Images/Characters");
        Directory.CreateDirectory("Assets/Resources/Images/Terrains");

        RenderCars(cam);
        RenderDrivers(cam);
        RenderTerrains(cam);

        AssetDatabase.Refresh();
        Debug.Log("[PreviewRenderer] Da xuat anh preview vao Assets/Resources/Images");
    }

    static void SetupLights()
    {
        var lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
        Light sun = null;
        foreach (var l in lights) if (l.type == LightType.Directional) { sun = l; break; }
        if (sun == null)
        {
            var g = new GameObject("Sun");
            sun = g.AddComponent<Light>();
            sun.type = LightType.Directional;
        }
        sun.transform.rotation = Quaternion.Euler(42f, -35f, 0f);
        sun.intensity = 1.25f;
        sun.color = new Color(1f, 0.96f, 0.90f);
        sun.shadows = LightShadows.Soft;
        sun.shadowStrength = 0.85f;

        var fill = new GameObject("Fill").AddComponent<Light>();
        fill.type = LightType.Directional;
        fill.transform.rotation = Quaternion.Euler(15f, 140f, 0f);
        fill.intensity = 0.35f;
        fill.color = new Color(0.65f, 0.80f, 1f);
        fill.shadows = LightShadows.None;

        var rim = new GameObject("Rim").AddComponent<Light>();
        rim.type = LightType.Directional;
        rim.transform.rotation = Quaternion.Euler(20f, 200f, 0f);
        rim.intensity = 0.55f;
        rim.color = new Color(1f, 0.85f, 0.65f);
        rim.shadows = LightShadows.None;

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Skybox;
        RenderSettings.ambientIntensity = 1.05f;

        var probe = new GameObject("ReflectionProbe").AddComponent<ReflectionProbe>();
        probe.size = new Vector3(40f, 25f, 40f);
        probe.resolution = 128;
        probe.transform.position = new Vector3(0f, 3f, 0f);
        probe.RenderProbe();
    }

    static GameObject Ground(Vector3 pos, float smoothness = 0.35f)
    {
        var g = GameObject.CreatePrimitive(PrimitiveType.Plane);
        g.name = "PreviewGround";
        Object.DestroyImmediate(g.GetComponent<Collider>());
        g.transform.position = pos;
        g.transform.localScale = Vector3.one * 6f;
        var m = new Material(Shader.Find("Standard"));
        m.color = new Color(0.13f, 0.15f, 0.19f);
        if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", smoothness);
        if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0.15f);
        g.GetComponent<Renderer>().sharedMaterial = m;
        return g;
    }

    static void RenderCars(Camera cam)
    {
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.07f, 0.09f, 0.15f);

        string[] names = { "Jeep", "Buggy", "MonsterTruck" };
        float[] radius = { 0.40f, 0.35f, 0.70f };
        float[] dist = { 6.4f, 5.6f, 7.8f };

        for (int i = 0; i < 3; i++)
        {
            var ground = Ground(new Vector3(0f, -radius[i] - 0.01f, 0f));
            var car = CarFactory.Create(i, 0, false, 0, Vector3.zero, Quaternion.Euler(0f, -28f, 0f), "Preview");

            Vector3 look = new Vector3(0f, radius[i] * 1.6f + 0.25f, 0f);
            Vector3 dir = new Vector3(0.95f, 0.42f, 1.0f).normalized;
            cam.fieldOfView = 38f;
            cam.transform.position = look + dir * dist[i];
            cam.transform.LookAt(look);

            Render(cam, "Assets/Resources/Images/Cars/" + names[i] + ".png");

            Object.DestroyImmediate(car.gameObject);
            Object.DestroyImmediate(ground);
        }
    }

    static void RenderDrivers(Camera cam)
    {
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.09f, 0.11f, 0.17f);

        for (int i = 0; i < 3; i++)
        {
            var ground = Ground(new Vector3(0f, 0f, 0f), 0.5f);
            var d = CarFactory.CreateDriverPreview(i);

            cam.fieldOfView = 30f;
            cam.transform.position = new Vector3(1.80f, 1.30f, 2.35f);
            cam.transform.LookAt(new Vector3(0f, 0.96f, 0.02f));

            Render(cam, "Assets/Resources/Images/Characters/Driver" + (i + 1) + ".png");

            Object.DestroyImmediate(d);
            Object.DestroyImmediate(ground);
        }
    }

    static void RenderTerrains(Camera cam)
    {
        cam.clearFlags = CameraClearFlags.Skybox;
        RenderSettings.fog = false;

        string[] names = { "Desert", "Forest", "Hills", "Mud" };
        for (int t = 0; t < 4; t++)
        {
            var terrain = TerrainGenerator.Build(t);
            float z = 160f;
            float cx = TerrainGenerator.CenterX(z);
            float y = terrain.SampleHeight(new Vector3(cx, 0f, z));

            cam.fieldOfView = 50f;
            cam.transform.position = new Vector3(cx - 30f, y + 26f, z - 42f);
            cam.transform.LookAt(new Vector3(cx + 4f, y + 3f, z + 90f));

            Render(cam, "Assets/Resources/Images/Terrains/" + names[t] + ".png");

            Object.DestroyImmediate(terrain.gameObject);
            foreach (var go in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
                if (go.name == "Props" || go.name == "InvisibleWalls" || go.name == "Terrain")
                    Object.DestroyImmediate(go);
        }
        RenderSettings.fog = false;
    }

    static void Render(Camera cam, string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        var rt = new RenderTexture(Size, Size, 24, RenderTextureFormat.ARGB32);
        rt.antiAliasing = 8;
        cam.targetTexture = rt;
        cam.Render();

        RenderTexture.active = rt;
        var tex = new Texture2D(Size, Size, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, Size, Size), 0, 0);
        tex.Apply();
        RenderTexture.active = null;
        cam.targetTexture = null;

        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        rt.Release();
        Object.DestroyImmediate(rt);

        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        var ti = AssetImporter.GetAtPath(path) as TextureImporter;
        if (ti != null)
        {
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.spritePixelsPerUnit = 100f;
            ti.mipmapEnabled = false;
            ti.alphaIsTransparency = false;
            ti.maxTextureSize = 2048;
            ti.textureCompression = TextureImporterCompression.CompressedHQ;
            ti.SaveAndReimport();
        }
        Debug.Log("[PreviewRenderer] " + path);
    }
}
#endif
