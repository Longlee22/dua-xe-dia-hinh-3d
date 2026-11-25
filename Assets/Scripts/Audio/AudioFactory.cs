using UnityEngine;

// Tạo âm thanh bằng code. Muốn dùng file thật: đặt vào Assets/Resources/Audio/ với tên music và engine.
public static class AudioFactory
{
    static AudioClip engineClip, musicClip;

    public static AudioClip Engine()
    {
        if (engineClip != null) return engineClip;
        var r = Resources.Load<AudioClip>("Audio/engine");
        if (r != null) return engineClip = r;

        int sr = 22050, n = sr;           // 1 giây, 60 Hz => lặp liền mạch
        var d = new float[n];
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)sr, s = 0;
            for (int k = 1; k <= 8; k++) s += Mathf.Sin(2f * Mathf.PI * 60f * k * t) / k;
            s += 0.15f * Mathf.Sin(2f * Mathf.PI * 30f * t);
            d[i] = Mathf.Clamp(s * 0.35f, -1f, 1f);
        }
        engineClip = AudioClip.Create("EngineLoop", n, 1, sr, false);
        engineClip.SetData(d, 0);
        return engineClip;
    }

    public static AudioClip Music()
    {
        if (musicClip != null) return musicClip;
        var r = Resources.Load<AudioClip>("Audio/music");
        if (r != null) return musicClip = r;

        int sr = 22050, steps = 32;
        float stepLen = 0.25f;
        int per = (int)(sr * stepLen), n = per * steps;
        var d = new float[n];
        float[] lead = { 0,3,7,10,12,10,7,3,  0,3,7,12,15,12,7,3,  -2,2,5,9,12,9,5,2,  -4,0,3,7,10,7,3,0 };
        float[] bassSemi = { 0, 0, -2, -4 };
        var rnd = new System.Random(7);
        for (int s = 0; s < steps; s++)
        {
            float f = 220f * Mathf.Pow(2f, lead[s] / 12f);
            float bf = 55f * Mathf.Pow(2f, bassSemi[s / 8] / 12f);
            for (int i = 0; i < per; i++)
            {
                float t = i / (float)sr;
                float env = Mathf.Exp(-5f * t);
                float v = Mathf.Sign(Mathf.Sin(2f * Mathf.PI * f * t)) * 0.07f * env;
                v += Mathf.Sin(2f * Mathf.PI * f * 2f * t) * 0.03f * env;
                v += Mathf.Sin(2f * Mathf.PI * bf * t) * 0.18f * (0.6f + 0.4f * Mathf.Exp(-3f * t));
                if (s % 2 == 0 && t < 0.10f) v += Mathf.Sin(2f * Mathf.PI * (90f - 400f * t) * t) * 0.35f * Mathf.Exp(-30f * t);
                if (s % 2 == 1 && t < 0.04f) v += ((float)rnd.NextDouble() * 2f - 1f) * 0.10f * Mathf.Exp(-60f * t);
                d[s * per + i] = Mathf.Clamp(v, -1f, 1f);
            }
        }
        musicClip = AudioClip.Create("RaceMusic", n, 1, sr, false);
        musicClip.SetData(d, 0);
        return musicClip;
    }
}
