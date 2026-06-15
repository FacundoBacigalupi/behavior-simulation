#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Rendering.Universal;
using TMPro;
using BehaviorSimulation.Core;
using BehaviorSimulation.AntColony;

public static class AntColonySceneBuilder
{
    const string ScenePath    = "Assets/_Project/Scenes/05_AntColony.unity";
    const string SettingsPath = "Assets/_Project/ScriptableObjects/AntSettings.asset";
    const float  HalfW = 19f, HalfH = 11f;

    [MenuItem("Behavior Simulation/Build Scene — 05 Ant Colony")]
    public static void Build()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // ── Camera ─────────────────────────────────────────────────────────────
        var camGO = new GameObject("Main Camera");
        camGO.tag = "MainCamera";
        var cam   = camGO.AddComponent<Camera>();
        cam.orthographic     = true;
        cam.orthographicSize = 13f;
        cam.transform.position = new Vector3(0f, 0f, -10f);
        cam.backgroundColor  = new Color(0.05f, 0.05f, 0.08f);
        cam.clearFlags       = CameraClearFlags.SolidColor;
        camGO.AddComponent<UniversalAdditionalCameraData>();
        camGO.AddComponent<CameraController2D>();

        // ── Settings SO ────────────────────────────────────────────────────────
        EnsureDir("Assets/_Project/ScriptableObjects");
        var settings = AssetDatabase.LoadAssetAtPath<AntSettings>(SettingsPath);
        if (settings == null)
        {
            settings = ScriptableObject.CreateInstance<AntSettings>();
            AssetDatabase.CreateAsset(settings, SettingsPath);
        }
        var sso = new SerializedObject(settings);
        SetFloat(sso, "boundsHalfW",   HalfW);
        SetFloat(sso, "boundsHalfH",   HalfH);
        SetInt  (sso, "gridW",         190);
        SetInt  (sso, "gridH",         110);
        SetFloat(sso, "evaporateRate", 0.12f);
        SetFloat(sso, "depositAmount", 0.08f);
        SetInt  (sso, "antCount",      120);
        SetFloat(sso, "antSpeed",      3.5f);
        SetFloat(sso, "turnSpeed",     5f);
        SetFloat(sso, "wanderNoise",   0.8f);
        SetFloat(sso, "sensorDist",    1.5f);
        SetFloat(sso, "sensorAngle",   40f);
        SetFloat(sso, "pickupRadius",  1.4f);
        SetFloat(sso, "nestRadius",    2.0f);
        SetInt  (sso, "foodPerSource", 150);
        sso.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.SaveAssets();

        // ── SimulationController ───────────────────────────────────────────────
        var scGO = new GameObject("SimulationController");
        var sc   = scGO.AddComponent<SimulationController>();

        // ── World border ───────────────────────────────────────────────────────
        SceneBuilderUtils.MakeWorldBorder(HalfW, HalfH,
            new Color(0.3f, 0.3f, 0.4f), z: 0.2f, lineWidth: 0.12f);

        // ── Pheromone grid ────────────────────────────────────────────────────
        var gridGO = new GameObject("PheromoneGrid");
        gridGO.transform.position = Vector3.forward * 0.5f;  // behind ants (z > 0)
        var pGrid = gridGO.AddComponent<PheromoneGrid>();
        SceneBuilderUtils.SetRef(pGrid, "settings", settings);

        // ── Nest visual ────────────────────────────────────────────────────────
        var nestGO = new GameObject("Nest");
        nestGO.transform.position = Vector3.forward * 0.3f;
        var nestSR    = nestGO.AddComponent<SpriteRenderer>();
        nestSR.sprite = SpriteFactory.Circle(32, 8f);   // 4 WU circle
        nestSR.color  = new Color(0.55f, 0.35f, 0.10f, 0.85f);
        nestSR.sortingOrder = -5;

        // ── Food sources ───────────────────────────────────────────────────────
        var foodPositions = new Vector2[]
        {
            new(-14f,  3f),
            new( 14f, -4f),
            new(  1f,  9f),
        };
        var foodGOs = new FoodSource[foodPositions.Length];
        for (int i = 0; i < foodPositions.Length; i++)
        {
            var go = new GameObject($"FoodSource_{i}");
            go.transform.position = new Vector3(foodPositions[i].x, foodPositions[i].y, 0.3f);
            var sr    = go.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteFactory.Circle(24, 8f);   // 3 WU circle
            sr.color  = new Color(0.20f, 0.70f, 0.15f);
            sr.sortingOrder = -5;
            var fs = go.AddComponent<FoodSource>();
            SceneBuilderUtils.SetRef(fs, "maxAmount", null);  // int — use SetInt below
            SetIntOnObject(fs, "maxAmount", 150);
            foodGOs[i] = fs;
        }

        // ── Ant Manager ────────────────────────────────────────────────────────
        var mgrGO   = new GameObject("AntManager");
        var manager = mgrGO.AddComponent<AntManager>();
        SceneBuilderUtils.SetRef(manager, "settings", settings);
        SceneBuilderUtils.SetRef(manager, "grid",     pGrid);

