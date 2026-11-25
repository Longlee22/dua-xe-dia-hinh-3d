using System.Collections.Generic;
using UnityEngine;

// Tạo material dùng shader Standard (Built-in RP) cho các bề mặt kim loại / sơn / kính.
public static class MatHelper
{
    static readonly Dictionary<Color, Material> cache = new Dictionary<Color, Material>();
    static readonly Dictionary<string, Material> keyed = new Dictionary<string, Material>();

    static Shader FindShader()
    {
        Shader sh = Shader.Find("Standard");
        if (sh == null) sh = Shader.Find("Universal Render Pipeline/Lit");
        if (sh == null) sh = Shader.Find("Sprites/Default");
        return sh;
    }

    static Material New(Color c)
    {
        var m = new Material(FindShader());
        m.color = c;
        return m;
    }

    static void Surface(Material m, float metallic, float smoothness)
    {
        if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metallic);
        if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", smoothness);
    }

    // Material cơ bản (giữ nguyên API cũ)
    public static Material Make(Color c)
    {
        Material m;
        if (cache.TryGetValue(c, out m) && m != null) return m;
        m = New(c);
        cache[c] = m;
        return m;
    }

    public static Material MakeTextured(Texture2D tex, Vector2 tiling)
    {
        var m = new Material(FindShader());
        m.mainTexture = tex;
        m.mainTextureScale = tiling;
        return m;
    }

    public static Material MakeParticle(Texture2D tex)
    {
        var m = new Material(Shader.Find("Sprites/Default"));
        m.mainTexture = tex;
        return m;
    }

    static Material Keyed(string key, System.Func<Material> build)
    {
        Material m;
        if (keyed.TryGetValue(key, out m) && m != null) return m;
        m = build();
        keyed[key] = m;
        return m;
    }

    static string K(string prefix, Color c) { return prefix + "|" + c.r + "," + c.g + "," + c.b + "," + c.a; }

    // Sơn xe: bóng, phản chiếu kim loại nhẹ
    public static Material MakePaint(Color c)
    {
        return Keyed(K("paint", c), delegate
        {
            var m = New(c);
            Surface(m, 0.55f, 0.82f);
            return m;
        });
    }

    // Nhựa / cao su cứng
    public static Material MakePlastic(Color c, float smoothness = 0.35f)
    {
        return Keyed(K("plastic" + smoothness, c), delegate
        {
            var m = New(c);
            Surface(m, 0.05f, smoothness);
            return m;
        });
    }

    // Lốp xe
    public static Material MakeRubber()
    {
        return Keyed("rubber", delegate
        {
            var m = New(new Color(0.045f, 0.045f, 0.05f));
            Surface(m, 0f, 0.22f);
            return m;
        });
    }

    // Kim loại thô (khung, gầm, ống)
    public static Material MakeMetal(Color c, float smoothness = 0.45f)
    {
        return Keyed(K("metal" + smoothness, c), delegate
        {
            var m = New(c);
            Surface(m, 0.95f, smoothness);
            return m;
        });
    }

    // Inox / chrome
    public static Material MakeChrome()
    {
        return Keyed("chrome", delegate
        {
            var m = New(new Color(0.92f, 0.93f, 0.95f));
            Surface(m, 1f, 0.92f);
            return m;
        });
    }

    // Kính xe: trong suốt, phản chiếu
    public static Material MakeGlass(Color tint)
    {
        return Keyed(K("glass", tint), delegate
        {
            var m = New(tint);
            m.SetOverrideTag("RenderType", "Transparent");
            m.SetFloat("_Mode", 3f);
            m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
            m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m.SetInt("_ZWrite", 0);
            m.DisableKeyword("_ALPHATEST_ON");
            m.DisableKeyword("_ALPHABLEND_ON");
            m.EnableKeyword("_ALPHAPREMULTIPLY_ON");
            m.renderQueue = 3000;
            Surface(m, 0.25f, 0.95f);
            return m;
        });
    }

    // Vật liệu phát sáng (đèn xe)
    public static Material MakeEmissive(Color c, float intensity = 2f)
    {
        return Keyed(K("emissive" + intensity, c), delegate
        {
            var m = New(c);
            Surface(m, 0f, 0.6f);
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", c * intensity);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            return m;
        });
    }
}
