#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Rendering.Universal;
using TMPro;
using BehaviorSimulation.Core;
using BehaviorSimulation.Boids;

public static class BoidsSceneBuilder
{
    const string ScenePath   = "Assets/_Project/Scenes/03_Boids2D.unity";
    const string SettingsPath = "Assets/_Project/ScriptableObjects/BoidSettings.asset";
    const float  HalfW = 19f, HalfH = 11f;

    [MenuItem("Behavior Simulation/Build Scene — 03 Boids 2D")]
    public static void Build()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // ── Camera ─────────────────────────────────────────────────────────────
        var camGO = new GameObject("Main Camera");
        camGO.tag = "MainCamera";
        var cam = camGO.AddComponent<Camera>();
        cam.orthographic     = true;
        cam.orthographicSize = 13f;
        cam.transform.position = new Vector3(0f, 0f, -10f);
        cam.backgroundColor  = new Color(0.05f, 0.05f, 0.08f);
        cam.clearFlags       = CameraClearFlags.SolidColor;
        var urcaCam = camGO.AddComponent<UniversalAdditionalCameraData>();

        var camCtrl = camGO.AddComponent<CameraController2D>();

        // ── BoidSettings ScriptableObject ──────────────────────────────────────
        EnsureDir("Assets/_Project/ScriptableObjects");
        var settings = AssetDatabase.LoadAssetAtPath<BoidSettings>(SettingsPath);
        if (settings == null)
        {
            settings = ScriptableObject.CreateInstance<BoidSettings>();
            AssetDatabase.CreateAsset(settings, SettingsPath);
        }

        // Always reset to spec defaults so stale assets don't carry wrong values.
        var settingsSO = new SerializedObject(settings);
        settingsSO.FindProperty("separationWeight").floatValue = 1.8f;
        settingsSO.FindProperty("alignmentWeight").floatValue  = 1.0f;
        settingsSO.FindProperty("cohesionWeight").floatValue   = 1.0f;
        settingsSO.FindProperty("maxSpeed").floatValue         = 6f;
        settingsSO.FindProperty("maxForce").floatValue         = 14f;
        settingsSO.FindProperty("perceptionRadius").floatValue = 3f;
        settingsSO.FindProperty("separationRadius").floatValue = 1.2f;
        settingsSO.FindProperty("boundsHalfW").floatValue      = HalfW;
        settingsSO.FindProperty("boundsHalfH").floatValue      = HalfH;
        settingsSO.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.SaveAssets();

        // ── SimulationController ───────────────────────────────────────────────
        var scGO = new GameObject("SimulationController");
        var sc   = scGO.AddComponent<SimulationController>();

        // ── World border ───────────────────────────────────────────────────────
        SceneBuilderUtils.MakeWorldBorder(HalfW, HalfH,
            new Color(0.3f, 0.3f, 0.4f), z: 0.1f, lineWidth: 0.12f);

        // ── BoidManager ────────────────────────────────────────────────────────
        var mgrGO   = new GameObject("BoidManager");
        var manager = mgrGO.AddComponent<BoidManager>();
        SceneBuilderUtils.SetRef(manager, "settings",    settings);
        SceneBuilderUtils.SetRef(manager, "startCount",  null);   // int, skip
        SetInt(manager, "startCount", 80);

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

        // ── Info box (top-left) ────────────────────────────────────────────────
        var infoTMP = SceneBuilderUtils.MakeInfoBox(canvasGO.transform,
            "Boids: 0\nFPS:   0",
            size:   new Vector2(160, 56),
            offset: new Vector2(10, -10));

        // ── Right control panel ────────────────────────────────────────────────
        var panel     = SceneBuilderUtils.MakeRightPanel(canvasGO.transform, 200f);
        var panelT    = panel.transform;

        SceneBuilderUtils.MakeLabel(panelT, "Title", "BOIDS 2D", fontSize: 16);
        SceneBuilderUtils.MakeSeparator(panelT, "Simulation");

        var playBtn  = SceneBuilderUtils.MakeButton(panelT, "PlayPauseBtn",  "Play");
        var resetBtn = SceneBuilderUtils.MakeButton(panelT, "ResetBtn",      "Reset");

        SceneBuilderUtils.MakeSeparator(panelT, "Spawn");

