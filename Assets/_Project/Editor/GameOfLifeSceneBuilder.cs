#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using BehaviorSimulation.Core;
using BehaviorSimulation.GameOfLife;

public static class GameOfLifeSceneBuilder
{
    private const string ScenePath = "Assets/_Project/Scenes/01_GameOfLife.unity";

    [MenuItem("Behavior Simulation/Build Scene — 01 Game of Life")]
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
        cam.orthographicSize = 34f;
        cam.clearFlags       = CameraClearFlags.SolidColor;
        cam.backgroundColor  = new Color(0.05f, 0.05f, 0.08f);
        cameraGO.transform.position = new Vector3(0f, 0f, -10f);
        cameraGO.AddComponent<AudioListener>();
        cameraGO.AddComponent<CameraController2D>();

        // ── Grid ─────────────────────────────────────────────────────────────
        var gridGO   = SceneBuilderUtils.MakeGO("Grid");
        var lifeGrid = gridGO.AddComponent<LifeGrid>();
        gridGO.AddComponent<SpriteRenderer>();
        var lifeRenderer = gridGO.AddComponent<LifeRenderer>();
        SceneBuilderUtils.SetRef(lifeRenderer, "grid", lifeGrid);

        // ── LifeInput on Camera ──────────────────────────────────────────────
        var lifeInput = cameraGO.AddComponent<LifeInput>();
        SceneBuilderUtils.SetRef(lifeInput, "grid", lifeGrid);
        SceneBuilderUtils.SetRef(lifeInput, "cam",  cam);

        // ── Canvas ───────────────────────────────────────────────────────────
        var canvasGO = SceneBuilderUtils.MakeGO("Canvas");
        var canvas   = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode        = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.screenMatchMode    = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        // ── EventSystem ──────────────────────────────────────────────────────
        var esGO = SceneBuilderUtils.MakeGO("EventSystem");
        esGO.AddComponent<EventSystem>();
        esGO.AddComponent<StandaloneInputModule>();

        // ── Right panel ──────────────────────────────────────────────────────
        var panel = SceneBuilderUtils.MakeRightPanel(canvasGO.transform);

        var ppBtn    = SceneBuilderUtils.MakeButton(panel.transform, "PlayPauseButton", "Play");
        var stepBtn  = SceneBuilderUtils.MakeButton(panel.transform, "StepButton",      "Step");
        var resetBtn = SceneBuilderUtils.MakeButton(panel.transform, "ResetButton",     "Reset");
        var clearBtn = SceneBuilderUtils.MakeButton(panel.transform, "ClearButton",     "Clear");
        var randBtn  = SceneBuilderUtils.MakeButton(panel.transform, "RandomButton",    "Random");

        SceneBuilderUtils.MakeSeparator(panel.transform, "— Speed —");
        var speedLbl    = SceneBuilderUtils.MakeLabel(panel.transform, "SpeedLabel", "5 gen/s");
        var speedSlider = SceneBuilderUtils.MakeSlider(panel.transform, "SpeedSlider", 1f, 30f, 5f);

        SceneBuilderUtils.MakeSeparator(panel.transform, "— Patterns —");
        var gliderBtn  = SceneBuilderUtils.MakeButton(panel.transform, "GliderButton",  "Glider");
        var blinkerBtn = SceneBuilderUtils.MakeButton(panel.transform, "BlinkerButton", "Blinker");
        var toadBtn    = SceneBuilderUtils.MakeButton(panel.transform, "ToadButton",    "Toad");
        var pulsarBtn  = SceneBuilderUtils.MakeButton(panel.transform, "PulsarButton",  "Pulsar");

        // ── Info text ────────────────────────────────────────────────────────
        var infoTMP = SceneBuilderUtils.MakeInfoBox(canvasGO.transform,
            "Generation: 0\nAlive: 0", new Vector2(220, 60), new Vector2(12, -12));

        // ── GameOfLifeUI ─────────────────────────────────────────────────────
        var goUI = canvasGO.AddComponent<GameOfLifeUI>();
        SceneBuilderUtils.SetRef(goUI, "grid",            lifeGrid);
        SceneBuilderUtils.SetRef(goUI, "simController",   simController);
        SceneBuilderUtils.SetRef(goUI, "playPauseButton", ppBtn);
        SceneBuilderUtils.SetRef(goUI, "stepButton",      stepBtn);
        SceneBuilderUtils.SetRef(goUI, "resetButton",     resetBtn);
        SceneBuilderUtils.SetRef(goUI, "clearButton",     clearBtn);
        SceneBuilderUtils.SetRef(goUI, "randomButton",    randBtn);
        SceneBuilderUtils.SetRef(goUI, "gliderButton",    gliderBtn);
        SceneBuilderUtils.SetRef(goUI, "blinkerButton",   blinkerBtn);
        SceneBuilderUtils.SetRef(goUI, "toadButton",      toadBtn);
        SceneBuilderUtils.SetRef(goUI, "pulsarButton",    pulsarBtn);
        SceneBuilderUtils.SetRef(goUI, "speedSlider",     speedSlider);
        SceneBuilderUtils.SetRef(goUI, "speedLabel",      speedLbl);
        SceneBuilderUtils.SetRef(goUI, "infoText",        infoTMP);

        // ── Grid border ──────────────────────────────────────────────────────
        SceneBuilderUtils.MakeWorldBorder(40f, 30f, new Color(0.4f, 0.4f, 0.5f));

        // ── Save ─────────────────────────────────────────────────────────────
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Scene Built", $"Saved to:\n{ScenePath}", "OK");
    }
}
#endif
