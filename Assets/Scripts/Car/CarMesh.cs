using System.Collections.Generic;
using UnityEngine;

// Sinh mesh thủ công (không cần asset ngoài): hộp bo cạnh, trụ tròn, lốp gai, vòm chắn bùn, cầu.
public static class CarMesh
{
    static readonly Dictionary<string, Mesh> cache = new Dictionary<string, Mesh>();

    static Mesh Cached(string key, System.Func<Mesh> build)
    {
        Mesh m;
        if (cache.TryGetValue(key, out m) && m != null) return m;
        m = build();
        m.name = key;
        cache[key] = m;
        return m;
    }

    // ---------- Hộp bo cạnh (thân xe, nắp capo, cản...) ----------
    public static Mesh BeveledBox(Vector3 size, float bevel)
    {
        string key = "bb" + size.x + "_" + size.y + "_" + size.z + "_" + bevel;
        return Cached(key, delegate { return MakeBeveledBox(size, bevel); });
    }

    static Mesh MakeBeveledBox(Vector3 size, float b)
    {
        float hx = size.x * 0.5f, hy = size.y * 0.5f, hz = size.z * 0.5f;
        float half = Mathf.Min(hx, Mathf.Min(hy, hz));
        b = Mathf.Clamp(b, 0.001f, half * 0.85f);

        var verts = new List<Vector3>();
        var norms = new List<Vector3>();
        var tris = new List<int>();

        System.Action<Vector3, Vector3, Vector3, Vector3, Vector3> quad = (p0, p1, p2, p3, n) =>
        {
            if (Vector3.Dot(Vector3.Cross(p1 - p0, p2 - p0), n) < 0f)
            {
                var tmp = p1; p1 = p3; p3 = tmp;
            }
            int i = verts.Count;
            verts.Add(p0); verts.Add(p1); verts.Add(p2); verts.Add(p3);
            for (int k = 0; k < 4; k++) norms.Add(n);
            tris.Add(i); tris.Add(i + 1); tris.Add(i + 2);
            tris.Add(i); tris.Add(i + 2); tris.Add(i + 3);
        };
        System.Action<Vector3, Vector3, Vector3, Vector3> tri = (p0, p1, p2, n) =>
        {
            if (Vector3.Dot(Vector3.Cross(p1 - p0, p2 - p0), n) < 0f)
            {
                var tmp = p1; p1 = p2; p2 = tmp;
            }
            int i = verts.Count;
            verts.Add(p0); verts.Add(p1); verts.Add(p2);
            norms.Add(n); norms.Add(n); norms.Add(n);
            tris.Add(i); tris.Add(i + 1); tris.Add(i + 2);
        };

        // 6 mặt
        for (int a = 0; a < 3; a++)
        {
            for (int s = -1; s <= 1; s += 2)
            {
                Vector3 n = Vector3.zero; n[a] = s;
                int i = (a + 1) % 3, j = (a + 2) % 3;
                Vector3 u = Vector3.zero; u[i] = 1f;
                Vector3 v = Vector3.zero; v[j] = 1f;
                if (Vector3.Dot(Vector3.Cross(u, v), n) < 0f) { var t = u; u = v; v = t; }
                float su = size[i] * 0.5f - b, sv = size[j] * 0.5f - b;
                Vector3 c = n * (size[a] * 0.5f);
                quad(c - u * su - v * sv, c + u * su - v * sv, c + u * su + v * sv, c - u * su + v * sv, n);
            }
        }

        // 12 cạnh vát
        for (int a = 0; a < 3; a++)
        {
            for (int bb = a + 1; bb < 3; bb++)
            {
                int c = 3 - a - bb;
                for (int sa = -1; sa <= 1; sa += 2)
                {
                    for (int sb = -1; sb <= 1; sb += 2)
                    {
                        Vector3 n = Vector3.zero; n[a] = sa; n[bb] = sb; n.Normalize();
                        float ec = size[c] * 0.5f - b;
                        Vector3 p0 = Vector3.zero, p1 = Vector3.zero, p2 = Vector3.zero, p3 = Vector3.zero;
                        p0[a] = sa * size[a] * 0.5f; p0[bb] = sb * (size[bb] * 0.5f - b); p0[c] = -ec;
                        p1 = p0; p1[c] = ec;
                        p2[a] = sa * (size[a] * 0.5f - b); p2[bb] = sb * size[bb] * 0.5f; p2[c] = ec;
                        p3 = p2; p3[c] = -ec;
                        quad(p0, p1, p2, p3, n);
                    }
                }
            }
        }

        // 8 góc
        for (int sx = -1; sx <= 1; sx += 2)
            for (int sy = -1; sy <= 1; sy += 2)
                for (int sz = -1; sz <= 1; sz += 2)
                {
                    Vector3 n = new Vector3(sx, sy, sz).normalized;
                    Vector3 pa = new Vector3(sx * hx, sy * (hy - b), sz * (hz - b));
                    Vector3 pb = new Vector3(sx * (hx - b), sy * hy, sz * (hz - b));
                    Vector3 pc = new Vector3(sx * (hx - b), sy * (hy - b), sz * hz);
                    tri(pa, pb, pc, n);
                }

        var m = new Mesh();
        m.SetVertices(verts);
        m.SetNormals(norms);
        m.SetTriangles(tris, 0);
        m.RecalculateBounds();
        return m;
    }

