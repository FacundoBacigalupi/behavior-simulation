using UnityEngine;
using BehaviorSimulation.Core;

namespace BehaviorSimulation.Steering
{
    // Manages the Steering Behaviors demo scene.
    // Moves the mouse-target marker and feeds the wandering target's velocity
    // to the main agent for Pursuit / Evade calculations.
    public class SteeringDemoController : MonoBehaviour, ISimulation
    {
        [Header("Main agent")]
        [SerializeField] private SteeringAgent mainAgent;

        [Header("Mouse target (Seek / Flee / Arrive)")]
        [SerializeField] private Transform mouseTargetMarker;

        [Header("Wandering target (Pursuit / Evade)")]
        [SerializeField] private SteeringAgent wanderingTarget;

        [SerializeField] private Camera cam;

        private bool _isPlaying;

        private void Start()
        {
            if (cam == null) cam = Camera.main;
            SimulationController.Instance?.Register(this);

            // Wandering target always wanders, regardless of main agent's mode.
            if (wanderingTarget != null)
                wanderingTarget.Mode = SteeringMode.Wander;

            ApplyMode(SteeringMode.Seek); // default mode for main agent
        }

        private void Update()
        {
            if (!_isPlaying) return;
            TrackMouse();
            FeedTargetVelocity();
        }

        public void SetMode(SteeringMode mode) => ApplyMode(mode);

        // ── ISimulation ──────────────────────────────────────────────────────

        public void Play()  { _isPlaying = true; }
        public void Pause() { _isPlaying = false; }
        public void Step()  { } // continuous sim — Step is a no-op

        public void ResetSimulation()
        {
            _isPlaying = false;
            mainAgent?.ResetAgent(Vector2.zero);
            wanderingTarget?.ResetAgent(new Vector2(6f, 4f));
        }

        // ── Private ──────────────────────────────────────────────────────────

        private void ApplyMode(SteeringMode mode)
        {
            if (mainAgent == null) return;
            mainAgent.Mode = mode;

            bool needsWander = mode == SteeringMode.Pursuit || mode == SteeringMode.Evade;
            mouseTargetMarker?.gameObject.SetActive(!needsWander);
            wanderingTarget?.gameObject.SetActive(needsWander);

            mainAgent.Target = needsWander
                ? wanderingTarget?.transform
                : mouseTargetMarker;
        }

        private void TrackMouse()
        {
            if (mouseTargetMarker == null || !mouseTargetMarker.gameObject.activeSelf) return;
            Vector3 world = cam.ScreenToWorldPoint(Input.mousePosition);
            mouseTargetMarker.position = new Vector3(world.x, world.y, 0f);
        }

        private void FeedTargetVelocity()
        {
            if (mainAgent == null || wanderingTarget == null) return;
            if (wanderingTarget.gameObject.activeSelf)
                mainAgent.TargetVelocity = wanderingTarget.Velocity;
        }
    }
}
