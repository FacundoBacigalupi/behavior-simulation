using TMPro;
using UnityEngine;
using UnityEngine.UI;
using BehaviorSimulation.Core;

namespace BehaviorSimulation.UI
{
    // Reusable UI panel wired to SimulationController.
    // Attach to a Canvas GameObject and assign references in the Inspector.
    public class SimulationUI : MonoBehaviour
    {
        [Header("Buttons")]
        [SerializeField] private Button playPauseButton;
        [SerializeField] private Button stepButton;
        [SerializeField] private Button resetButton;

        [Header("Speed")]
        [SerializeField] private Slider speedSlider;
        [SerializeField] private TMP_Text speedLabel;

        [Header("Info")]
        [SerializeField] private TMP_Text infoText;

        [Header("Debug Toggle")]
        [SerializeField] private Toggle debugToggle;

        public bool DebugVisible { get; private set; } = false;

        // Optional external info provider (set by each simulation scene)
        private System.Func<string> _infoProvider;

        public void SetInfoProvider(System.Func<string> provider)
        {
            _infoProvider = provider;
        }

        private void Start()
        {
            playPauseButton?.onClick.AddListener(OnPlayPause);
            stepButton?.onClick.AddListener(OnStep);
            resetButton?.onClick.AddListener(OnReset);

            if (speedSlider != null)
            {
                speedSlider.minValue = 0.05f;
                speedSlider.maxValue = 10f;
                speedSlider.value = 1f;
                speedSlider.onValueChanged.AddListener(OnSpeedChanged);
            }

            debugToggle?.onValueChanged.AddListener(v => DebugVisible = v);
        }

        private void Update()
        {
            if (infoText != null && _infoProvider != null)
                infoText.text = _infoProvider();
        }

        private void OnPlayPause()
        {
            SimulationController.Instance?.TogglePlayPause();
            UpdatePlayPauseLabel();
        }

        private void OnStep() => SimulationController.Instance?.Step();

        private void OnReset()
        {
            SimulationController.Instance?.ResetSimulation();
            UpdatePlayPauseLabel();
        }

        private void OnSpeedChanged(float value)
        {
            SimulationController.Instance?.SetSpeed(value);
            if (speedLabel != null)
                speedLabel.text = $"Speed: {value:F1}x";
        }

        private void UpdatePlayPauseLabel()
        {
            if (playPauseButton == null) return;
            var label = playPauseButton.GetComponentInChildren<TMP_Text>();
            if (label == null) return;
            bool playing = SimulationController.Instance != null && SimulationController.Instance.IsPlaying;
            label.text = playing ? "Pause" : "Play";
        }
    }
}
