using UnityEngine;
using UnityEngine.Rendering;

// Tạo địa hình bằng Unity Terrain: đồi, 5 dốc, 4 hố, đường đua quanh co với khúc cua.
public static class TerrainGenerator
{
    public const float SizeX = 300f, SizeZ = 900f, MaxHeight = 60f;
    public const float StartZ = 30f, FinishZ = 850f, HalfWidth = 8f;

    static readonly float[] Ramps = { 130f, 280f, 430f, 580f, 730f };   // 5 đoạn dốc
    static readonly float[] Pits = { 210f, 360f, 510f, 660f };          // 4 cái hố

    // Đường tim của đường đua (uốn lượn trái - phải)
    public static float CenterX(float z) { return 150f + 45f * Mathf.Sin(z / 95f) + 12f * Mathf.Sin(z / 37f + 1f); }
    public static float Slope(float z) { return (CenterX(z + 1f) - CenterX(z - 1f)) * 0.5f; }
    public static float HeadingDeg(float z) { return Mathf.Atan(Slope(z)) * Mathf.Rad2Deg; }

    static float Gauss(float z, float c, float a, float s) { float d = (z - c) / s; return a * Mathf.Exp(-d * d * 0.5f); }

    public static float TrackHeight(float z)
    {
        float h = 6f;
        foreach (var c in Ramps) h += Gauss(z, c, 9f, 18f);
        foreach (var c in Pits) h -= Gauss(z, c, 2.2f, 5f);
        return h;
    }

    public static float GroundY(float x, float z)
    {
        var t = Terrain.activeTerrain;
        return t == null ? 0f : t.SampleHeight(new Vector3(x, 0f, z)) + t.transform.position.y;
    }

