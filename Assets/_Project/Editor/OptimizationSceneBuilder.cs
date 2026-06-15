#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Rendering.Universal;
using TMPro;
using BehaviorSimulation.Core;
using BehaviorSimulation.Optimization;

public static class OptimizationSceneBuilder
{
    const string ScenePath    = "Assets/_Project/Scenes/08_Optimization.unity";
    const string SettingsPath = "Assets/_Project/ScriptableObjects/OptimizationSettings.asset";
    const float  HalfW = 19f, HalfH = 11f;

    [MenuItem("Behavior Simulation/Build Scene — 08 Optimization")]
    public static void Build()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // ── Camera ─────────────────────────────────────────────────────────────
        var camGO = new GameObject("Main Camera");
        camGO.tag = "MainCamera";
        var cam   = camGO.AddComponent<Camera>();
        cam.orthographic       = true;
        cam.orthographicSize   = 13f;
        cam.transform.position = new Vector3(0f, 0f, -10f);
        cam.backgroundColor    = new Color(0.04f, 0.04f, 0.08f);
        cam.clearFlags         = CameraClearFlags.SolidColor;
        camGO.AddComponent<UniversalAdditionalCameraData>();
        camGO.AddComponent<CameraController2D>();

        // ── Settings SO ────────────────────────────────────────────────────────
        EnsureDir("Assets/_Project/ScriptableObjects");
        var settings = AssetDatabase.LoadAssetAtPath<OptimizationSettings>(SettingsPath);
        if (settings == null)
        {
            settings = ScriptableObject.CreateInstance<OptimizationSettings>();
            AssetDatabase.CreateAsset(settings, SettingsPath);
        }
        var sso = new SerializedObject(settings);
        SetFloat(sso, "boundsHalfW",      HalfW);
        SetFloat(sso, "boundsHalfH",      HalfH);
        SetInt  (sso, "agentCount",       300);
        SetFloat(sso, "maxSpeed",         5f);
        SetFloat(sso, "maxForce",         12f);
        SetFloat(sso, "perceptionRadius", 3f);
        SetFloat(sso, "separationRadius", 1.2f);
        SetFloat(sso, "separationWeight", 1.8f);
        SetFloat(sso, "alignmentWeight",  1.0f);
        SetFloat(sso, "cohesionWeight",   1.0f);
        sso.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.SaveAssets();

        // ── SimulationController ───────────────────────────────────────────────
        var scGO = new GameObject("SimulationController");
        var sc   = scGO.AddComponent<SimulationController>();

        // ── World border ───────────────────────────────────────────────────────
        SceneBuilderUtils.MakeWorldBorder(HalfW, HalfH,
            new Color(0.28f, 0.28f, 0.38f), z: 0.1f, lineWidth: 0.12f);

        // ── GridOverlay ────────────────────────────────────────────────────────
        var overlayGO = new GameObject("GridOverlay");
        overlayGO.transform.position = new Vector3(0f, 0f, 0.5f);
        var overlay = overlayGO.AddComponent<GridOverlay>();

        // ── OptimizationManager ────────────────────────────────────────────────
        var mgrGO   = new GameObject("OptimizationManager");
        var manager = mgrGO.AddComponent<OptimizationManager>();
        SceneBuilderUtils.SetRef(manager, "settings",    settings);
        SceneBuilderUtils.SetRef(manager, "gridOverlay", overlay);

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
        var panel  = SceneBuilderUtils.MakeRightPanel(canvasGO.transform, 215f);
        var panelT = panel.transform;

        SceneBuilderUtils.MakeLabel(panelT, "Title", "BOIDS  OPTIMIZATION", fontSize: 15);
        SceneBuilderUtils.MakeSeparator(panelT, "Simulation");

        var playBtn  = SceneBuilderUtils.MakeButton(panelT, "PlayPauseBtn", "Play");
        var stepBtn  = SceneBuilderUtils.MakeButton(panelT, "StepBtn",      "Step");
        var resetBtn = SceneBuilderUtils.MakeButton(panelT, "ResetBtn",     "Reset");

