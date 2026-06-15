#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Shared helpers used by all scene builders.
public static class SceneBuilderUtils
{
    // ── GameObject ────────────────────────────────────────────────────────────

    public static GameObject MakeGO(string name, Transform parent = null)
    {
        var go = new GameObject(name);
        if (parent != null) go.transform.SetParent(parent, false);
        return go;
    }

    // ── UI primitives ─────────────────────────────────────────────────────────

    public static GameObject MakePanel(Transform parent, string name,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 size, Color color)
    {
        var go   = MakeGO(name, parent);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin        = anchorMin;
        rect.anchorMax        = anchorMax;
        rect.pivot            = pivot;
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta        = size;
        go.AddComponent<Image>().color = color;
        return go;
    }

    public static Button MakeButton(Transform parent, string name, string label)
    {
        var go   = MakeGO(name, parent);
        var rect = go.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(0, 36);

        var img = go.AddComponent<Image>();
        img.color = new Color(0.22f, 0.22f, 0.28f);

        var btn    = go.AddComponent<Button>();
        var colors = btn.colors;
        colors.normalColor      = new Color(0.22f, 0.22f, 0.28f);
        colors.highlightedColor = new Color(0.35f, 0.35f, 0.45f);
        colors.pressedColor     = new Color(0.15f, 0.45f, 0.80f);
        colors.selectedColor    = new Color(0.25f, 0.25f, 0.35f);
        btn.colors        = colors;
        btn.targetGraphic = img;

        var textGO   = MakeGO("Text", go.transform);
        var textRect = textGO.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        var tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text      = label;
        tmp.fontSize  = 15;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color     = Color.white;

        return btn;
    }

    public static TextMeshProUGUI MakeLabel(Transform parent, string name, string text,
        int fontSize = 14, TextAlignmentOptions align = TextAlignmentOptions.Center)
    {
        var go   = MakeGO(name, parent);
        var rect = go.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(0, 24);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = fontSize;
        tmp.alignment = align;
        tmp.color     = new Color(0.85f, 0.85f, 0.85f);
        return tmp;
    }

    public static void MakeSeparator(Transform parent, string text)
    {
        var go   = MakeGO("Sep", parent);
        var rect = go.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(0, 20);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text               = text;
        tmp.fontSize           = 11;
        tmp.alignment          = TextAlignmentOptions.Center;
        tmp.color              = new Color(0.5f, 0.5f, 0.5f);
        tmp.textWrappingMode   = TextWrappingModes.NoWrap;
        tmp.overflowMode       = TextOverflowModes.Overflow;
    }

    public static Slider MakeSlider(Transform parent, string name,
        float min, float max, float defaultVal)
    {
        var go   = MakeGO(name, parent);
        var rect = go.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(0, 22);

        // Background track
        var bg     = MakeGO("BG", go.transform);
        var bgRect = bg.AddComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0, 0.25f);
        bgRect.anchorMax = new Vector2(1, 0.75f);
        bgRect.sizeDelta = Vector2.zero;
        bg.AddComponent<Image>().color = new Color(0.15f, 0.15f, 0.15f);

        // Fill area
        var fillArea     = MakeGO("Fill Area", go.transform);
        var fillAreaRect = fillArea.AddComponent<RectTransform>();
        fillAreaRect.anchorMin = new Vector2(0, 0.25f);
        fillAreaRect.anchorMax = new Vector2(1, 0.75f);
        fillAreaRect.offsetMin = new Vector2(5, 0);
        fillAreaRect.offsetMax = new Vector2(-15, 0);

        var fill     = MakeGO("Fill", fillArea.transform);
        var fillRect = fill.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.sizeDelta = new Vector2(10, 0);
        fill.AddComponent<Image>().color = new Color(0.20f, 0.55f, 0.90f);