    static Texture2D NoiseTex(Color c, float variation, int seed)
    {
        int n = 64;
        var tex = new Texture2D(n, n, TextureFormat.RGB24, true);
        var rnd = new System.Random(seed);
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float k = Mathf.Lerp(1f - variation, 1f + variation * 0.5f, Mathf.PerlinNoise(x * 0.15f + seed, y * 0.15f));
                k *= 1f + ((float)rnd.NextDouble() - 0.5f) * variation * 0.5f;
                tex.SetPixel(x, y, new Color(c.r * k, c.g * k, c.b * k));
            }
        tex.wrapMode = TextureWrapMode.Repeat;
        tex.Apply();
        return tex;
    }

    // type: 0 Sa mạc, 1 Rừng, 2 Đồi núi, 3 Đường bùn
    public static Terrain Build(int type)
    {
        type = Mathf.Clamp(type, 0, 3);
        Color[] ground = { new Color(0.86f, 0.72f, 0.42f), new Color(0.18f, 0.42f, 0.16f), new Color(0.48f, 0.46f, 0.43f), new Color(0.30f, 0.22f, 0.13f) };
        Color[] track = { new Color(0.72f, 0.58f, 0.32f), new Color(0.42f, 0.32f, 0.20f), new Color(0.38f, 0.33f, 0.28f), new Color(0.20f, 0.14f, 0.08f) };
        float[] amps = { 6f, 10f, 16f, 5f };
        float amp = amps[type];

        int res = 1025;
        var h = new float[res, res];
        for (int zi = 0; zi < res; zi++)
        {
            float z = zi / (res - 1f) * SizeZ;
            float cx = CenterX(z), sl = Slope(z), th = TrackHeight(z);
            float norm = 1f / Mathf.Sqrt(1f + sl * sl);
            for (int xi = 0; xi < res; xi++)
            {
                float x = xi / (res - 1f) * SizeX;
                float d = Mathf.Abs(x - cx) * norm;
                float w = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(HalfWidth + 2f, HalfWidth + 24f, d));
                float noise = Mathf.PerlinNoise(x * 0.018f + 11.3f, z * 0.018f + 7.7f) * amp
                            + Mathf.PerlinNoise(x * 0.06f, z * 0.06f) * amp * 0.15f;
                float edge = Mathf.Min(Mathf.Min(x, SizeX - x), Mathf.Min(z, SizeZ - z));
                float outside = 8f + noise + Mathf.InverseLerp(HalfWidth + 10f, HalfWidth + 60f, d) * amp * 0.6f
                              + Mathf.InverseLerp(40f, 0f, edge) * 18f;
                h[zi, xi] = Mathf.Clamp01(Mathf.Lerp(th, outside, w) / MaxHeight);
            }
        }

        var td = new TerrainData();
        td.heightmapResolution = res;
        td.size = new Vector3(SizeX, MaxHeight, SizeZ);
        td.SetHeights(0, 0, h);

        var l0 = new TerrainLayer { diffuseTexture = NoiseTex(ground[type], 0.25f, 3), tileSize = new Vector2(8, 8) };
        var l1 = new TerrainLayer { diffuseTexture = NoiseTex(track[type], 0.20f, 9), tileSize = new Vector2(6, 6) };
        td.terrainLayers = new[] { l0, l1 };
        int ar = 512;
        td.alphamapResolution = ar;
        var a = new float[ar, ar, 2];
        for (int zi = 0; zi < ar; zi++)
        {
            float z = zi / (ar - 1f) * SizeZ;
            float cx = CenterX(z), sl = Slope(z);
            float norm = 1f / Mathf.Sqrt(1f + sl * sl);
            for (int xi = 0; xi < ar; xi++)
            {
                float x = xi / (ar - 1f) * SizeX;
                float d = Mathf.Abs(x - cx) * norm;
                float t = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(HalfWidth - 1f, HalfWidth + 2f, d));
                a[zi, xi, 0] = 1f - t; a[zi, xi, 1] = t;
            }
        }
        td.SetAlphamaps(0, 0, a);

        var go = Terrain.CreateTerrainGameObject(td);
        go.name = "Terrain";
        var terrain = go.GetComponent<Terrain>();
        if (GraphicsSettings.currentRenderPipeline != null)
        {
            var sh = Shader.Find("Universal Render Pipeline/Terrain/Lit");
            if (sh != null) terrain.materialTemplate = new Material(sh);
        }

        AddBounds();
        AddProps(type, terrain);

        if (type == 1 || type == 3)
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogDensity = type == 1 ? 0.006f : 0.004f;
            RenderSettings.fogColor = type == 1 ? new Color(0.55f, 0.65f, 0.6f) : new Color(0.55f, 0.5f, 0.45f);
        }
        return terrain;
    }

    static void AddBounds()
    {
        var root = new GameObject("InvisibleWalls");
        Vector3[] pos = { new Vector3(SizeX / 2, 30, -2), new Vector3(SizeX / 2, 30, SizeZ + 2), new Vector3(-2, 30, SizeZ / 2), new Vector3(SizeX + 2, 30, SizeZ / 2) };
        Vector3[] size = { new Vector3(SizeX, 80, 4), new Vector3(SizeX, 80, 4), new Vector3(4, 80, SizeZ), new Vector3(4, 80, SizeZ) };
        for (int i = 0; i < 4; i++)
        {
            var w = new GameObject("Wall" + i); w.transform.SetParent(root.transform);
            w.transform.position = pos[i];
            w.AddComponent<BoxCollider>().size = size[i];
        }
    }

    static void AddProps(int type, Terrain terrain)
    {
        var root = new GameObject("Props").transform;
        var rnd = new System.Random(42);
        int count = type == 1 ? 260 : type == 2 ? 160 : type == 0 ? 90 : 70;
        for (int i = 0; i < count; i++)
        {
            float x = 6f + (float)rnd.NextDouble() * (SizeX - 12f), z = 6f + (float)rnd.NextDouble() * (SizeZ - 12f);
            float sl = Slope(z);
            float d = Mathf.Abs(x - CenterX(z)) / Mathf.Sqrt(1f + sl * sl);
            if (d < HalfWidth + 7f) continue;
            float y = terrain.SampleHeight(new Vector3(x, 0, z));
            float s = 0.7f + (float)rnd.NextDouble() * 1.1f;
            if (type == 1)   // cây
            {
                var trunk = Prop(PrimitiveType.Cylinder, root, new Vector3(x, y + 2.5f * s, z), new Vector3(0.7f, 2.5f, 0.7f) * s, new Color(0.35f, 0.22f, 0.1f), true);
                Prop(PrimitiveType.Sphere, root, new Vector3(x, y + 6.2f * s, z), Vector3.one * 4.5f * s, new Color(0.12f, 0.38f, 0.12f), false);
            }
            else if (type == 0 && i % 3 != 0)   // xương rồng
                Prop(PrimitiveType.Capsule, root, new Vector3(x, y + 2f * s, z), new Vector3(0.9f, 2.2f, 0.9f) * s, new Color(0.2f, 0.5f, 0.2f), true);
            else   // đá
                Prop(PrimitiveType.Sphere, root, new Vector3(x, y + 0.6f * s, z), new Vector3(2.5f, 1.6f, 2.2f) * s * (type == 2 ? 1.8f : 1f),
                     type == 3 ? new Color(0.28f, 0.2f, 0.14f) : new Color(0.5f, 0.48f, 0.45f), true);
        }
    }

    static GameObject Prop(PrimitiveType t, Transform parent, Vector3 pos, Vector3 scale, Color c, bool collider)
    {
        var g = GameObject.CreatePrimitive(t);
        g.transform.SetParent(parent);
        g.transform.position = pos; g.transform.localScale = scale;
        g.GetComponent<Renderer>().sharedMaterial = MatHelper.Make(c);
        if (!collider) { var col = g.GetComponent<Collider>(); if (col != null) Object.Destroy(col); }
        return g;
    }
}
