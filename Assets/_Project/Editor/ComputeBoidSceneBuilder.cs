#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Rendering.Universal;
using TMPro;
using BehaviorSimulation.Core;
using BehaviorSimulation.ComputeShaders;

public static class ComputeBoidSceneBuilder
{
    const string ScenePath      = "Assets/_Project/Scenes/09_ComputeShaders.unity";
    const string SettingsPath   = "Assets/_Project/ScriptableObjects/ComputeBoidSettings.asset";
    const string ComputePath    = "Assets/_Project/Shaders/BoidComputeShader.compute";
    const string ShaderPath     = "Assets/_Project/Shaders/BoidGPU.shader";
    const string MaterialPath   = "Assets/_Project/Materials/BoidGPU.mat";
    const float  HalfW = 19f, HalfH = 11f;

    [MenuItem("Behavior Simulation/Build Scene — 09 Compute Shaders")]
    public static void Build()
    {
        // ── Validate shader assets exist ───────────────────────────────────────
        var computeShader = AssetDatabase.LoadAssetAtPath<ComputeShader>(ComputePath);
        if (computeShader == null)
        {
            Debug.LogError($"[ComputeBoidBuilder] Missing compute shader at {ComputePath}. Save it first.");
            return;
        }

        // ── Create / recreate material ─────────────────────────────────────────
        EnsureDir("Assets/_Project/Materials");
        var shader = Shader.Find("BehaviorSim/BoidGPU");
        if (shader == null)
        {
            Debug.LogError($"[ComputeBoidBuilder] Shader 'BehaviorSim/BoidGPU' not found. " +
                           $"Check {ShaderPath} compiled without errors.");
            return;
        }
        // Always recreate so shader tag changes are picked up
        if (AssetDatabase.LoadAssetAtPath<Material>(MaterialPath) != null)
            AssetDatabase.DeleteAsset(MaterialPath);
        var mat = new Material(shader);
        mat.color = new Color(0.35f, 0.90f, 0.55f, 1f);
        mat.SetFloat("_Scale", 0.35f);
        AssetDatabase.CreateAsset(mat, MaterialPath);
        AssetDatabase.SaveAssets();

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // ── Camera ─────────────────────────────────────────────────────────────
        var camGO = new GameObject("Main Camera");
        camGO.tag = "MainCamera";
        var cam   = camGO.AddComponent<Camera>();
        cam.orthographic       = true;
        cam.orthographicSize   = 13f;
        cam.transform.position = new Vector3(0f, 0f, -10f);
        cam.backgroundColor    = new Color(0.03f, 0.03f, 0.06f);
        cam.clearFlags         = CameraClearFlags.SolidColor;
        camGO.AddComponent<UniversalAdditionalCameraData>();
        camGO.AddComponent<CameraController2D>();

        // ── Settings SO ────────────────────────────────────────────────────────
        EnsureDir("Assets/_Project/ScriptableObjects");
        var settings = AssetDatabase.LoadAssetAtPath<ComputeBoidSettings>(SettingsPath);
        if (settings == null)
        {
            settings = ScriptableObject.CreateInstance<ComputeBoidSettings>();
            AssetDatabase.CreateAsset(settings, SettingsPath);
        }
        var sso = new SerializedObject(settings);
        SetFloat(sso, "boundsHalfW",      HalfW);
        SetFloat(sso, "boundsHalfH",      HalfH);
        SetInt  (sso, "agentCount",       3000);
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
            new Color(0.25f, 0.25f, 0.35f), z: 0.1f, lineWidth: 0.12f);

        // ── ComputeBoidManager ─────────────────────────────────────────────────
        var mgrGO   = new GameObject("ComputeBoidManager");
        var manager = mgrGO.AddComponent<ComputeBoidManager>();
        SceneBuilderUtils.SetRef(manager, "computeShader",    computeShader);
        SceneBuilderUtils.SetRef(manager, "instanceMaterial", mat);
        SceneBuilderUtils.SetRef(manager, "settings",         settings);

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

        SceneBuilderUtils.MakeLabel(panelT, "Title", "COMPUTE SHADERS", fontSize: 16);
        SceneBuilderUtils.MakeSeparator(panelT, "Simulation");

        var playBtn  = SceneBuilderUtils.MakeButton(panelT, "PlayPauseBtn", "Play");
        var stepBtn  = SceneBuilderUtils.MakeButton(panelT, "StepBtn",      "Step");
        var resetBtn = SceneBuilderUtils.MakeButton(panelT, "ResetBtn",     "Reset");

        SceneBuilderUtils.MakeSeparator(panelT, "Boids");

        var countLbl    = SceneBuilderUtils.MakeLabel(panelT, "CountLbl", "Boids: 3,000",
            fontSize: 12, align: TextAlignmentOptions.Left);
        var countSlider = SceneBuilderUtils.MakeSlider(panelT, "CountSlider", 100f, 10000f, 3000f);

        SceneBuilderUtils.MakeSeparator(panelT, "GPU Stats");

        var statsLbl = SceneBuilderUtils.MakeLabel(panelT, "StatsText",
            "FPS:     --\nTick:    -- ms\nBoids:   --\nThreads: --",
            fontSize: 12, align: TextAlignmentOptions.Left);
        statsLbl.color = new Color(0.60f, 0.85f, 1.00f);
        statsLbl.gameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 72);

        SceneBuilderUtils.MakeSeparator(panelT, "Pipeline");
        var pipeLbl = SceneBuilderUtils.MakeLabel(panelT, "PipeInfo",
            "Physics:  GPU (compute)\nRender:   1 draw call\nCPU load: ~0%",
            fontSize: 11, align: TextAlignmentOptions.Left);
        pipeLbl.color = new Color(0.50f, 0.50f, 0.55f);
        pipeLbl.gameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 52);

        // ── Play label ref ─────────────────────────────────────────────────────
        var playLblTMP = playBtn.transform.Find("Text")?.GetComponent<TextMeshProUGUI>();

        // ── ComputeBoidUI ──────────────────────────────────────────────────────
        var uiGO = new GameObject("ComputeBoidUI");
        var ui   = uiGO.AddComponent<ComputeBoidUI>();
        SceneBuilderUtils.SetRef(ui, "manager",         manager);
        SceneBuilderUtils.SetRef(ui, "simController",   sc);
        SceneBuilderUtils.SetRef(ui, "playPauseButton", playBtn);
        SceneBuilderUtils.SetRef(ui, "stepButton",      stepBtn);
        SceneBuilderUtils.SetRef(ui, "resetButton",     resetBtn);
        SceneBuilderUtils.SetRef(ui, "countSlider",     countSlider);
        SceneBuilderUtils.SetRef(ui, "statsText",       statsLbl);
        if (playLblTMP != null)
            SceneBuilderUtils.SetRef(ui, "playPauseLabel", playLblTMP);
        if (countLbl != null)
            SceneBuilderUtils.SetRef(ui, "countLabel", countLbl);

        // ── Save ────────────────────────────────────────────────────────────────
        EnsureDir("Assets/_Project/Scenes");
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.Refresh();
        Debug.Log("[ComputeBoidSceneBuilder] 09_ComputeShaders.unity built and saved.");
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
        else Debug.LogWarning($"[ComputeBoidBuilder] float '{f}' not found");
    }

    static void SetInt(SerializedObject so, string f, int v)
    {
        var p = so.FindProperty(f);
        if (p != null) p.intValue = v;
        else Debug.LogWarning($"[ComputeBoidBuilder] int '{f}' not found");
    }
}
#endif
