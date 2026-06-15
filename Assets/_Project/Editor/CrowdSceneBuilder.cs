#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Rendering.Universal;
using TMPro;
using BehaviorSimulation.Core;
using BehaviorSimulation.Crowd;

public static class CrowdSceneBuilder
{
    const string ScenePath    = "Assets/_Project/Scenes/06_Crowd.unity";
    const string SettingsPath = "Assets/_Project/ScriptableObjects/CrowdSettings.asset";
    const float  HalfW = 19f, HalfH = 11f;

    [MenuItem("Behavior Simulation/Build Scene — 06 Crowd")]
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
        cam.backgroundColor  = new Color(0.06f, 0.06f, 0.10f);
        cam.clearFlags       = CameraClearFlags.SolidColor;
        camGO.AddComponent<UniversalAdditionalCameraData>();
        camGO.AddComponent<CameraController2D>();

        // ── Settings SO ────────────────────────────────────────────────────────
        EnsureDir("Assets/_Project/ScriptableObjects");
        var settings = AssetDatabase.LoadAssetAtPath<CrowdSettings>(SettingsPath);
        if (settings == null)
        {
            settings = ScriptableObject.CreateInstance<CrowdSettings>();
            AssetDatabase.CreateAsset(settings, SettingsPath);
        }
        var sso = new SerializedObject(settings);
        SetFloat(sso, "boundsHalfW",   HalfW);
        SetFloat(sso, "boundsHalfH",   HalfH);
        SetFloat(sso, "desiredSpeed",  0.8f);   // slow walk — orderly queue at this speed
        SetFloat(sso, "maxSpeed",      2.5f);   // panic (×2.5) → 2.0 WU/s, still under max
        SetFloat(sso, "tau",           0.5f);
        SetFloat(sso, "agentRadius",   0.25f);
        SetInt  (sso, "agentCount",    80);
        SetFloat(sso, "agentA",        10f);    // stronger social force → stable arches at panic
        SetFloat(sso, "agentB",        0.15f);  // tighter range → force concentrated at contact
        SetFloat(sso, "bodyForce",     50f);
        SetFloat(sso, "wallA",         12f);
        SetFloat(sso, "wallB",         0.10f);
        SetFloat(sso, "bottleneckX",   2f);
        SetFloat(sso, "bottleneckGap", 0.5f);   // 1.0 WU opening = ~2 body diameters
        sso.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.SaveAssets();

        // ── SimulationController ───────────────────────────────────────────────
        var scGO = new GameObject("SimulationController");
        var sc   = scGO.AddComponent<SimulationController>();

        // ── World border ───────────────────────────────────────────────────────
        SceneBuilderUtils.MakeWorldBorder(HalfW, HalfH,
            new Color(0.35f, 0.35f, 0.45f), z: 0.1f, lineWidth: 0.12f);

        // ── Bottleneck wall renderers (visual only; two segments around the gap) ──
        var wlrBot = MakeWallLine("BottleneckWallBot");
        var wlrTop = MakeWallLine("BottleneckWallTop");

        // ── CrowdManager ───────────────────────────────────────────────────────
        // ── Density grid ───────────────────────────────────────────────────────
        var densityGO   = new GameObject("DensityGrid");
        densityGO.transform.position = new Vector3(0f, 0f, 0.8f);  // behind agents
        var densityGrid = densityGO.AddComponent<CrowdDensityGrid>();

        // ── CrowdManager ───────────────────────────────────────────────────────
        var mgrGO   = new GameObject("CrowdManager");
        var manager = mgrGO.AddComponent<CrowdManager>();
        SceneBuilderUtils.SetRef(manager, "settings",        settings);
        SceneBuilderUtils.SetRef(manager, "densityGrid",     densityGrid);
        SceneBuilderUtils.SetRef(manager, "wallRendererBot", wlrBot);
        SceneBuilderUtils.SetRef(manager, "wallRendererTop", wlrTop);

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
        var panel  = SceneBuilderUtils.MakeRightPanel(canvasGO.transform, 210f);
        var panelT = panel.transform;

        SceneBuilderUtils.MakeLabel(panelT, "Title", "CROWD SIMULATION", fontSize: 16);
        SceneBuilderUtils.MakeSeparator(panelT, "Simulation");

        var playBtn  = SceneBuilderUtils.MakeButton(panelT, "PlayPauseBtn", "Play");
        var resetBtn = SceneBuilderUtils.MakeButton(panelT, "ResetBtn",     "Reset");

        SceneBuilderUtils.MakeSeparator(panelT, "Scenario");

        var bidirBtn = SceneBuilderUtils.MakeButton(panelT, "BidirBtn",  "Bidirectional");
        var botBtn   = SceneBuilderUtils.MakeButton(panelT, "BotBtn",    "Bottleneck");
        var crossBtn = SceneBuilderUtils.MakeButton(panelT, "CrossBtn",  "Crossflow");

