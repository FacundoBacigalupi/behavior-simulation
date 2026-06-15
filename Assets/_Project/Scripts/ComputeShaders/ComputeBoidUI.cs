using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BehaviorSimulation.Core;

namespace BehaviorSimulation.ComputeShaders
{
    public sealed class ComputeBoidUI : MonoBehaviour
    {
        [SerializeField] ComputeBoidManager  manager;
        [SerializeField] SimulationController simController;

        [SerializeField] Button   playPauseButton;
        [SerializeField] Button   stepButton;
        [SerializeField] Button   resetButton;
        [SerializeField] TMP_Text playPauseLabel;

        [SerializeField] Slider   countSlider;
        [SerializeField] TMP_Text countLabel;

        [SerializeField] TMP_Text statsText;

        bool _playing;

        void Start()
        {
            playPauseButton?.onClick.AddListener(OnPlayPause);
            stepButton?.onClick.AddListener(OnStep);
            resetButton?.onClick.AddListener(OnReset);
            InitCountSlider();
            UpdatePlayLabel();
        }

        void Update()
        {
            if (!manager || !statsText) return;
            statsText.text =
                $"FPS:     {manager.FPS:F0}\n" +
                $"Tick:    {manager.TickMs:F2} ms\n" +
                $"Boids:   {manager.BoidCount:N0}\n" +
                $"Threads: {Mathf.CeilToInt(manager.BoidCount / 64f) * 64}";
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

        void InitCountSlider()
        {
            if (!countSlider) return;
            int cur = Mathf.RoundToInt(countSlider.value);
            if (countLabel) countLabel.text = $"Boids: {cur:N0}";
            manager?.SetCount(cur);
            countSlider.onValueChanged.AddListener(v =>
            {
                int n = Mathf.RoundToInt(v);
                manager?.SetCount(n);
                if (countLabel) countLabel.text = $"Boids: {n:N0}";
            });
        }

        void UpdatePlayLabel()
        {
            if (playPauseLabel) playPauseLabel.text = _playing ? "Pause" : "Play";
        }
    }
}
