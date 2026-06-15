#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Rendering.Universal;
using TMPro;
using BehaviorSimulation.Core;
using BehaviorSimulation.DecisionAI;

public static class NPCSceneBuilder
{
    const string ScenePath    = "Assets/_Project/Scenes/07_NPCDecision.unity";
    const string SettingsPath = "Assets/_Project/ScriptableObjects/NPCSettings.asset";
    const float  HalfW = 19f, HalfH = 11f;

    [MenuItem("Behavior Simulation/Build Scene — 07 NPC Decision Making")]
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
        var settings = AssetDatabase.LoadAssetAtPath<NPCSettings>(SettingsPath);
        if (settings == null)
        {
            settings = ScriptableObject.CreateInstance<NPCSettings>();
            AssetDatabase.CreateAsset(settings, SettingsPath);
        }
        var sso = new SerializedObject(settings);
        SetFloat(sso, "boundsHalfW",   HalfW);
        SetFloat(sso, "boundsHalfH",   HalfH);
        SetInt  (sso, "fsmCount",      5);
        SetInt  (sso, "btCount",       5);
        SetFloat(sso, "maxHP",         100f);
        SetFloat(sso, "regenRate",     10f);
        SetFloat(sso, "attackDamage",  60f);
        SetFloat(sso, "fleeHpPct",     0.30f);
        SetFloat(sso, "recoverHpPct",  0.70f);
        SetFloat(sso, "patrolSpeed",   2.2f);
        SetFloat(sso, "chaseSpeed",    3.8f);
        SetFloat(sso, "fleeSpeed",     4.5f);
        SetFloat(sso, "chaseRange",    7f);   // matches ring: 14 WU diameter / 2
        SetFloat(sso, "attackRange",   2.0f);
        SetFloat(sso, "targetA",       12f);
        SetFloat(sso, "targetB",       6f);
        SetFloat(sso, "targetSpeed",   0.15f);
        sso.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.SaveAssets();

        // ── SimulationController ───────────────────────────────────────────────
        var scGO = new GameObject("SimulationController");
        var sc   = scGO.AddComponent<SimulationController>();

        // ── World border ───────────────────────────────────────────────────────
        SceneBuilderUtils.MakeWorldBorder(HalfW, HalfH,
            new Color(0.30f, 0.30f, 0.40f), z: 0.1f, lineWidth: 0.12f);

        // ── Centre divider ─────────────────────────────────────────────────────
        var divGO = new GameObject("Divider");
        var dlr   = divGO.AddComponent<LineRenderer>();
        dlr.useWorldSpace  = true;
        dlr.positionCount  = 2;
        dlr.startWidth     = 0.06f;
        dlr.endWidth       = 0.06f;
        dlr.material       = new Material(Shader.Find("Sprites/Default"));
        dlr.startColor     = new Color(0.40f, 0.40f, 0.50f, 0.50f);
        dlr.endColor       = dlr.startColor;
        dlr.sortingOrder   = -1;
        dlr.SetPositions(new Vector3[] { new(0f, -HalfH, 0f), new(0f, HalfH, 0f) });

        // ── Group labels (world space TextMeshPro) ─────────────────────────────
        MakeWorldLabel("FSM_Label", "FSM GUARDS", new Vector3(-13f,  HalfH - 1f, -0.2f),
            new Color(0.55f, 0.75f, 1.00f));
        MakeWorldLabel("BT_Label",  "BT GUARDS",  new Vector3( 13f,  HalfH - 1f, -0.2f),
            new Color(0.45f, 1.00f, 0.60f));

        // ── Target sprite ──────────────────────────────────────────────────────
        var targetGO = new GameObject("Target");
        var targetSR = targetGO.AddComponent<SpriteRenderer>();
        targetSR.sprite       = SpriteFactory.Circle(20, 40f);   // 0.5 WU
        targetSR.color        = new Color(1f, 1f, 1f, 0.95f);
        targetSR.sortingOrder = 5;
        targetGO.transform.position = Vector3.zero;

        // Chase-range ring — diameter = 2 * chaseRange = 20 WU
        // Ring(200, 10f, 10f): 200px texture, ppu=10 → 20 WU diameter, ~1 WU ring width
        var ringGO = new GameObject("TargetRing");
        ringGO.transform.SetParent(targetGO.transform, false);
        var ringSR    = ringGO.AddComponent<SpriteRenderer>();
        ringSR.sprite = SpriteFactory.Ring(140, 3f, 10f);  // 14 WU diameter, 0.3 WU border
        ringSR.color  = new Color(1f, 1f, 1f, 0.18f);
        ringSR.sortingOrder = -2;

