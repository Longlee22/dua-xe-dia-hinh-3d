using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

// Scene 06_KetQua
public class ResultController : MonoBehaviour
{
    public void Build()
    {
        var canvas = UIHelper.CreateCanvas();
        var r = canvas.transform;
        bool win = GameData.LastWin;
        UIHelper.MakeImage(r, "Background", win ? new Color(0.08f, 0.16f, 0.1f) : new Color(0.07f, 0.09f, 0.15f), Vector2.zero, Vector2.one);
        UIHelper.MakeText(r, "Title", win ? "BẠN ĐÃ CHIẾN THẮNG!" : "KẾT QUẢ CUỘC ĐUA", 90, new Vector2(0.5f, 1f), new Vector2(0, -120), new Vector2(1700, 120),
            TextAnchor.MiddleCenter, win ? new Color(1f, 0.85f, 0.3f) : Color.white);

        string s = "Xe: " + GameData.CarName + "\n"
                 + "Tay đua: " + GameData.DriverName + "\n"
                 + "Địa hình: " + GameData.TerrainName + "\n"
                 + "Thời gian: " + GameData.FormatTime(GameData.LastTime) + "\n"
                 + "Xếp hạng: Top " + GameData.LastRank + "\n\n";
        string board = GameData.LastBoard;
        if (!string.IsNullOrEmpty(board))
        {
            var parts = board.Split('|');
            for (int i = 0; i < parts.Length; i++) s += "Top " + (i + 1) + ": " + parts[i] + "\n";
        }
        UIHelper.MakeText(r, "Info", s, 44, new Vector2(0.5f, 0.5f), new Vector2(0, 0), new Vector2(1000, 600));

        UIHelper.MakeButton(r, "Btn_Again", "Chơi lại", new Vector2(0.5f, 0f), new Vector2(-330, 90), new Vector2(300, 90), new Color(0.15f, 0.6f, 0.25f), 38);
        UIHelper.MakeButton(r, "Btn_Menu", "Menu chính", new Vector2(0.5f, 0f), new Vector2(0, 90), new Vector2(300, 90), new Color(0.2f, 0.4f, 0.75f), 38);
        UIHelper.MakeButton(r, "Btn_Quit", "Thoát", new Vector2(0.5f, 0f), new Vector2(330, 90), new Vector2(300, 90), new Color(0.65f, 0.2f, 0.2f), 38);
    }

    void Start()
    {
        var old = GameObject.Find("Canvas");
        if (old != null) DestroyImmediate(old);   // dựng lại để hiển thị kết quả mới nhất
        Build();
        Time.timeScale = 1f;
        GameObject.Find("Btn_Again").GetComponent<Button>().onClick.AddListener(() => SceneManager.LoadScene(SceneNames.ChonXe));
        GameObject.Find("Btn_Menu").GetComponent<Button>().onClick.AddListener(() => SceneManager.LoadScene(SceneNames.MenuChinh));
        GameObject.Find("Btn_Quit").GetComponent<Button>().onClick.AddListener(() =>
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        });
    }
}