        SceneBuilderUtils.MakeSeparator(panelT, "Algorithm");

        var modeBtn = SceneBuilderUtils.MakeButton(panelT, "ModeBtn", "Brute Force  O(n²)");
        var modeLbl = modeBtn.transform.Find("Text")?.GetComponent<TextMeshProUGUI>();

        SceneBuilderUtils.MakeSeparator(panelT, "Boids");

        var countLbl    = SceneBuilderUtils.MakeLabel(panelT, "CountLbl", "Count: 300",
            fontSize: 12, align: TextAlignmentOptions.Left);
        var countSlider = SceneBuilderUtils.MakeSlider(panelT, "CountSlider", 50f, 1000f, 300f);

        SceneBuilderUtils.MakeSeparator(panelT, "Visualization");

        var gridBtn = SceneBuilderUtils.MakeButton(panelT, "GridBtn", "Show Grid");

        SceneBuilderUtils.MakeSeparator(panelT, "Stats");

        var statsLbl = SceneBuilderUtils.MakeLabel(panelT, "StatsText",
            "FPS:    --\nTick:   -- ms\nBoids:  --\nMode:   --\nCells:  --",
            fontSize: 12, align: TextAlignmentOptions.Left);
        statsLbl.color = new Color(0.65f, 0.95f, 0.65f);
        statsLbl.gameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 80);

        SceneBuilderUtils.MakeSeparator(panelT, "Hint");
        var hint = SceneBuilderUtils.MakeLabel(panelT, "Hint",
            "Scale to 800+ boids\nto see Tick ms diverge",
            fontSize: 11, align: TextAlignmentOptions.Left);
        hint.color = new Color(0.55f, 0.55f, 0.55f);
        hint.gameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 32);

        // ── Play label ref ─────────────────────────────────────────────────────
        var playLblTMP = playBtn.transform.Find("Text")?.GetComponent<TextMeshProUGUI>();

        // ── OptimizationUI ─────────────────────────────────────────────────────
        var uiGO = new GameObject("OptimizationUI");
        var ui   = uiGO.AddComponent<OptimizationUI>();
        SceneBuilderUtils.SetRef(ui, "manager",           manager);
        SceneBuilderUtils.SetRef(ui, "simController",     sc);
        SceneBuilderUtils.SetRef(ui, "playPauseButton",   playBtn);
        SceneBuilderUtils.SetRef(ui, "stepButton",        stepBtn);
        SceneBuilderUtils.SetRef(ui, "resetButton",       resetBtn);
        SceneBuilderUtils.SetRef(ui, "modeButton",        modeBtn);
        SceneBuilderUtils.SetRef(ui, "countSlider",       countSlider);
        SceneBuilderUtils.SetRef(ui, "gridOverlayButton", gridBtn);
        SceneBuilderUtils.SetRef(ui, "statsText",         statsLbl);
        if (playLblTMP != null)
            SceneBuilderUtils.SetRef(ui, "playPauseLabel", playLblTMP);
        if (modeLbl != null)
            SceneBuilderUtils.SetRef(ui, "modeBtnLabel", modeLbl);
        if (countLbl != null)
            SceneBuilderUtils.SetRef(ui, "countLabel",  countLbl);

        // ── Save ────────────────────────────────────────────────────────────────
        EnsureDir("Assets/_Project/Scenes");
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.Refresh();
        Debug.Log("[OptimizationSceneBuilder] 08_Optimization.unity built and saved.");
    }

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
        else Debug.LogWarning($"[OptBuilder] float '{f}' not found");
    }

    static void SetInt(SerializedObject so, string f, int v)
    {
        var p = so.FindProperty(f);
        if (p != null) p.intValue = v;
        else Debug.LogWarning($"[OptBuilder] int '{f}' not found");
    }
}
#endif