        // Wire food sources array
        var mgrSO  = new SerializedObject(manager);
        var srcArr = mgrSO.FindProperty("foodSources");
        srcArr.arraySize = foodGOs.Length;
        for (int i = 0; i < foodGOs.Length; i++)
            srcArr.GetArrayElementAtIndex(i).objectReferenceValue = foodGOs[i];
        mgrSO.ApplyModifiedPropertiesWithoutUndo();

        // ── EventSystem ────────────────────────────────────────────────────────
        var esGO = SceneBuilderUtils.MakeGO("EventSystem");
        esGO.AddComponent<EventSystem>();
        esGO.AddComponent<StandaloneInputModule>();

        // ── Canvas ─────────────────────────────────────────────────────────────
        var canvasGO = new GameObject("Canvas");
        var canvas   = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGO.AddComponent<GraphicRaycaster>();
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight  = 0.5f;

        // ── Right panel ────────────────────────────────────────────────────────
        var panel  = SceneBuilderUtils.MakeRightPanel(canvasGO.transform, 200f);
        var panelT = panel.transform;

        SceneBuilderUtils.MakeLabel(panelT, "Title", "ANT COLONY", fontSize: 16);
        SceneBuilderUtils.MakeSeparator(panelT, "Simulation");

        var playBtn  = SceneBuilderUtils.MakeButton(panelT, "PlayPauseBtn", "Play");
        var resetBtn = SceneBuilderUtils.MakeButton(panelT, "ResetBtn",     "Reset");

        SceneBuilderUtils.MakeSeparator(panelT, "Stats");

        var infoLbl = SceneBuilderUtils.MakeLabel(panelT, "InfoLbl",
            "Ants:  120\nFood:  0", fontSize: 14, align: TextAlignmentOptions.Left);
        infoLbl.gameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 42);

        SceneBuilderUtils.MakeSeparator(panelT, "Pheromone tuning");

        var evapLbl    = SceneBuilderUtils.MakeLabel(panelT, "EvapLbl",  "Evaporate: 0.12",
            fontSize: 12, align: TextAlignmentOptions.Left);
        var evapSlider = SceneBuilderUtils.MakeSlider(panelT, "EvapSlider", 0.01f, 0.5f, 0.12f);

        var depLbl    = SceneBuilderUtils.MakeLabel(panelT, "DepLbl",   "Deposit: 0.08",
            fontSize: 12, align: TextAlignmentOptions.Left);
        var depSlider = SceneBuilderUtils.MakeSlider(panelT, "DepSlider", 0.01f, 0.3f, 0.08f);

        SceneBuilderUtils.MakeSeparator(panelT, "Legend");
        var legendLbl = SceneBuilderUtils.MakeLabel(panelT, "Legend",
            "Orange = food trail\nCyan   = nest trail\nYellow = searching\nGreen  = carrying",
            fontSize: 11, align: TextAlignmentOptions.Left);
        legendLbl.gameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 58);

        // ── AntColonyUI ────────────────────────────────────────────────────────
        var uiGO = new GameObject("AntColonyUI");
        var ui   = uiGO.AddComponent<AntColonyUI>();
        SceneBuilderUtils.SetRef(ui, "manager",        manager);
        SceneBuilderUtils.SetRef(ui, "settings",       settings);
        SceneBuilderUtils.SetRef(ui, "simController",  sc);
        SceneBuilderUtils.SetRef(ui, "playPauseButton",playBtn);
        SceneBuilderUtils.SetRef(ui, "resetButton",    resetBtn);
        SceneBuilderUtils.SetRef(ui, "infoText",       infoLbl);
        SceneBuilderUtils.SetRef(ui, "evaporateSlider",evapSlider);
        SceneBuilderUtils.SetRef(ui, "depositSlider",  depSlider);

        var evapLblTMP = panelT.Find("EvapLbl")?.GetComponent<TextMeshProUGUI>();
        var depLblTMP  = panelT.Find("DepLbl")?.GetComponent<TextMeshProUGUI>();
        if (evapLblTMP != null) SceneBuilderUtils.SetRef(ui, "evaporateLabel", evapLblTMP);
        if (depLblTMP  != null) SceneBuilderUtils.SetRef(ui, "depositLabel",   depLblTMP);

        // ── Save ────────────────────────────────────────────────────────────────
        EnsureDir("Assets/_Project/Scenes");
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.Refresh();
        Debug.Log("[AntColonySceneBuilder] 05_AntColony.unity built and saved.");
    }

    // ── Utilities ─────────────────────────────────────────────────────────────

    static void EnsureDir(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        var parts  = path.Split('/');
        var parent = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            var next = parent + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(parent, parts[i]);
            parent = next;
        }
    }

    static void SetFloat(SerializedObject so, string f, float v)
    {
        var p = so.FindProperty(f);
        if (p != null) p.floatValue = v;
        else Debug.LogWarning($"[Builder] float '{f}' not found");
    }

    static void SetInt(SerializedObject so, string f, int v)
    {
        var p = so.FindProperty(f);
        if (p != null) p.intValue = v;
        else Debug.LogWarning($"[Builder] int '{f}' not found");
    }

    static void SetIntOnObject(Object target, string field, int value)
    {
        var so = new SerializedObject(target);
        var p  = so.FindProperty(field);
        if (p != null) { p.intValue = value; so.ApplyModifiedPropertiesWithoutUndo(); }
    }
}
#endif
