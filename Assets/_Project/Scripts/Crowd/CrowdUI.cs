using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BehaviorSimulation.Core;

namespace BehaviorSimulation.Crowd
{
    public class CrowdUI : MonoBehaviour
    {
        [SerializeField] private CrowdManager        manager;
        [SerializeField] private CrowdSettings       settings;
        [SerializeField] private SimulationController simController;

        [SerializeField] private Button   playPauseButton;
        [SerializeField] private Button   resetButton;
        [SerializeField] private Button   scenarioBidirBtn;
        [SerializeField] private Button   scenarioBotBtn;
        [SerializeField] private Button   scenarioCrossBtn;

        [SerializeField] private TMP_Text playPauseLabel;
        [SerializeField] private TMP_Text scenarioTitle;

        [SerializeField] private Slider   speedSlider;
        [SerializeField] private TMP_Text speedLabel;
        [SerializeField] private Slider   countSlider;
        [SerializeField] private TMP_Text countLabel;

        // ── Expansion ─────────────────────────────────────────────────────────
        [SerializeField] private Button   panicButton;
        [SerializeField] private Button   densityButton;
        [SerializeField] private TMP_Text throughputText;

        bool _playing;
        CrowdScenario _current = CrowdScenario.Bidirectional;

        void Start()
        {
            playPauseButton?.onClick.AddListener(OnPlayPause);
            resetButton?.onClick.AddListener(OnReset);
            scenarioBidirBtn?.onClick.AddListener(() => SelectScenario(CrowdScenario.Bidirectional));
            scenarioBotBtn?.onClick.AddListener(()   => SelectScenario(CrowdScenario.Bottleneck));
            scenarioCrossBtn?.onClick.AddListener(() => SelectScenario(CrowdScenario.Crossflow));
            panicButton?.onClick.AddListener(OnPanic);
            densityButton?.onClick.AddListener(OnToggleDensity);

            InitSlider(speedSlider, speedLabel, "Speed",
                v => { if (settings) settings.desiredSpeed = v; });
            InitSlider(countSlider, countLabel, "Agents",
                v => { if (settings) settings.agentCount = Mathf.RoundToInt(v); }, "F0");

            SelectScenario(CrowdScenario.Bidirectional, rebuild: false);
            UpdatePlayLabel();
            UpdateThroughputVisibility();
        }

        void Update()
        {
            if (throughputText && throughputText.gameObject.activeSelf && manager)
                throughputText.text = $"Flow: {manager.Throughput:F1} ag/s";
        }

        void OnPlayPause()
        {
            _playing = !_playing;
            if (_playing) simController?.Play();
            else          simController?.Pause();
            UpdatePlayLabel();
        }

        void OnReset()
        {
            _playing = false;
            UpdatePlayLabel();
            // Also clear panic highlight on reset
            HighlightBtn(panicButton, false);
            simController?.ResetSimulation();
        }

        void OnPanic()
        {
            manager?.TogglePanic();
            bool panic = manager?.IsPanic ?? false;
            HighlightBtn(panicButton, panic, panicColor: true);
        }

        void OnToggleDensity()
        {
            manager?.ToggleDensity();
            bool showing = manager?.IsDensityVisible ?? false;
            HighlightBtn(densityButton, showing);
            var lbl = densityButton?.GetComponentInChildren<TMP_Text>();
            if (lbl) lbl.text = showing ? "Hide Density" : "Show Density";
        }

        void SelectScenario(CrowdScenario s, bool rebuild = true)
        {
            _current = s;
            if (rebuild) manager?.SetScenario(s);
            _playing = false;
            UpdatePlayLabel();
            HighlightBtn(panicButton, false);   // reset panic visual on scenario change

            if (scenarioTitle)
                scenarioTitle.text = s switch
                {
                    CrowdScenario.Bidirectional => "Bidirectional",
                    CrowdScenario.Bottleneck    => "Bottleneck",
                    CrowdScenario.Crossflow     => "Crossflow",
                    _                           => ""
                };

            HighlightBtn(scenarioBidirBtn, s == CrowdScenario.Bidirectional);
            HighlightBtn(scenarioBotBtn,   s == CrowdScenario.Bottleneck);
            HighlightBtn(scenarioCrossBtn, s == CrowdScenario.Crossflow);
            UpdateThroughputVisibility();
        }

        void UpdateThroughputVisibility()
        {
            if (throughputText)
                throughputText.gameObject.SetActive(_current == CrowdScenario.Bottleneck);
        }

        void UpdatePlayLabel()
        {
            if (playPauseLabel) playPauseLabel.text = _playing ? "Pause" : "Play";
        }

        static void HighlightBtn(Button btn, bool on, bool panicColor = false)
        {
            if (!btn) return;
            var img = btn.GetComponent<UnityEngine.UI.Image>();
            if (!img) return;
            if (!on)
                img.color = new Color(0.25f, 0.25f, 0.30f);
            else
                img.color = panicColor
                    ? new Color(0.75f, 0.20f, 0.15f)   // red for panic
                    : new Color(0.35f, 0.70f, 0.35f);  // green for toggles
        }

        static void InitSlider(Slider slider, TMP_Text label, string prefix,
            System.Action<float> onChange, string fmt = "F1")
        {
            if (!slider) return;
            float cur = slider.value;
            if (label) label.text = $"{prefix}: {cur.ToString(fmt)}";
            onChange?.Invoke(cur);
            slider.onValueChanged.AddListener(v =>
            {
                onChange(v);
                if (label) label.text = $"{prefix}: {v.ToString(fmt)}";
            });
        }
    }
}