    // ---------- Trụ tròn, trục Y, đường kính 1, cao 1 ----------
    public static Mesh Cylinder(int seg)
    {
        return Cached("cyl" + seg, delegate
        {
            var verts = new List<Vector3>();
            var norms = new List<Vector3>();
            var tris = new List<int>();
            for (int i = 0; i <= seg; i++)
            {
                float a = i / (float)seg * Mathf.PI * 2f;
                float x = Mathf.Cos(a) * 0.5f, z = Mathf.Sin(a) * 0.5f;
                verts.Add(new Vector3(x, -0.5f, z)); verts.Add(new Vector3(x, 0.5f, z));
                norms.Add(new Vector3(x * 2f, 0, z * 2f)); norms.Add(new Vector3(x * 2f, 0, z * 2f));
            }
            for (int i = 0; i < seg; i++)
            {
                int k = i * 2;
                tris.Add(k); tris.Add(k + 1); tris.Add(k + 2);
                tris.Add(k + 1); tris.Add(k + 3); tris.Add(k + 2);
            }
            // hai nắp
            for (int s = -1; s <= 1; s += 2)
            {
                int baseI = verts.Count;
                verts.Add(new Vector3(0, s * 0.5f, 0));
                norms.Add(new Vector3(0, s, 0));
                for (int i = 0; i <= seg; i++)
                {
                    float a = i / (float)seg * Mathf.PI * 2f;
                    verts.Add(new Vector3(Mathf.Cos(a) * 0.5f, s * 0.5f, Mathf.Sin(a) * 0.5f));
                    norms.Add(new Vector3(0, s, 0));
                }
                for (int i = 0; i < seg; i++)
                {
                    if (s > 0) { tris.Add(baseI); tris.Add(baseI + 1 + i); tris.Add(baseI + 2 + i); }
                    else { tris.Add(baseI); tris.Add(baseI + 2 + i); tris.Add(baseI + 1 + i); }
                }
            }
            var m = new Mesh();
            m.SetVertices(verts); m.SetNormals(norms); m.SetTriangles(tris, 0);
            m.RecalculateBounds();
            return m;
        });
    }

    // ---------- Cầu (quả cầu) ----------
    public static Mesh Sphere(int seg, int rings)
    {
        return Cached("sph" + seg + "_" + rings, delegate
        {
            var verts = new List<Vector3>();
            var tris = new List<int>();
            for (int r = 0; r <= rings; r++)
            {
                float phi = Mathf.PI * r / rings;
                for (int s = 0; s <= seg; s++)
                {
                    float theta = Mathf.PI * 2f * s / seg;
                    verts.Add(new Vector3(Mathf.Sin(phi) * Mathf.Cos(theta), Mathf.Cos(phi), Mathf.Sin(phi) * Mathf.Sin(theta)) * 0.5f);
                }
            }
            for (int r = 0; r < rings; r++)
                for (int s = 0; s < seg; s++)
                {
                    int a = r * (seg + 1) + s, b = a + seg + 1;
                    tris.Add(a); tris.Add(a + 1); tris.Add(b);
                    tris.Add(a + 1); tris.Add(b + 1); tris.Add(b);
                }
            var m = new Mesh();
            m.SetVertices(verts); m.SetTriangles(tris, 0);
            m.RecalculateNormals(); m.RecalculateBounds();
            return m;
        });
    }

