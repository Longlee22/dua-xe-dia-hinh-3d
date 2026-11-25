using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

// Dùng chung cho 3 scene: 02_ChonXe, 03_ChonNhanVat, 04_ChonDiaHinh
public class SelectionMenu : MonoBehaviour
{
    public enum Kind { Xe, NhanVat, DiaHinh }
    public Kind kind = Kind.Xe;

    string[] Names()
    {
        switch (kind) { case Kind.Xe: return GameData.CarNames; case Kind.NhanVat: return GameData.DriverNames; default: return GameData.TerrainNames; }
    }
    string[] Subs()
    {
        switch (kind)
        {
            case Kind.Xe: return new[] { "Cân bằng, bền bỉ", "Nhẹ, nhanh, linh hoạt", "Nặng, khỏe, bánh lớn" };
            case Kind.NhanVat: return new[] { "Màu xanh dương", "Màu vàng", "Màu hồng" };
            default: return new[] { "Cát trơn, bụi nhiều", "Cây dày, sương mù", "Đá, đồi cao", "Bùn rất trơn" };
        }
    }
    // Tên file ảnh đặt trong Assets/Resources/Images/<Folder>/
    string[] SpriteNames()
    {
        switch (kind)
        {
            case Kind.Xe: return new[] { "Jeep", "Buggy", "MonsterTruck" };
            case Kind.NhanVat: return new[] { "Driver1", "Driver2", "Driver3" };
            default: return new[] { "Desert", "Forest", "Hills", "Mud" };
        }
    }
    string Folder() { return kind == Kind.Xe ? "Images/Cars" : kind == Kind.NhanVat ? "Images/Characters" : "Images/Terrains"; }
    string Title() { return kind == Kind.Xe ? "CHỌN XE" : kind == Kind.NhanVat ? "CHỌN NHÂN VẬT" : "CHỌN ĐỊA HÌNH"; }
    string PrevScene() { return kind == Kind.Xe ? SceneNames.MenuChinh : kind == Kind.NhanVat ? SceneNames.ChonXe : SceneNames.ChonNhanVat; }
    string NextScene() { return kind == Kind.Xe ? SceneNames.ChonNhanVat : kind == Kind.NhanVat ? SceneNames.ChonDiaHinh : SceneNames.DuaXe; }
    Color[] Colors()
    {
        switch (kind)
        {
            case Kind.Xe: return new[] { new Color(0.35f, 0.45f, 0.25f), new Color(0.9f, 0.5f, 0.15f), new Color(0.75f, 0.2f, 0.2f) };
            case Kind.NhanVat: return new[] { new Color(0.25f, 0.5f, 0.9f), new Color(0.95f, 0.8f, 0.2f), new Color(0.95f, 0.45f, 0.7f) };
            default: return new[] { new Color(0.85f, 0.7f, 0.4f), new Color(0.2f, 0.5f, 0.2f), new Color(0.5f, 0.5f, 0.55f), new Color(0.35f, 0.25f, 0.15f) };
        }
    }

    public void Build()
    {
        var canvas = UIHelper.CreateCanvas();
        var r = canvas.transform;
        UIHelper.MakeImage(r, "Background", new Color(0.07f, 0.09f, 0.15f), Vector2.zero, Vector2.one);
        UIHelper.MakeText(r, "Title", Title(), 80, new Vector2(0.5f, 1f), new Vector2(0, -90), new Vector2(1400, 110));

        var names = Names(); var subs = Subs(); var cols = Colors(); var sprites = SpriteNames();
        int n = names.Length;
        float cw = 360f, ch = 520f, gap = 40f;
        for (int i = 0; i < n; i++)
        {
            float x = (i - (n - 1) / 2f) * (cw + gap);
            var card = UIHelper.MakeButton(r, "Card_" + i, "", new Vector2(0.5f, 0.5f), new Vector2(x, -10), new Vector2(cw, ch), new Color(0.16f, 0.2f, 0.3f));
            var pic = UIHelper.MakeImage(card.transform, "Pic", cols[i], Vector2.zero, Vector2.one);
            pic.rectTransform.offsetMin = new Vector2(14, 174);
            pic.rectTransform.offsetMax = new Vector2(-14, -14);
            pic.raycastTarget = false;
            UIHelper.MakeText(pic.transform, "Hint", "(Thay bằng ảnh thật)\nResources/" + Folder() + "/" + sprites[i], 20,
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(cw - 40, 80), TextAnchor.MiddleCenter, new Color(1, 1, 1, 0.75f));
            UIHelper.MakeText(card.transform, "Name", names[i], 40, new Vector2(0.5f, 0f), new Vector2(0, 105), new Vector2(cw - 20, 56));
            UIHelper.MakeText(card.transform, "Sub", subs[i], 24, new Vector2(0.5f, 0f), new Vector2(0, 52), new Vector2(cw - 20, 40), TextAnchor.MiddleCenter, new Color(0.75f, 0.8f, 0.9f));
        }
        UIHelper.MakeButton(r, "Btn_Back", "< Quay lại", new Vector2(0f, 0f), new Vector2(40, 40), new Vector2(260, 80), new Color(0.35f, 0.38f, 0.48f), 32);
    }

    void Start()
    {
        if (GameObject.Find("Canvas") == null) Build();
        var names = Names(); var sprites = SpriteNames();
        for (int i = 0; i < names.Length; i++)
        {
            var go = GameObject.Find("Card_" + i);
            if (go == null) continue;
            int idx = i;
            go.GetComponent<Button>().onClick.AddListener(() => Select(idx));
            // Nếu có ảnh thật trong Resources thì hiển thị thay cho ô màu
            var sp = Resources.Load<Sprite>(Folder() + "/" + sprites[i]);
            var pic = go.transform.Find("Pic");
            if (sp != null && pic != null)
            {
                var img = pic.GetComponent<Image>();
                img.sprite = sp; img.color = Color.white; img.preserveAspect = true;
                var hint = pic.Find("Hint");
                if (hint != null) hint.gameObject.SetActive(false);
            }
        }
        var back = GameObject.Find("Btn_Back");
        if (back != null) back.GetComponent<Button>().onClick.AddListener(() => SceneManager.LoadScene(PrevScene()));
    }

    void Select(int index)
    {
        switch (kind)
        {
            case Kind.Xe: GameData.SelectedCar = index; break;
            case Kind.NhanVat: GameData.SelectedDriver = index; break;
            default: GameData.SelectedTerrain = index; break;
        }
        SceneManager.LoadScene(NextScene());
    }
}