        // Highlight the default
        var bidirImg = bidirBtn.GetComponent<Image>();
        if (bidirImg) bidirImg.color = new Color(0.35f, 0.70f, 0.35f);

        var scenarioLbl = SceneBuilderUtils.MakeLabel(panelT, "ScenarioLbl",
            "Bidirectional", fontSize: 13, align: TextAlignmentOptions.Center);

        SceneBuilderUtils.MakeSeparator(panelT, "Parameters");

        var speedLbl    = SceneBuilderUtils.MakeLabel(panelT, "SpeedLbl",  "Speed: 1.4",
            fontSize: 12, align: TextAlignmentOptions.Left);
        var speedSlider = SceneBuilderUtils.MakeSlider(panelT, "SpeedSlider", 0.3f, 2.0f, 0.8f);

        var countLbl    = SceneBuilderUtils.MakeLabel(panelT, "CountLbl",  "Agents: 80",
            fontSize: 12, align: TextAlignmentOptions.Left);
        var countSlider = SceneBuilderUtils.MakeSlider(panelT, "CountSlider", 20f, 150f, 80f);

        SceneBuilderUtils.MakeSeparator(panelT, "Expansion");

        var panicBtn   = SceneBuilderUtils.MakeButton(panelT, "PanicBtn",   "Panic!");
        var densityBtn = SceneBuilderUtils.MakeButton(panelT, "DensityBtn", "Show Density");

        var throughputLbl = SceneBuilderUtils.MakeLabel(panelT, "ThroughputLbl", "Flow: -- ag/s",
            fontSize: 13, align: TextAlignmentOptions.Left);
        throughputLbl.gameObject.SetActive(false);   // only visible in Bottleneck

        SceneBuilderUtils.MakeSeparator(panelT, "Legend");
        var legendLbl = SceneBuilderUtils.MakeLabel(panelT, "Legend",
            "Blue   = Group A\nOrange = Group B",
            fontSize: 11, align: TextAlignmentOptions.Left);
        legendLbl.gameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 36);

        // ── Play button text ref ───────────────────────────────────────────────
        var playLblTMP = playBtn.transform.Find("Text")?.GetComponent<TextMeshProUGUI>();

        // ── CrowdUI ────────────────────────────────────────────────────────────
        var uiGO = new GameObject("CrowdUI");
        var ui   = uiGO.AddComponent<CrowdUI>();
        SceneBuilderUtils.SetRef(ui, "manager",           manager);
        SceneBuilderUtils.SetRef(ui, "settings",          settings);
        SceneBuilderUtils.SetRef(ui, "simController",     sc);
        SceneBuilderUtils.SetRef(ui, "playPauseButton",   playBtn);
        SceneBuilderUtils.SetRef(ui, "resetButton",       resetBtn);
        SceneBuilderUtils.SetRef(ui, "scenarioBidirBtn",  bidirBtn);
        SceneBuilderUtils.SetRef(ui, "scenarioBotBtn",    botBtn);
        SceneBuilderUtils.SetRef(ui, "scenarioCrossBtn",  crossBtn);
        SceneBuilderUtils.SetRef(ui, "speedSlider",     speedSlider);
        SceneBuilderUtils.SetRef(ui, "countSlider",     countSlider);
        if (playLblTMP != null)
            SceneBuilderUtils.SetRef(ui, "playPauseLabel", playLblTMP);
        SceneBuilderUtils.SetRef(ui, "scenarioTitle",   scenarioLbl);
        if (speedLbl != null)
            SceneBuilderUtils.SetRef(ui, "speedLabel",  speedLbl);
        if (countLbl != null)
            SceneBuilderUtils.SetRef(ui, "countLabel",  countLbl);
        SceneBuilderUtils.SetRef(ui, "panicButton",     panicBtn);
        SceneBuilderUtils.SetRef(ui, "densityButton",   densityBtn);
        SceneBuilderUtils.SetRef(ui, "throughputText",  throughputLbl);

        // ── Save ────────────────────────────────────────────────────────────────
        EnsureDir("Assets/_Project/Scenes");
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.Refresh();
        Debug.Log("[CrowdSceneBuilder] 06_Crowd.unity built and saved.");
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
        else Debug.LogWarning($"[CrowdBuilder] float '{f}' not found");
    }

    static void SetInt(SerializedObject so, string f, int v)
    {
        var p = so.FindProperty(f);
        if (p != null) p.intValue = v;
        else Debug.LogWarning($"[CrowdBuilder] int '{f}' not found");
    }

    static LineRenderer MakeWallLine(string name)
    {
        var go  = new GameObject(name);
        var lr  = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.positionCount = 2;
        lr.startWidth    = 0.14f;
        lr.endWidth      = 0.14f;
        lr.material      = new Material(Shader.Find("Sprites/Default"));
        lr.startColor    = new Color(0.85f, 0.75f, 0.30f, 0.85f);
        lr.endColor      = new Color(0.85f, 0.75f, 0.30f, 0.85f);
        lr.sortingOrder  = 1;
        lr.enabled       = false;
        return lr;
    }
}
#endif