    // ---------- Lốp gai: trục Y, đường kính 1, bề dày 1 ----------
    public static Mesh Tire(int seg)
    {
        return Cached("tire" + seg, delegate
        {
            var verts = new List<Vector3>();
            var tris = new List<int>();
            float[] xs = { -0.5f, -0.36f, 0.36f, 0.5f };
            // 3 vòng ngoài: vai - gai - gai - vai
            for (int ring = 0; ring < xs.Length; ring++)
            {
                for (int i = 0; i < seg; i++)
                {
                    float a = Mathf.PI * 2f * i / seg;
                    float rad;
                    if (ring == 0 || ring == xs.Length - 1) rad = 0.42f;
                    else rad = 0.5f * (1f + ((i + (ring == 2 ? 1 : 0)) % 2 == 0 ? 0.07f : 0f));
                    verts.Add(new Vector3(Mathf.Cos(a) * rad, xs[ring], Mathf.Sin(a) * rad));
                }
            }
            for (int ring = 0; ring < xs.Length - 1; ring++)
                for (int i = 0; i < seg; i++)
                {
                    int i2 = (i + 1) % seg;
                    int a = ring * seg + i, b = ring * seg + i2;
                    int c = (ring + 1) * seg + i, d = (ring + 1) * seg + i2;
                    tris.Add(a); tris.Add(c); tris.Add(d);
                    tris.Add(a); tris.Add(d); tris.Add(b);
                }
            // thành lốp (vành trong) hai bên
            for (int s = -1; s <= 1; s += 2)
            {
                int baseI = verts.Count;
                float y = s * 0.5f;
                for (int i = 0; i < seg; i++)
                {
                    float a = Mathf.PI * 2f * i / seg;
                    verts.Add(new Vector3(Mathf.Cos(a) * 0.42f, y, Mathf.Sin(a) * 0.42f));
                    verts.Add(new Vector3(Mathf.Cos(a) * 0.19f, y, Mathf.Sin(a) * 0.19f));
                }
                for (int i = 0; i < seg; i++)
                {
                    int i2 = (i + 1) % seg;
                    int a = baseI + i * 2, b = baseI + i2 * 2, c = a + 1, d = b + 1;
                    if (s > 0) { tris.Add(a); tris.Add(b); tris.Add(c); tris.Add(c); tris.Add(b); tris.Add(d); }
                    else { tris.Add(a); tris.Add(c); tris.Add(b); tris.Add(c); tris.Add(d); tris.Add(b); }
                }
            }
            var m = new Mesh();
            m.SetVertices(verts); m.SetTriangles(tris, 0);
            m.RecalculateNormals(); m.RecalculateBounds();
            return m;
        });
    }

    // ---------- Vòm chắn bùn: cung nửa vành khăn quanh trục X ----------
    public static Mesh Arch(float inner, float outer, float width, float startDeg, float endDeg, int seg)
    {
        string key = "arch" + inner + "_" + outer + "_" + width + "_" + startDeg + "_" + endDeg + "_" + seg;
        return Cached(key, delegate
        {
            var verts = new List<Vector3>();
            var tris = new List<int>();
            float hw = width * 0.5f;
            for (int i = 0; i <= seg; i++)
            {
                float t = Mathf.Lerp(startDeg, endDeg, i / (float)seg) * Mathf.Deg2Rad;
                float sy = Mathf.Sin(t), cz = Mathf.Cos(t);
                // 4 điểm của tiết diện chữ nhật
                verts.Add(new Vector3(-hw, sy * inner, cz * inner));
                verts.Add(new Vector3(hw, sy * inner, cz * inner));
                verts.Add(new Vector3(hw, sy * outer, cz * outer));
                verts.Add(new Vector3(-hw, sy * outer, cz * outer));
            }
            for (int i = 0; i < seg; i++)
            {
                int a = i * 4, b = (i + 1) * 4;
                for (int k = 0; k < 4; k++)
                {
                    int k2 = (k + 1) % 4;
                    tris.Add(a + k); tris.Add(b + k); tris.Add(b + k2);
                    tris.Add(a + k); tris.Add(b + k2); tris.Add(a + k2);
                }
            }
            // hai đầu mút
            for (int e = 0; e < 2; e++)
            {
                int a = e == 0 ? 0 : seg * 4;
                tris.Add(a + 0); tris.Add(a + 1); tris.Add(a + 2);
                tris.Add(a + 0); tris.Add(a + 2); tris.Add(a + 3);
            }
            var m = new Mesh();
            m.SetVertices(verts); m.SetTriangles(tris, 0);
            m.RecalculateNormals(); m.RecalculateBounds();
            return m;
        });
    }
}