        var spawn10Btn  = SceneBuilderUtils.MakeButton(panelT, "Spawn10Btn",  "Spawn 10");
        var spawn50Btn  = SceneBuilderUtils.MakeButton(panelT, "Spawn50Btn",  "Spawn 50");
        var spawn100Btn = SceneBuilderUtils.MakeButton(panelT, "Spawn100Btn", "Spawn 100");
        var spawn300Btn = SceneBuilderUtils.MakeButton(panelT, "Spawn300Btn", "Spawn 300");

        SceneBuilderUtils.MakeSeparator(panelT, "Weights  (Sep / Ali / Coh)");

        SceneBuilderUtils.MakeLabel(panelT, "SepLbl",  "Sep: 1.8", fontSize: 13,
            align: TextAlignmentOptions.Left);
        var sepSlider = SceneBuilderUtils.MakeSlider(panelT, "SepSlider", 0f, 5f, 1.8f);

        SceneBuilderUtils.MakeLabel(panelT, "AliLbl",  "Ali: 1.0", fontSize: 13,
            align: TextAlignmentOptions.Left);
        var aliSlider = SceneBuilderUtils.MakeSlider(panelT, "AliSlider", 0f, 5f, 1.0f);

        SceneBuilderUtils.MakeLabel(panelT, "CohLbl",  "Coh: 1.0", fontSize: 13,
            align: TextAlignmentOptions.Left);
        var cohSlider = SceneBuilderUtils.MakeSlider(panelT, "CohSlider", 0f, 5f, 1.0f);

        // ── BoidsUI ────────────────────────────────────────────────────────────
        var uiGO = new GameObject("BoidsUI");
        var ui   = uiGO.AddComponent<BoidsUI>();

        SceneBuilderUtils.SetRef(ui, "manager",         manager);
        SceneBuilderUtils.SetRef(ui, "settings",        settings);
        SceneBuilderUtils.SetRef(ui, "simController",   sc);
        SceneBuilderUtils.SetRef(ui, "playPauseButton", playBtn);
        SceneBuilderUtils.SetRef(ui, "resetButton",     resetBtn);
        SceneBuilderUtils.SetRef(ui, "spawn10Btn",      spawn10Btn);
        SceneBuilderUtils.SetRef(ui, "spawn50Btn",      spawn50Btn);
        SceneBuilderUtils.SetRef(ui, "spawn100Btn",     spawn100Btn);
        SceneBuilderUtils.SetRef(ui, "spawn300Btn",     spawn300Btn);
        SceneBuilderUtils.SetRef(ui, "separationSlider",sepSlider);
        SceneBuilderUtils.SetRef(ui, "alignmentSlider", aliSlider);
        SceneBuilderUtils.SetRef(ui, "cohesionSlider",  cohSlider);
        SceneBuilderUtils.SetRef(ui, "infoText",        infoTMP);

        // Wire the three weight labels so BoidsUI can update them
        // The labels were created by MakeLabel above — find by parent + name
        var sepLbl  = panelT.Find("SepLbl")?.GetComponent<TextMeshProUGUI>();
        var aliLbl  = panelT.Find("AliLbl")?.GetComponent<TextMeshProUGUI>();
        var cohLbl  = panelT.Find("CohLbl")?.GetComponent<TextMeshProUGUI>();
        if (sepLbl != null) SceneBuilderUtils.SetRef(ui, "separationLabel", sepLbl);
        if (aliLbl != null) SceneBuilderUtils.SetRef(ui, "alignmentLabel",  aliLbl);
        if (cohLbl != null) SceneBuilderUtils.SetRef(ui, "cohesionLabel",   cohLbl);

        // ── Save scene ─────────────────────────────────────────────────────────
        EnsureDir("Assets/_Project/Scenes");
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.Refresh();

        Debug.Log("[BoidsSceneBuilder] 03_Boids2D.unity built and saved.");
    }

    // ── Utilities ──────────────────────────────────────────────────────────────

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

    // SerializedObject can't set int fields via SetRef (expects Object), so handle separately.
    static void SetInt(Object target, string field, int value)
    {
        var so   = new SerializedObject(target);
        var prop = so.FindProperty(field);
        if (prop != null) { prop.intValue = value; so.ApplyModifiedPropertiesWithoutUndo(); }
    }
}
#endif
