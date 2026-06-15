using TMPro;
using UnityEngine;
using UnityEngine.UI;
using BehaviorSimulation.Core;

namespace BehaviorSimulation.Steering
{
    public class SteeringUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private SteeringDemoController controller;
        [SerializeField] private SimulationController   simController;
        [SerializeField] private SteeringAgent          mainAgent;

        [Header("Core buttons")]
        [SerializeField] private Button playPauseButton;
        [SerializeField] private Button resetButton;

        [Header("Mode buttons")]
        [SerializeField] private Button seekButton;
        [SerializeField] private Button fleeButton;
        [SerializeField] private Button arriveButton;
        [SerializeField] private Button wanderButton;
        [SerializeField] private Button pursuitButton;
        [SerializeField] private Button evadeButton;

        [Header("Info")]
        [SerializeField] private TMP_Text infoText;

        private Button _activeMode;

        private void Start()
        {
            playPauseButton?.onClick.AddListener(OnPlayPause);
            resetButton?.onClick.AddListener(OnReset);

            WireMode(seekButton,    SteeringMode.Seek);
            WireMode(fleeButton,    SteeringMode.Flee);
            WireMode(arriveButton,  SteeringMode.Arrive);
            WireMode(wanderButton,  SteeringMode.Wander);
            WireMode(pursuitButton, SteeringMode.Pursuit);
            WireMode(evadeButton,   SteeringMode.Evade);

            HighlightButton(seekButton); // matches default mode
        }

        private void Update()
        {
            if (infoText == null || mainAgent == null) return;
            infoText.text = $"Mode:  {mainAgent.Mode}\n" +
                            $"Speed: {mainAgent.Velocity.magnitude:F1} u/s";
        }

        // ── Handlers ─────────────────────────────────────────────────────────

        private void OnPlayPause()
        {
            simController?.TogglePlayPause();
            RefreshPlayPauseLabel();
        }

        private void OnReset()
        {
            simController?.ResetSimulation();
            RefreshPlayPauseLabel();
        }

        private void WireMode(Button btn, SteeringMode mode)
        {
            btn?.onClick.AddListener(() =>
            {
                controller?.SetMode(mode);
                HighlightButton(btn);
            });
        }

        // ── Label helpers ─────────────────────────────────────────────────────

        private void RefreshPlayPauseLabel()
        {
            if (playPauseButton == null) return;
            var lbl = playPauseButton.GetComponentInChildren<TMP_Text>();
            if (lbl != null)
                lbl.text = (simController != null && simController.IsPlaying) ? "Pause" : "Play";
        }

        // Tint the active mode button to make the selection obvious.
        private void HighlightButton(Button active)
        {
            Button[] all = { seekButton, fleeButton, arriveButton, wanderButton, pursuitButton, evadeButton };
            foreach (var b in all)
            {
                if (b == null) continue;
                var img = b.GetComponent<Image>();
                if (img == null) continue;
                img.color = b == active
                    ? new Color(0.15f, 0.45f, 0.80f)   // active: blue
                    : new Color(0.22f, 0.22f, 0.28f);   // inactive: dark
            }
            _activeMode = active;
        }
    }
}
