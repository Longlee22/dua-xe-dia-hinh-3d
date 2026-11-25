using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

// Scene 05_DuaXe: tạo địa hình, xe người chơi + AI, camera, âm thanh nền, HUD, vạch đích, xếp hạng.
public class RaceManager : MonoBehaviour
{
    CarController player;
    readonly List<CarController> all = new List<CarController>();
    Text hudCar, hudDriver, hudTerrain, hudSpeed, hudTime, hudRank, hudMsg, hudNitroLabel;
    RectTransform nitroFill;
    float raceTime;
    bool running, ended;

    void Start()
    {
        Time.timeScale = 1f;
        int terrainType = GameData.SelectedTerrain;
        TerrainGenerator.Build(terrainType);

        float z0 = TerrainGenerator.StartZ;
        player = SpawnCar(GameData.SelectedCar, GameData.SelectedDriver, 0f, true, "Bạn", 1f);
        all.Add(player);
        all.Add(SpawnCar((GameData.SelectedCar + 1) % 3, (GameData.SelectedDriver + 1) % 3, -4.5f, false, "Đối thủ 1", 0.82f));
        all.Add(SpawnCar((GameData.SelectedCar + 2) % 3, (GameData.SelectedDriver + 2) % 3, 4.5f, false, "Đối thủ 2", 0.74f));
        foreach (var c in all) c.raceActive = false;

        SetupCamera();
        SetupMusic();
        CreateFinishLine();
        BuildHud();
        StartCoroutine(Countdown());
    }

    CarController SpawnCar(int type, int driver, float xOff, bool isPlayer, string racerName, float aiFactor)
    {
        float z = TerrainGenerator.StartZ;
        float x = TerrainGenerator.CenterX(z) + xOff;
        Vector3 pos = new Vector3(x, TerrainGenerator.GroundY(x, z) + 1.6f, z);
        var car = CarFactory.Create(type, driver, isPlayer, GameData.SelectedTerrain, pos, Quaternion.Euler(0f, TerrainGenerator.HeadingDeg(z), 0f), racerName);
        if (!isPlayer)
        {
            var ai = car.gameObject.AddComponent<AICarController>();
            ai.speedFactor = aiFactor; ai.laneOffset = xOff * 0.5f;
        }
        return car;
    }

    void SetupCamera()
    {
        var cam = Camera.main;
        if (cam == null)
        {
            var g = new GameObject("Main Camera"); g.tag = "MainCamera";
            cam = g.AddComponent<Camera>(); g.AddComponent<AudioListener>();
        }
        cam.clearFlags = CameraClearFlags.Skybox;
        cam.farClipPlane = 1500f;
        var follow = cam.GetComponent<CameraFollow>();
        if (follow == null) follow = cam.gameObject.AddComponent<CameraFollow>();
        follow.target = player.transform;
        follow.Snap();
    }

    void SetupMusic()
    {
        var g = new GameObject("BackgroundMusic");
        var src = g.AddComponent<AudioSource>();
        src.clip = AudioFactory.Music();
        src.loop = true; src.volume = 0.35f; src.spatialBlend = 0f;
        src.Play();
    }

    void CreateFinishLine()
    {
        float z = TerrainGenerator.FinishZ;
        float x = TerrainGenerator.CenterX(z);
        var root = new GameObject("FinishLine");
        root.transform.position = new Vector3(x, TerrainGenerator.GroundY(x, z), z);
        root.transform.rotation = Quaternion.Euler(0f, TerrainGenerator.HeadingDeg(z), 0f);

        var trigger = root.AddComponent<BoxCollider>();
        trigger.isTrigger = true;
        trigger.center = new Vector3(0, 5f, 0);
        trigger.size = new Vector3(26f, 10f, 3f);
        root.AddComponent<FinishLine>().manager = this;

        // Ô vuông đen trắng
        var tex = new Texture2D(16, 2, TextureFormat.RGB24, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Repeat };
        for (int y = 0; y < 2; y++) for (int xx = 0; xx < 16; xx++) tex.SetPixel(xx, y, ((xx + y) % 2 == 0) ? Color.white : Color.black);
        tex.Apply();

        var bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
        bar.name = "Banner"; bar.transform.SetParent(root.transform, false);
        bar.transform.localPosition = new Vector3(0, 9f, 0); bar.transform.localScale = new Vector3(22f, 1.6f, 0.4f);
        bar.GetComponent<Renderer>().sharedMaterial = MatHelper.MakeTextured(tex, Vector2.one);
        Object.Destroy(bar.GetComponent<Collider>());
        for (int i = -1; i <= 1; i += 2)
        {
            var pole = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pole.name = "Pole"; pole.transform.SetParent(root.transform, false);
            pole.transform.localPosition = new Vector3(i * 11f, 4.5f, 0); pole.transform.localScale = new Vector3(0.6f, 9f, 0.6f);
            pole.GetComponent<Renderer>().sharedMaterial = MatHelper.Make(new Color(0.9f, 0.9f, 0.9f));
            Object.Destroy(pole.GetComponent<Collider>());
        }
    }

