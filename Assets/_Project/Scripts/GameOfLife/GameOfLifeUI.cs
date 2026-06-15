using TMPro;
using UnityEngine;
using UnityEngine.UI;
using BehaviorSimulation.Core;

namespace BehaviorSimulation.GameOfLife
{
    // UI specific to the Game of Life scene.
    // Attach to a Canvas. Wire all references in the Inspector.
    public class GameOfLifeUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private LifeGrid grid;
        [SerializeField] private SimulationController simController;

        [Header("Core Buttons")]
        [SerializeField] private Button playPauseButton;
        [SerializeField] private Button stepButton;
        [SerializeField] private Button resetButton;
        [SerializeField] private Button clearButton;
        [SerializeField] private Button randomButton;

        [Header("Speed")]
        [SerializeField] private Slider speedSlider;   // range 1-30 gen/s
        [SerializeField] private TMP_Text speedLabel;

        [Header("Pattern Buttons")]
        [SerializeField] private Button gliderButton;
        [SerializeField] private Button blinkerButton;
        [SerializeField] private Button toadButton;
        [SerializeField] private Button pulsarButton;

        [Header("Info")]
        [SerializeField] private TMP_Text infoText;

        private void Start()
        {
            playPauseButton?.onClick.AddListener(OnPlayPause);
            stepButton?.onClick.AddListener(() => simController?.Step());
            resetButton?.onClick.AddListener(OnReset);
            clearButton?.onClick.AddListener(OnClear);
            randomButton?.onClick.AddListener(OnRandom);

            gliderButton?.onClick.AddListener(() => PlacePattern(LifePatterns.Glider));
            blinkerButton?.onClick.AddListener(() => PlacePattern(LifePatterns.Blinker));
            toadButton?.onClick.AddListener(() => PlacePattern(LifePatterns.Toad));
            pulsarButton?.onClick.AddListener(() => PlacePattern(LifePatterns.Pulsar));

            if (speedSlider != null)
            {
                speedSlider.minValue = 1f;
                speedSlider.maxValue = 30f;
                speedSlider.value = 5f;
                speedSlider.onValueChanged.AddListener(OnSpeedChanged);
                UpdateSpeedLabel(5f);
            }
        }

        private void Update()
        {
            if (infoText != null && grid != null)
                infoText.text = $"Generation: {grid.Generation}\nAlive: {grid.AliveCount}";

            UpdatePlayPauseLabel();
        }

        // --- Button handlers ---

        private void OnPlayPause() => simController?.TogglePlayPause();

        private void OnReset()
        {
            simController?.ResetSimulation();
        }

        private void OnClear()
        {
            simController?.Pause();
            grid?.ClearGrid();
        }

        private void OnRandom()
        {
            simController?.Pause();
            grid?.RandomFill();
        }

        private void PlacePattern(Vector2Int[] pattern)
        {
            simController?.Pause();
            if (grid == null) return;
            // Place at grid center
            grid.PlacePattern(pattern, grid.Width / 2, grid.Height / 2);
        }

        private void OnSpeedChanged(float value)
        {
            grid?.SetTicksPerSecond(value);
            UpdateSpeedLabel(value);
        }

        // --- Label helpers ---

        private void UpdatePlayPauseLabel()
        {
            if (playPauseButton == null || simController == null) return;
            var label = playPauseButton.GetComponentInChildren<TMP_Text>();
            if (label != null)
                label.text = simController.IsPlaying ? "Pause" : "Play";
        }

        private void UpdateSpeedLabel(float value)
        {
            if (speedLabel != null)
                speedLabel.text = $"{value:F0} gen/s";
        }
    }
}
