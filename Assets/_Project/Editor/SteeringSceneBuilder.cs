#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using BehaviorSimulation.Core;
using BehaviorSimulation.Steering;

public static class SteeringSceneBuilder
{
    private const string ScenePath = "Assets/_Project/Scenes/02_SteeringBehaviors.unity";

    // World bounds — agents wrap around at these edges.
    private const float HalfW = 19f;
    private const float HalfH = 11f;

    [MenuItem("Behavior Simulation/Build Scene — 02 Steering Behaviors")]
    public static void BuildScene()
    {
        Directory.CreateDirectory("Assets/_Project/Scenes");
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // ── SimulationController ─────────────────────────────────────────────
        var simControllerGO = SceneBuilderUtils.MakeGO("SimulationController");
        var simController   = simControllerGO.AddComponent<SimulationController>();

        // ── Main Camera ──────────────────────────────────────────────────────
        var cameraGO = SceneBuilderUtils.MakeGO("Main Camera");
        cameraGO.tag = "MainCamera";
        var cam = cameraGO.AddComponent<Camera>();
        cam.orthographic     = true;
        cam.orthographicSize = 12f;          // shows full HalfH=11 + 1 unit margin
        cam.clearFlags       = CameraClearFlags.SolidColor;
        cam.backgroundColor  = new Color(0.05f, 0.05f, 0.08f);
        cameraGO.transform.position = new Vector3(0f, 0f, -10f);
        cameraGO.AddComponent<AudioListener>();
        cameraGO.AddComponent<CameraController2D>();

        // ── Main agent (light blue arrow) ────────────────────────────────────
        var mainAgentGO  = SceneBuilderUtils.MakeGO("Agent");
        var mainAgentSR  = mainAgentGO.AddComponent<SpriteRenderer>();
        mainAgentSR.color = new Color(0.4f, 0.8f, 1f);   // light blue
        var mainAgent    = mainAgentGO.AddComponent<SteeringAgent>();
        SetBounds(mainAgent);

        // ── Mouse target marker (yellow ring) ────────────────────────────────
        var markerGO = SceneBuilderUtils.MakeGO("MouseTargetMarker");
        var markerSR = markerGO.AddComponent<SpriteRenderer>();
        markerSR.color = Color.yellow;
        // Ring sprite is assigned at runtime in MouseTargetMarker.Awake
        markerGO.AddComponent<MouseTargetMarker>();

        // ── Wandering target (orange arrow, for Pursuit/Evade) ───────────────
        var wanderGO  = SceneBuilderUtils.MakeGO("WanderingTarget");
        var wanderSR  = wanderGO.AddComponent<SpriteRenderer>();
        wanderSR.color = new Color(1f, 0.4f, 0.15f);     // orange
        var wanderAgent = wanderGO.AddComponent<SteeringAgent>();
        SetBounds(wanderAgent);
        wanderGO.transform.position = new Vector3(6f, 4f, 0f);
        wanderGO.SetActive(false); // only active in Pursuit / Evade mode

        // ── SteeringDemoController ───────────────────────────────────────────
        var controllerGO = SceneBuilderUtils.MakeGO("SteeringDemoController");
        var controller   = controllerGO.AddComponent<SteeringDemoController>();
        SceneBuilderUtils.SetRef(controller, "mainAgent",          mainAgent);
        SceneBuilderUtils.SetRef(controller, "mouseTargetMarker",  markerGO.transform);
        SceneBuilderUtils.SetRef(controller, "wanderingTarget",    wanderAgent);
        SceneBuilderUtils.SetRef(controller, "cam",                cam);

        // ── Canvas ───────────────────────────────────────────────────────────
        var canvasGO = SceneBuilderUtils.MakeGO("Canvas");
        var canvas   = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution  = new Vector2(1280, 720);
        scaler.screenMatchMode      = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight   = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        // ── EventSystem ──────────────────────────────────────────────────────
        var esGO = SceneBuilderUtils.MakeGO("EventSystem");
        esGO.AddComponent<EventSystem>();
        esGO.AddComponent<StandaloneInputModule>();

        // ── Right control panel ──────────────────────────────────────────────
        var panel = SceneBuilderUtils.MakeRightPanel(canvasGO.transform);

        var ppBtn    = SceneBuilderUtils.MakeButton(panel.transform, "PlayPauseButton", "Play");
        var resetBtn = SceneBuilderUtils.MakeButton(panel.transform, "ResetButton",     "Reset");

        SceneBuilderUtils.MakeSeparator(panel.transform, "— Mode —");
        var seekBtn    = SceneBuilderUtils.MakeButton(panel.transform, "SeekButton",    "Seek");
        var fleeBtn    = SceneBuilderUtils.MakeButton(panel.transform, "FleeButton",    "Flee");
        var arriveBtn  = SceneBuilderUtils.MakeButton(panel.transform, "ArriveButton",  "Arrive");
        var wanderBtn  = SceneBuilderUtils.MakeButton(panel.transform, "WanderButton",  "Wander");
        var pursuitBtn = SceneBuilderUtils.MakeButton(panel.transform, "PursuitButton", "Pursuit");
        var evadeBtn   = SceneBuilderUtils.MakeButton(panel.transform, "EvadeButton",   "Evade");

        // ── Info text ────────────────────────────────────────────────────────
        var infoTMP = SceneBuilderUtils.MakeInfoBox(canvasGO.transform,
            "Mode:  Seek\nSpeed: 0.0 u/s", new Vector2(210, 55), new Vector2(12, -12));

        // ── SteeringUI ───────────────────────────────────────────────────────
        var steeringUI = canvasGO.AddComponent<SteeringUI>();
        SceneBuilderUtils.SetRef(steeringUI, "controller",     controller);
        SceneBuilderUtils.SetRef(steeringUI, "simController",  simController);
        SceneBuilderUtils.SetRef(steeringUI, "mainAgent",      mainAgent);
        SceneBuilderUtils.SetRef(steeringUI, "playPauseButton", ppBtn);
        SceneBuilderUtils.SetRef(steeringUI, "resetButton",    resetBtn);
        SceneBuilderUtils.SetRef(steeringUI, "seekButton",     seekBtn);
        SceneBuilderUtils.SetRef(steeringUI, "fleeButton",     fleeBtn);
        SceneBuilderUtils.SetRef(steeringUI, "arriveButton",   arriveBtn);
        SceneBuilderUtils.SetRef(steeringUI, "wanderButton",   wanderBtn);
        SceneBuilderUtils.SetRef(steeringUI, "pursuitButton",  pursuitBtn);
        SceneBuilderUtils.SetRef(steeringUI, "evadeButton",    evadeBtn);
        SceneBuilderUtils.SetRef(steeringUI, "infoText",       infoTMP);

        // ── World border ─────────────────────────────────────────────────────
        SceneBuilderUtils.MakeWorldBorder(HalfW, HalfH, new Color(0.35f, 0.35f, 0.45f));

        // ── Save ─────────────────────────────────────────────────────────────
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Scene Built", $"Saved to:\n{ScenePath}", "OK");
    }

    // Sets world-wrap bounds and initial velocity on a SteeringAgent via SerializedObject.
    static void SetBounds(SteeringAgent agent)
    {
        var so = new UnityEditor.SerializedObject(agent);
        so.FindProperty("boundsHalfW").floatValue = HalfW;
        so.FindProperty("boundsHalfH").floatValue = HalfH;
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
#endif