    // ---------- HUD ----------
    void BuildHud()
    {
        var r = UIHelper.CreateCanvas().transform;
        var L = TextAnchor.UpperLeft;
        hudCar = UIHelper.MakeText(r, "HUD_Car", "", 36, new Vector2(0, 1), new Vector2(30, -25), new Vector2(700, 50), L);
        hudDriver = UIHelper.MakeText(r, "HUD_Driver", "", 28, new Vector2(0, 1), new Vector2(30, -75), new Vector2(700, 40), L, new Color(0.85f, 0.9f, 1f));
        hudTerrain = UIHelper.MakeText(r, "HUD_Terrain", "", 28, new Vector2(0, 1), new Vector2(30, -112), new Vector2(700, 40), L, new Color(0.85f, 0.9f, 1f));
        hudTime = UIHelper.MakeText(r, "HUD_Time", "", 48, new Vector2(0.5f, 1), new Vector2(0, -25), new Vector2(700, 60));
        hudRank = UIHelper.MakeText(r, "HUD_Rank", "", 44, new Vector2(1, 1), new Vector2(-30, -25), new Vector2(500, 60), TextAnchor.UpperRight, new Color(1f, 0.85f, 0.3f));
        hudSpeed = UIHelper.MakeText(r, "HUD_Speed", "", 54, new Vector2(1, 0), new Vector2(-30, 40), new Vector2(700, 80), TextAnchor.LowerRight);
        hudMsg = UIHelper.MakeText(r, "HUD_Message", "", 110, new Vector2(0.5f, 0.5f), new Vector2(0, 120), new Vector2(1600, 160), TextAnchor.MiddleCenter, new Color(1f, 0.9f, 0.3f));
        UIHelper.MakeText(r, "HUD_Help", "W/S: tiến/lùi  A/D: lái  Space: phanh  Shift: Nitro  R: dựng xe  Esc: menu", 22,
            new Vector2(0, 0), new Vector2(30, 25), new Vector2(1200, 34), L, new Color(1, 1, 1, 0.7f));

        var bg = UIHelper.MakeImage(r, "NitroBG", new Color(0, 0, 0, 0.6f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f));
        bg.rectTransform.pivot = new Vector2(0.5f, 0f);
        bg.rectTransform.anchoredPosition = new Vector2(0, 40);
        bg.rectTransform.sizeDelta = new Vector2(520, 28);
        var fill = UIHelper.MakeImage(bg.transform, "NitroFill", new Color(1f, 0.55f, 0.1f), Vector2.zero, Vector2.one);
        nitroFill = fill.rectTransform;
        hudNitroLabel = UIHelper.MakeText(r, "NitroLabel", "NITRO (Shift)", 24, new Vector2(0.5f, 0f), new Vector2(0, 74), new Vector2(400, 34));

        hudCar.text = "Xe: " + GameData.CarName;
        hudDriver.text = "Tay đua: " + GameData.DriverName;
        hudTerrain.text = "Địa hình: " + GameData.TerrainName;
    }

    IEnumerator Countdown()
    {
        yield return new WaitForSeconds(0.8f);
        for (int i = 3; i >= 1; i--) { hudMsg.text = i.ToString(); yield return new WaitForSeconds(1f); }
        hudMsg.text = "XUẤT PHÁT!";
        foreach (var c in all) c.raceActive = true;
        running = true;
        yield return new WaitForSeconds(1f);
        if (!ended) hudMsg.text = "";
    }

    // ---------- Xếp hạng ----------
    float Progress(CarController c) { return c.finished ? 100000f - c.finishTime : c.transform.position.z; }

    int RankOf(CarController c)
    {
        int rank = 1;
        foreach (var o in all) if (o != c && Progress(o) > Progress(c)) rank++;
        return rank;
    }

    void Update()
    {
        if (InputHelper.EscPressed()) { SceneManager.LoadScene(SceneNames.MenuChinh); return; }
        if (running && !ended) raceTime += Time.deltaTime;

        hudSpeed.text = "Tốc độ: " + Mathf.RoundToInt(player.SpeedKmh) + " km/h";
        hudTime.text = "Thời gian: " + GameData.FormatTime(raceTime);
        hudRank.text = "Hạng: Top " + RankOf(player) + "/" + all.Count;
        nitroFill.anchorMax = new Vector2(Mathf.Clamp01(player.nitro / 100f), 1f);
    }

    public void OnCarFinished(CarController car)
    {
        if (car.finished) return;
        car.finished = true;
        car.finishTime = raceTime;
        if (car != player || ended) return;

        ended = true;
        player.raceActive = false;
        int rank = RankOf(player);

        var order = new List<CarController>(all);
        order.Sort((a, b) => Progress(b).CompareTo(Progress(a)));
        var names = new List<string>();
        foreach (var c in order) names.Add(c.racerName);

        GameData.LastRank = rank;
        GameData.LastWin = rank == 1;
        GameData.LastTime = raceTime;
        GameData.LastBoard = string.Join("|", names.ToArray());

        hudMsg.text = rank == 1 ? "Bạn đã chiến thắng!" : "Bạn về đích - Top " + rank;
        StartCoroutine(GoToResult());
    }

    IEnumerator GoToResult()
    {
        yield return new WaitForSeconds(3f);
        SceneManager.LoadScene(SceneNames.KetQua);
    }
}
