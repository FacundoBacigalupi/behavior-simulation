using TMPro;
using UnityEngine;
using UnityEngine.UI;
using BehaviorSimulation.Core;

namespace BehaviorSimulation.AntColony
{
    public class AntColonyUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private AntManager          manager;
        [SerializeField] private AntSettings         settings;
        [SerializeField] private SimulationController simController;

        [Header("Buttons")]
        [SerializeField] private Button playPauseButton;
        [SerializeField] private Button resetButton;

        [Header("Info")]
        [SerializeField] private TMP_Text infoText;

        [Header("Sliders")]
        [SerializeField] private Slider   evaporateSlider;
        [SerializeField] private TMP_Text evaporateLabel;
        [SerializeField] private Slider   depositSlider;
        [SerializeField] private TMP_Text depositLabel;

        // ── Unity ─────────────────────────────────────────────────────────────

        void Start()
        {
            playPauseButton?.onClick.AddListener(OnPlayPause);
            resetButton?.onClick.AddListener(() => simController?.ResetSimulation());

            InitSlider(evaporateSlider, evaporateLabel, "Evaporate",
                v => { if (settings) settings.evaporateRate = v; });
            InitSlider(depositSlider, depositLabel, "Deposit",
                v => { if (settings) settings.depositAmount = v; });

            if (manager != null)
                manager.OnFoodCollected += n => UpdateInfo(n);
        }

        void Update() => RefreshPlayLabel();

        // ── Helpers ───────────────────────────────────────────────────────────

        void OnPlayPause()
        {
            simController?.TogglePlayPause();
            RefreshPlayLabel();
        }

        void UpdateInfo(int collected)
        {
            if (infoText == null) return;
            infoText.text = $"Ants:  {manager?.AntCount ?? 0}\nFood:  {collected}";
        }

        void RefreshPlayLabel()
        {
            if (playPauseButton == null) return;
            var lbl = playPauseButton.GetComponentInChildren<TMP_Text>();
            if (lbl)
                lbl.text = (simController != null && simController.IsPlaying) ? "Pause" : "Play";
        }

        static void InitSlider(Slider slider, TMP_Text label, string prefix,
            System.Action<float> onChange)
        {
            if (slider == null) return;
            float cur = slider.value;   // use builder-set value
            if (label != null) label.text = $"{prefix}: {cur:F2}";
            onChange?.Invoke(cur);      // sync settings immediately
            slider.onValueChanged.AddListener(v =>
            {
                onChange(v);
                if (label != null) label.text = $"{prefix}: {v:F2}";
            });
        }
    }
}