        // Handle
        var hsa     = MakeGO("Handle Slide Area", go.transform);
        var hsaRect = hsa.AddComponent<RectTransform>();
        hsaRect.anchorMin = Vector2.zero;
        hsaRect.anchorMax = Vector2.one;
        hsaRect.offsetMin = new Vector2(10, 0);
        hsaRect.offsetMax = new Vector2(-10, 0);

        var handle     = MakeGO("Handle", hsa.transform);
        var handleRect = handle.AddComponent<RectTransform>();
        handleRect.anchorMin = new Vector2(0, 0);
        handleRect.anchorMax = new Vector2(0, 1);
        handleRect.sizeDelta = new Vector2(20, 0);
        var handleImg = handle.AddComponent<Image>();
        handleImg.color = Color.white;

        var slider           = go.AddComponent<Slider>();
        slider.fillRect      = fillRect;
        slider.handleRect    = handleRect;
        slider.targetGraphic = handleImg;
        slider.direction     = Slider.Direction.LeftToRight;
        slider.minValue      = min;
        slider.maxValue      = max;
        slider.value         = defaultVal;

        return slider;
    }

    // Info box (top-left corner). Returns the TMP_Text inside.
    public static TextMeshProUGUI MakeInfoBox(Transform canvasParent, string initialText,
        Vector2 size, Vector2 offset)
    {
        var go   = MakeGO("InfoText", canvasParent);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin        = new Vector2(0, 1);
        rect.anchorMax        = new Vector2(0, 1);
        rect.pivot            = new Vector2(0, 1);
        rect.anchoredPosition = offset;
        rect.sizeDelta        = size;
        go.AddComponent<Image>().color = new Color(0, 0, 0, 0.5f);

        var textGO   = MakeGO("Text", go.transform);
        var textRect = textGO.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(8, 4);
        textRect.offsetMax = new Vector2(-8, -4);

        var tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text     = initialText;
        tmp.fontSize = 16;
        tmp.color    = Color.white;
        return tmp;
    }

    // Right-side vertical control panel with a VerticalLayoutGroup.
    public static GameObject MakeRightPanel(Transform canvasParent, float width = 190f)
    {
        var panel = MakePanel(canvasParent, "ControlPanel",
            anchorMin: new Vector2(1, 0), anchorMax: new Vector2(1, 1),
            pivot:     new Vector2(1, 0.5f),
            size:      new Vector2(width, 0),
            color:     new Color(0f, 0f, 0f, 0.80f));

        var vl = panel.AddComponent<VerticalLayoutGroup>();
        vl.padding                = new RectOffset(8, 8, 10, 10);
        vl.spacing                = 5;
        vl.childAlignment         = TextAnchor.UpperCenter;
        vl.childControlWidth      = true;
        vl.childControlHeight     = false;
        vl.childForceExpandWidth  = true;
        vl.childForceExpandHeight = false;
        return panel;
    }

    // Grey border rectangle (LineRenderer) around a world-space area.
    public static void MakeWorldBorder(float halfW, float halfH,
        Color color, float z = 0.1f, float lineWidth = 0.25f)
    {
        var go = new GameObject("WorldBorder");
        go.transform.position = new Vector3(0f, 0f, z);
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace  = true;
        lr.loop           = true;
        lr.positionCount  = 4;
        lr.startWidth     = lineWidth;
        lr.endWidth       = lineWidth;
        lr.material       = new Material(Shader.Find("Sprites/Default"));
        lr.startColor     = color;
        lr.endColor       = color;
        lr.sortingOrder   = -1;
        lr.SetPositions(new Vector3[]
        {
            new(-halfW, -halfH, 0f), new( halfW, -halfH, 0f),
            new( halfW,  halfH, 0f), new(-halfW,  halfH, 0f),
        });
    }

    // Sets a [SerializeField] private field by name.
    public static void SetRef(Object target, string field, Object value)
    {
        var so   = new SerializedObject(target);
        var prop = so.FindProperty(field);
        if (prop == null) { Debug.LogWarning($"[Builder] '{field}' not found on {target.GetType().Name}"); return; }
        prop.objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
#endif
