using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

// Scene 01_MenuChinh
public class MenuController : MonoBehaviour
{
    public void Build()
    {
        var canvas = UIHelper.CreateCanvas();
        var r = canvas.transform;
        UIHelper.MakeImage(r, "Background", new Color(0.07f, 0.09f, 0.15f), Vector2.zero, Vector2.one);
        UIHelper.MakeImage(r, "Stripe", new Color(0.95f, 0.5f, 0.1f), new Vector2(0, 0.62f), new Vector2(1, 0.635f));
        UIHelper.MakeText(r, "Title", "ĐUA XE ĐỊA HÌNH 3D", 110, new Vector2(0.5f, 1f), new Vector2(0, -170), new Vector2(1700, 150));
        UIHelper.MakeText(r, "Subtitle", "Off-road Racing", 40, new Vector2(0.5f, 1f), new Vector2(0, -290), new Vector2(1000, 60), TextAnchor.MiddleCenter, new Color(1f, 0.7f, 0.3f));
        UIHelper.MakeButton(r, "Btn_Start", "Bắt đầu", new Vector2(0.5f, 0.5f), new Vector2(0, -60), new Vector2(460, 110), new Color(0.15f, 0.6f, 0.25f), 48);
        UIHelper.MakeButton(r, "Btn_Quit", "Thoát", new Vector2(0.5f, 0.5f), new Vector2(0, -200), new Vector2(460, 110), new Color(0.65f, 0.2f, 0.2f), 48);
        UIHelper.MakeText(r, "Hint", "W/S: tiến/lùi   A/D: trái/phải   Space: phanh   Shift: Nitro   R: dựng xe", 26,
            new Vector2(0.5f, 0f), new Vector2(0, 40), new Vector2(1500, 40), TextAnchor.MiddleCenter, new Color(0.7f, 0.75f, 0.85f));
    }

    void Start()
    {
        if (GameObject.Find("Canvas") == null) Build();
        Time.timeScale = 1f;
        GameObject.Find("Btn_Start").GetComponent<Button>().onClick.AddListener(() => SceneManager.LoadScene(SceneNames.ChonXe));
        GameObject.Find("Btn_Quit").GetComponent<Button>().onClick.AddListener(Quit);
    }

    void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
