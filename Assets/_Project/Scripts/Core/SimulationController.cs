using System;
using UnityEngine;

namespace BehaviorSimulation.Core
{
    // Central hub for play/pause/step/reset. Each simulation registers itself and
    // handles its own timing — this controller does not touch Time.timeScale.
    public class SimulationController : MonoBehaviour
    {
        public static SimulationController Instance { get; private set; }

        public bool IsPlaying { get; private set; }

        // Simulations subscribe to receive speed changes (each interprets it differently).
        public event Action<float> OnSpeedChanged;

        private ISimulation _simulation;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public void Register(ISimulation simulation)
        {
            _simulation = simulation;
        }

        public void Play()
        {
            IsPlaying = true;
            _simulation?.Play();
        }

        public void Pause()
        {
            IsPlaying = false;
            _simulation?.Pause();
        }

        public void TogglePlayPause()
        {
            if (IsPlaying) Pause(); else Play();
        }

        public void Step()
        {
            if (!IsPlaying) _simulation?.Step();
        }

        public void ResetSimulation()
        {
            IsPlaying = false;
            _simulation?.ResetSimulation();
        }

        public void SetSpeed(float value)
        {
            OnSpeedChanged?.Invoke(value);
        }
    }
}
