using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BehaviorSimulation.Core;

namespace BehaviorSimulation.Optimization
{
    public sealed class OptimizationUI : MonoBehaviour
    {
        [SerializeField] OptimizationManager  manager;
        [SerializeField] SimulationController simController;

        [SerializeField] Button   playPauseButton;
        [SerializeField] Button   stepButton;
        [SerializeField] Button   resetButton;
        [SerializeField] TMP_Text playPauseLabel;

        [SerializeField] Button   modeButton;
        [SerializeField] TMP_Text modeBtnLabel;

        [SerializeField] Slider   countSlider;
        [SerializeField] TMP_Text countLabel;

        [SerializeField] Button   gridOverlayButton;

        [SerializeField] TMP_Text statsText;

        bool _playing;

        void Start()
        {
            playPauseButton?.onClick.AddListener(OnPlayPause);
            stepButton?.onClick.AddListener(OnStep);
            resetButton?.onClick.AddListener(OnReset);
            modeButton?.onClick.AddListener(OnToggleMode);
            gridOverlayButton?.onClick.AddListener(OnToggleOverlay);

            InitCountSlider();
            UpdatePlayLabel();
            UpdateModeButton();
        }

        void Update()
        {
            if (!manager || !statsText) return;
            string mode = manager.UseSpatialGrid ? "Spatial Grid" : "Brute Force";
            statsText.text =
                $"FPS:    {manager.FPS:F0}\n" +
                $"Tick:   {manager.TickMs:F2} ms\n" +
                $"Agents: {manager.ActiveCount}\n" +
                $"Mode:   {mode}\n" +
                $"Cells:  {manager.GridCells}";
        }

        void OnPlayPause()
        {
            _playing = !_playing;
            if (_playing) simController?.Play();
            else          simController?.Pause();
            UpdatePlayLabel();
        }

        void OnStep()
        {
            if (!_playing) simController?.Step();
        }

        void OnReset()
        {
            _playing = false;
            UpdatePlayLabel();
            simController?.ResetSimulation();
        }

        void OnToggleMode()
        {
            manager?.ToggleSpatialGrid();
            UpdateModeButton();
        }

        void OnToggleOverlay()
        {
            manager?.ToggleGridOverlay();
            bool on = manager?.IsGridOverlayVisible ?? false;
            HighlightBtn(gridOverlayButton, on);
            var lbl = gridOverlayButton?.GetComponentInChildren<TMP_Text>();
            if (lbl) lbl.text = on ? "Hide Grid" : "Show Grid";
        }

        void UpdatePlayLabel()
        {
            if (playPauseLabel) playPauseLabel.text = _playing ? "Pause" : "Play";
        }

        void UpdateModeButton()
        {
            if (!manager) return;
            bool grid = manager.UseSpatialGrid;
            HighlightBtn(modeButton, grid);
            if (modeBtnLabel) modeBtnLabel.text = grid ? "Spatial Grid  O(n·k)" : "Brute Force  O(n²)";
        }

        void InitCountSlider()
        {
            if (!countSlider) return;
            int cur = Mathf.RoundToInt(countSlider.value);
            if (countLabel) countLabel.text = $"Agents: {cur}";
            manager?.SetCount(cur);
            countSlider.onValueChanged.AddListener(v =>
            {
                int n = Mathf.RoundToInt(v);
                manager?.SetCount(n);
                if (countLabel) countLabel.text = $"Agents: {n}";
            });
        }

        static void HighlightBtn(Button btn, bool on)
        {
            if (!btn) return;
            var img = btn.GetComponent<Image>();
            if (!img) return;
            img.color = on
                ? new Color(0.20f, 0.55f, 0.90f)
                : new Color(0.22f, 0.22f, 0.28f);
        }
    }
}