        // ── NPCManager ─────────────────────────────────────────────────────────
        var mgrGO   = new GameObject("NPCManager");
        var manager = mgrGO.AddComponent<NPCManager>();
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
        var panel  = SceneBuilderUtils.MakeRightPanel(canvasGO.transform, 210f);
        var panelT = panel.transform;

        SceneBuilderUtils.MakeLabel(panelT, "Title", "NPC DECISIONS", fontSize: 16);
        SceneBuilderUtils.MakeSeparator(panelT, "Simulation");

        var playBtn  = SceneBuilderUtils.MakeButton(panelT, "PlayPauseBtn", "Play");
        var stepBtn  = SceneBuilderUtils.MakeButton(panelT, "StepBtn",      "Step");
        var resetBtn = SceneBuilderUtils.MakeButton(panelT, "ResetBtn",     "Reset");

        // ── FSM stats ──────────────────────────────────────────────────────────
        SceneBuilderUtils.MakeSeparator(panelT, "FSM Guards (blue)");

        var fsmStats = SceneBuilderUtils.MakeLabel(panelT, "FSMStats",
            "Patrol: 0\nChase:  0\nAttack: 0\nFlee:   0",
            fontSize: 13, align: TextAlignmentOptions.Left);
        fsmStats.color = new Color(0.65f, 0.80f, 1.00f);
        fsmStats.gameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 72);

        // ── BT stats ───────────────────────────────────────────────────────────
        SceneBuilderUtils.MakeSeparator(panelT, "BT Guards (green)");

        var btStats = SceneBuilderUtils.MakeLabel(panelT, "BTStats",
            "Patrol: 0\nChase:  0\nAttack: 0\nFlee:   0",
            fontSize: 13, align: TextAlignmentOptions.Left);
        btStats.color = new Color(0.55f, 1.00f, 0.65f);
        btStats.gameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 72);

        // ── Legend ─────────────────────────────────────────────────────────────
        SceneBuilderUtils.MakeSeparator(panelT, "State colours");
        var legend = SceneBuilderUtils.MakeLabel(panelT, "Legend",
            "Blue/Green = Patrol\nYellow      = Chase\nRed         = Attack\nMagenta     = Flee",
            fontSize: 11, align: TextAlignmentOptions.Left);
        legend.gameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 72);

        // ── Play button label ref ─────────────────────────────────────────────
        var playLblTMP = playBtn.transform.Find("Text")?.GetComponent<TextMeshProUGUI>();

        // ── NPCUI ──────────────────────────────────────────────────────────────
        var uiGO = new GameObject("NPCUI");
        var ui   = uiGO.AddComponent<NPCUI>();
        SceneBuilderUtils.SetRef(ui, "manager",          manager);
        SceneBuilderUtils.SetRef(ui, "simController",    sc);
        SceneBuilderUtils.SetRef(ui, "playPauseButton",  playBtn);
        SceneBuilderUtils.SetRef(ui, "resetButton",      resetBtn);
        SceneBuilderUtils.SetRef(ui, "stepButton",       stepBtn);
        SceneBuilderUtils.SetRef(ui, "fsmStatsText",     fsmStats);
        SceneBuilderUtils.SetRef(ui, "btStatsText",      btStats);
        if (playLblTMP != null)
            SceneBuilderUtils.SetRef(ui, "playPauseLabel", playLblTMP);

        // ── Save ────────────────────────────────────────────────────────────────
        EnsureDir("Assets/_Project/Scenes");
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.Refresh();
        Debug.Log("[NPCSceneBuilder] 07_NPCDecision.unity built and saved.");
    }

    // ── Utilities ─────────────────────────────────────────────────────────────

    static void MakeWorldLabel(string name, string text, Vector3 pos, Color color)
    {
        var go  = new GameObject(name);
        go.transform.position = pos;
        var tmp = go.AddComponent<TextMeshPro>();
        tmp.text            = text;
        tmp.fontSize        = 3f;
        tmp.alignment       = TextAlignmentOptions.Center;
        tmp.color           = color;
        tmp.sortingOrder    = 10;
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
        else Debug.LogWarning($"[NPCBuilder] float '{f}' not found");
    }

    static void SetInt(SerializedObject so, string f, int v)
    {
        var p = so.FindProperty(f);
        if (p != null) p.intValue = v;
        else Debug.LogWarning($"[NPCBuilder] int '{f}' not found");
    }
}
#endif
