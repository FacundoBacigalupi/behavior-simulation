using TMPro;
using UnityEngine;
using UnityEngine.UI;
using BehaviorSimulation.Core;

namespace BehaviorSimulation.Boids
{
    public class BoidsUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private BoidManager          manager;
        [SerializeField] private BoidSettings         settings;
        [SerializeField] private SimulationController simController;

        [Header("Core buttons")]
        [SerializeField] private Button playPauseButton;
        [SerializeField] private Button resetButton;

        [Header("Spawn buttons")]
        [SerializeField] private Button spawn10Btn;
        [SerializeField] private Button spawn50Btn;
        [SerializeField] private Button spawn100Btn;
        [SerializeField] private Button spawn300Btn;

        [Header("Weight sliders + labels")]
        [SerializeField] private Slider   separationSlider;
        [SerializeField] private TMP_Text separationLabel;
        [SerializeField] private Slider   alignmentSlider;
        [SerializeField] private TMP_Text alignmentLabel;
        [SerializeField] private Slider   cohesionSlider;
        [SerializeField] private TMP_Text cohesionLabel;

        [Header("Info")]
        [SerializeField] private TMP_Text infoText;

        private void Start()
        {
            playPauseButton?.onClick.AddListener(OnPlayPause);
            resetButton?.onClick.AddListener(() => simController?.ResetSimulation());

            spawn10Btn?.onClick.AddListener(()  => manager?.SpawnBoids(10));
            spawn50Btn?.onClick.AddListener(()  => manager?.SpawnBoids(50));
            spawn100Btn?.onClick.AddListener(() => manager?.SpawnBoids(100));
            spawn300Btn?.onClick.AddListener(() => manager?.SpawnBoids(300));

            // Read initial value from slider (set by scene builder), then sync to settings.
            InitSlider(separationSlider, separationLabel, "Sep",
                v => { if (settings) settings.separationWeight = v; });
            InitSlider(alignmentSlider, alignmentLabel, "Ali",
                v => { if (settings) settings.alignmentWeight  = v; });
            InitSlider(cohesionSlider, cohesionLabel, "Coh",
                v => { if (settings) settings.cohesionWeight   = v; });
        }

        private void Update()
        {
            if (infoText == null) return;
            int fps   = Mathf.RoundToInt(1f / Mathf.Max(Time.unscaledDeltaTime, 0.0001f));
            int count = manager != null ? manager.BoidCount : 0;
            infoText.text = $"Boids: {count}\nFPS:   {fps}";
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private void OnPlayPause()
        {
            simController?.TogglePlayPause();
            RefreshPlayLabel();
        }

        private void RefreshPlayLabel()
        {
            if (playPauseButton == null) return;
            var lbl = playPauseButton.GetComponentInChildren<TMP_Text>();
            if (lbl != null)
                lbl.text = (simController != null && simController.IsPlaying) ? "Pause" : "Play";
        }

        private static void InitSlider(Slider slider, TMP_Text label,
            string prefix, System.Action<float> onChange)
        {
            if (slider == null) return;
            float cur = slider.value;                        // use builder-set value
            if (label != null) label.text = $"{prefix}: {cur:F1}";
            onChange?.Invoke(cur);                           // sync settings immediately
            slider.onValueChanged.AddListener(v =>
            {
                onChange(v);
                if (label != null) label.text = $"{prefix}: {v:F1}";
            });
        }
    }
}
