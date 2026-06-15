#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Rendering.Universal;
using TMPro;
using BehaviorSimulation.Core;
using BehaviorSimulation.Ecosystem;

public static class EcosystemSceneBuilder
{
    const string ScenePath    = "Assets/_Project/Scenes/04_PredatorPrey.unity";
    const string SettingsPath = "Assets/_Project/ScriptableObjects/PredatorPreySettings.asset";
    const float  HalfW = 19f, HalfH = 11f;

    [MenuItem("Behavior Simulation/Build Scene — 04 Predator Prey")]
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

        // ── Settings ScriptableObject ──────────────────────────────────────────
        EnsureDir("Assets/_Project/ScriptableObjects");
        var settings = AssetDatabase.LoadAssetAtPath<PredatorPreySettings>(SettingsPath);
        if (settings == null)
        {
            settings = ScriptableObject.CreateInstance<PredatorPreySettings>();
            AssetDatabase.CreateAsset(settings, SettingsPath);
        }

        var sso = new SerializedObject(settings);
        SetFloat(sso, "boundsHalfW",      HalfW);
        SetFloat(sso, "boundsHalfH",      HalfH);
        SetInt  (sso, "preyStart",          50);
        SetInt  (sso, "preyMax",           200);
        SetFloat(sso, "preySpeed",          4f);
        SetFloat(sso, "preyMaxForce",      10f);
        SetFloat(sso, "preyFleeSpeedMult",  1.6f);
        SetFloat(sso, "preyEnergyStart",   12f);
        SetFloat(sso, "preyEnergyDecay",    0.3f);
        SetFloat(sso, "preyFleeRadius",     5f);
        SetFloat(sso, "preyEatRadius",      0.7f);
        SetFloat(sso, "preyReproduceE",    12f);
        SetFloat(sso, "preyReproduceCool",  4f);
        SetInt  (sso, "predStart",           8);
        SetInt  (sso, "predMax",            60);
        SetFloat(sso, "predSpeed",           5f);
        SetFloat(sso, "predMaxForce",       12f);
        SetFloat(sso, "predHuntRadius",     12f);
        SetFloat(sso, "predEnergyStart",    15f);
        SetFloat(sso, "predEnergyDecay",     0.6f);
        SetFloat(sso, "predEatRadius",       1.0f);
        SetFloat(sso, "predEatGain",         8f);
        SetFloat(sso, "predReproduceE",     26f);
        SetFloat(sso, "predReproduceCool",  12f);
        SetInt  (sso, "foodCount",          80);
        SetFloat(sso, "foodRegrowTime",      5f);
        SetFloat(sso, "foodEatGain",         6f);
        SetFloat(sso, "foodEatRadius",       0.7f);
        sso.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.SaveAssets();

        // ── SimulationController ───────────────────────────────────────────────
        var scGO = new GameObject("SimulationController");
        var sc   = scGO.AddComponent<SimulationController>();

        // ── World border ───────────────────────────────────────────────────────
        SceneBuilderUtils.MakeWorldBorder(HalfW, HalfH,
            new Color(0.3f, 0.3f, 0.4f), z: 0.1f, lineWidth: 0.12f);

        // ── EcosystemManager ───────────────────────────────────────────────────
        var mgrGO   = new GameObject("EcosystemManager");
        var manager = mgrGO.AddComponent<EcosystemManager>();
        SceneBuilderUtils.SetRef(manager, "settings", settings);

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

        SceneBuilderUtils.MakeLabel(panelT, "Title", "PREDATOR / PREY", fontSize: 15);
        SceneBuilderUtils.MakeSeparator(panelT, "Simulation");

        var playBtn  = SceneBuilderUtils.MakeButton(panelT, "PlayPauseBtn", "Play");
        var resetBtn = SceneBuilderUtils.MakeButton(panelT, "ResetBtn",     "Reset");

        SceneBuilderUtils.MakeSeparator(panelT, "Population");

        var preyLbl = SceneBuilderUtils.MakeLabel(panelT, "PreyLbl",  "Prey:  40",
            fontSize: 14, align: TextAlignmentOptions.Left);
        var predLbl = SceneBuilderUtils.MakeLabel(panelT, "PredLbl", "Pred:   8",
            fontSize: 14, align: TextAlignmentOptions.Left);

        SceneBuilderUtils.MakeSeparator(panelT, "Graph (green=prey  red=pred)");

        // Population graph (RawImage inside panel)
        var graphGO   = SceneBuilderUtils.MakeGO("PopGraph", panelT);
        var graphRect  = graphGO.AddComponent<RectTransform>();
        graphRect.sizeDelta = new Vector2(0, 64);
        var graphLE = graphGO.AddComponent<LayoutElement>();
        graphLE.preferredHeight = 64;
        var rawImg = graphGO.AddComponent<RawImage>();
        rawImg.color = Color.white;

        SceneBuilderUtils.MakeSeparator(panelT, "Legend");

        var legendGO   = SceneBuilderUtils.MakeLabel(panelT, "Legend",
            "Green circles = prey\nRed arrows = predators\nSmall dots = food",
            fontSize: 11, align: TextAlignmentOptions.Left);
        legendGO.gameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 48);

        // ── EcosystemUI ────────────────────────────────────────────────────────
        var uiGO = new GameObject("EcosystemUI");
        var ui   = uiGO.AddComponent<EcosystemUI>();

        SceneBuilderUtils.SetRef(ui, "manager",        manager);
        SceneBuilderUtils.SetRef(ui, "simController",  sc);
        SceneBuilderUtils.SetRef(ui, "playPauseButton",playBtn);
        SceneBuilderUtils.SetRef(ui, "resetButton",    resetBtn);
        SceneBuilderUtils.SetRef(ui, "preyCountText",  preyLbl);
        SceneBuilderUtils.SetRef(ui, "predCountText",  predLbl);
        SceneBuilderUtils.SetRef(ui, "graphImage",     rawImg);

        // ── Save ────────────────────────────────────────────────────────────────
        EnsureDir("Assets/_Project/Scenes");
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.Refresh();

        Debug.Log("[EcosystemSceneBuilder] 04_PredatorPrey.unity built and saved.");
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

    static void SetFloat(SerializedObject so, string field, float val)
    {
        var p = so.FindProperty(field);
        if (p != null) p.floatValue = val;
        else Debug.LogWarning($"[Builder] float field '{field}' not found");
    }

    static void SetInt(SerializedObject so, string field, int val)
    {
        var p = so.FindProperty(field);
        if (p != null) p.intValue = val;
        else Debug.LogWarning($"[Builder] int field '{field}' not found");
    }
}
#endif
