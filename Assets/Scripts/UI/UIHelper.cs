using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem.UI;
#endif

// Các hàm dựng UI bằng code (dùng được cả trong Editor lẫn lúc chạy)
public static class UIHelper
{
    public static Font GetFont()
    {
#if UNITY_2022_2_OR_NEWER
        return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
#else
        return Resources.GetBuiltinResource<Font>("Arial.ttf");
#endif
    }

    public static Canvas CreateCanvas()
    {
        var go = new GameObject("Canvas");
        var c = go.AddComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        var sc = go.AddComponent<CanvasScaler>();
        sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(1920, 1080);
        sc.matchWidthOrHeight = 0.5f;
        go.AddComponent<GraphicRaycaster>();
        EnsureEventSystem();
        return c;
    }

    public static void EnsureEventSystem()
    {
#if UNITY_2023_1_OR_NEWER
        if (Object.FindFirstObjectByType<EventSystem>() != null) return;
#else
        if (Object.FindObjectOfType<EventSystem>() != null) return;
#endif
        var es = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        es.AddComponent<InputSystemUIInputModule>();
#else
        es.AddComponent<StandaloneInputModule>();
#endif
    }

    public static Image MakeImage(Transform parent, string name, Color color, Vector2 anchorMin, Vector2 anchorMax)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        var img = go.GetComponent<Image>();
        img.color = color;
        return img;
    }

    public static Text MakeText(Transform parent, string name, string text, int size, Vector2 anchor, Vector2 pos, Vector2 sizeDelta,
        TextAnchor align = TextAnchor.MiddleCenter, Color? color = null)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
        rt.anchoredPosition = pos;
        rt.sizeDelta = sizeDelta;
        var t = go.AddComponent<Text>();
        t.font = GetFont();
        t.text = text;
        t.fontSize = size;
        t.alignment = align;
        t.color = color.HasValue ? color.Value : Color.white;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        return t;
    }

    public static Button MakeButton(Transform parent, string name, string label, Vector2 anchor, Vector2 pos, Vector2 size, Color color, int fontSize = 34)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        var img = go.GetComponent<Image>();
        img.color = color;
        var b = go.GetComponent<Button>();
        b.targetGraphic = img;
        var cb = b.colors;
        cb.highlightedColor = new Color(0.8f, 0.9f, 1f, 1f);
        cb.pressedColor = new Color(0.6f, 0.6f, 0.6f, 1f);
        b.colors = cb;
        if (!string.IsNullOrEmpty(label))
            MakeText(go.transform, "Label", label, fontSize, new Vector2(0.5f, 0.5f), Vector2.zero, size);
        return b;
    }
}
