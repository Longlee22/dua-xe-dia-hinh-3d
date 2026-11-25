using UnityEngine;

public static class SceneNames
{
    public const string MenuChinh = "01_MenuChinh";
    public const string ChonXe = "02_ChonXe";
    public const string ChonNhanVat = "03_ChonNhanVat";
    public const string ChonDiaHinh = "04_ChonDiaHinh";
    public const string DuaXe = "05_DuaXe";
    public const string KetQua = "06_KetQua";
}

// Lưu / đọc dữ liệu bằng PlayerPrefs
public static class GameData
{
    public static readonly string[] CarNames = { "Jeep", "Buggy", "Monster Truck" };
    public static readonly string[] DriverNames = { "Tay đua 1", "Tay đua 2", "Tay đua 3" };
    public static readonly string[] TerrainNames = { "Sa mạc", "Rừng", "Đồi núi", "Đường bùn" };
    public static readonly float[] TerrainGrip = { 0.9f, 1.0f, 1.1f, 0.6f };
    public static readonly Color[] DustColors =
    {
        new Color(0.85f, 0.72f, 0.45f, 0.55f), new Color(0.45f, 0.40f, 0.30f, 0.45f),
        new Color(0.62f, 0.60f, 0.57f, 0.50f), new Color(0.35f, 0.25f, 0.15f, 0.60f)
    };

    const string KCar = "SelectedCar", KDriver = "SelectedDriver", KTerrain = "SelectedTerrain";
    const string KWin = "LastWin", KRank = "LastRank", KTime = "LastTime", KBoard = "LastBoard";

    public static int SelectedCar
    {
        get { return Mathf.Clamp(PlayerPrefs.GetInt(KCar, 0), 0, CarNames.Length - 1); }
        set { PlayerPrefs.SetInt(KCar, value); PlayerPrefs.Save(); }
    }
    public static int SelectedDriver
    {
        get { return Mathf.Clamp(PlayerPrefs.GetInt(KDriver, 0), 0, DriverNames.Length - 1); }
        set { PlayerPrefs.SetInt(KDriver, value); PlayerPrefs.Save(); }
    }
    public static int SelectedTerrain
    {
        get { return Mathf.Clamp(PlayerPrefs.GetInt(KTerrain, 0), 0, TerrainNames.Length - 1); }
        set { PlayerPrefs.SetInt(KTerrain, value); PlayerPrefs.Save(); }
    }

    public static string CarName { get { return CarNames[SelectedCar]; } }
    public static string DriverName { get { return DriverNames[SelectedDriver]; } }
    public static string TerrainName { get { return TerrainNames[SelectedTerrain]; } }

    // Kết quả cuộc đua gần nhất
    public static bool LastWin { get { return PlayerPrefs.GetInt(KWin, 0) == 1; } set { PlayerPrefs.SetInt(KWin, value ? 1 : 0); } }
    public static int LastRank { get { return PlayerPrefs.GetInt(KRank, 1); } set { PlayerPrefs.SetInt(KRank, value); } }
    public static float LastTime { get { return PlayerPrefs.GetFloat(KTime, 0f); } set { PlayerPrefs.SetFloat(KTime, value); } }
    public static string LastBoard { get { return PlayerPrefs.GetString(KBoard, ""); } set { PlayerPrefs.SetString(KBoard, value); PlayerPrefs.Save(); } }

    public static string FormatTime(float t)
    {
        int m = (int)(t / 60f), s = (int)(t % 60f);
        return m.ToString("00") + ":" + s.ToString("00");
    }
}
