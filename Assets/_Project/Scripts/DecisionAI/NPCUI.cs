using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BehaviorSimulation.Core;

namespace BehaviorSimulation.DecisionAI
{
    public class NPCUI : MonoBehaviour
    {
        [SerializeField] private NPCManager          manager;
        [SerializeField] private SimulationController simController;

        [SerializeField] private Button   playPauseButton;
        [SerializeField] private Button   resetButton;
        [SerializeField] private Button   stepButton;
        [SerializeField] private TMP_Text playPauseLabel;

        [SerializeField] private TMP_Text fsmStatsText;
        [SerializeField] private TMP_Text btStatsText;

        bool _playing;

        void Start()
        {
            playPauseButton?.onClick.AddListener(OnPlayPause);
            resetButton?.onClick.AddListener(OnReset);
            stepButton?.onClick.AddListener(OnStep);
            UpdatePlayLabel();
        }

        void Update()
        {
            if (!manager) return;
            if (fsmStatsText)
                fsmStatsText.text =
                    $"Patrol: {manager.FSMPatrol}\n" +
                    $"Chase:  {manager.FSMChase}\n"  +
                    $"Attack: {manager.FSMAttack}\n" +
                    $"Flee:   {manager.FSMFlee}";

            if (btStatsText)
                btStatsText.text =
                    $"Patrol: {manager.BTPatrol}\n" +
                    $"Chase:  {manager.BTChase}\n"  +
                    $"Attack: {manager.BTAttack}\n" +
                    $"Flee:   {manager.BTFlee}";
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
            simController?.ResetSimulation();
        }

        void OnStep()
        {
            if (!_playing) simController?.Step();
        }

        void UpdatePlayLabel()
        {
            if (playPauseLabel) playPauseLabel.text = _playing ? "Pause" : "Play";
        }
    }
}
