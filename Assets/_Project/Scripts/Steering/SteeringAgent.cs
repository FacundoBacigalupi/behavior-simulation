using UnityEngine;
using BehaviorSimulation.Core;

namespace BehaviorSimulation.Steering
{
    public enum SteeringMode { Seek, Flee, Arrive, Wander, Pursuit, Evade }

    // Autonomous 2D agent controlled by classic steering forces.
    // Rotates to face its velocity. Wraps around world bounds.
    [RequireComponent(typeof(SpriteRenderer))]
    public class SteeringAgent : MonoBehaviour
    {
        [Header("Motion")]
        [SerializeField] private float maxSpeed = 6f;
        [SerializeField] private float maxForce = 14f;
        [SerializeField] private float mass     = 1f;

        [Header("Arrive")]
        [SerializeField] private float slowingRadius = 4f;

        [Header("Wander")]
        [SerializeField] private float wanderCircleDist   = 2.5f;
        [SerializeField] private float wanderCircleRadius = 1.5f;
        [SerializeField] private float wanderJitter       = 3f;

        [Header("World Bounds (wrap-around)")]
        [SerializeField] private float boundsHalfW = 20f;
        [SerializeField] private float boundsHalfH = 12f;

        // ── Runtime state ────────────────────────────────────────────────────
        public Vector2 Velocity  { get; private set; }
        public SteeringMode Mode { get; set; } = SteeringMode.Seek;

        // Set by the controller each frame before Update runs.
        public Transform Target         { get; set; }
        public Vector2   TargetVelocity { get; set; }

        // ── Gizmo debug data ─────────────────────────────────────────────────
        private float   _wanderAngle;
        private Vector2 _dbgCircleCenter;
        private Vector2 _dbgWanderPoint;
        private Vector2 _dbgPredicted;

        private void Awake()
        {
            _wanderAngle = Random.Range(0f, Mathf.PI * 2f);
            var sr = GetComponent<SpriteRenderer>();
            if (sr.sprite == null)
                sr.sprite = SpriteFactory.Arrow();
        }

        private void Update()
        {
            // Respect global pause
            if (SimulationController.Instance != null && !SimulationController.Instance.IsPlaying)
                return;

            Vector2 pos      = transform.position;
            Vector2 targetPos = Target != null ? (Vector2)Target.position : pos;

            Vector2 steering = ComputeSteering(pos, targetPos);
            Velocity = Vector2.ClampMagnitude(Velocity + steering / Mathf.Max(mass, 0.001f) * Time.deltaTime, maxSpeed);

            // Move and wrap
            Vector2 next = pos + Velocity * Time.deltaTime;
            next.x = Mathf.Repeat(next.x + boundsHalfW, boundsHalfW * 2f) - boundsHalfW;
            next.y = Mathf.Repeat(next.y + boundsHalfH, boundsHalfH * 2f) - boundsHalfH;
            transform.position = new Vector3(next.x, next.y, 0f);

            // Face velocity direction (sprite tip points +Y, so subtract 90°)
            if (Velocity.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.Euler(0f, 0f,
                    Mathf.Atan2(Velocity.y, Velocity.x) * Mathf.Rad2Deg - 90f);
        }

        public void ResetAgent(Vector2 spawnPos)
        {
            transform.SetPositionAndRotation(new Vector3(spawnPos.x, spawnPos.y, 0f), Quaternion.identity);
            Velocity     = Vector2.zero;
            _wanderAngle = Random.Range(0f, Mathf.PI * 2f);
        }

        // ── Gizmos ───────────────────────────────────────────────────────────

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying) return;
            Vector2 pos = transform.position;

            // Green: current velocity
            Gizmos.color = Color.green;
            Gizmos.DrawLine(pos, pos + Velocity);

            switch (Mode)
            {
                case SteeringMode.Arrive:
                    Gizmos.color = new Color(0f, 1f, 1f, 0.2f);
                    DrawCircle(pos, slowingRadius);
                    break;

                case SteeringMode.Wander:
                    Gizmos.color = new Color(1f, 1f, 0f, 0.35f);
                    DrawCircle(_dbgCircleCenter, wanderCircleRadius);
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawSphere(_dbgWanderPoint, 0.18f);
                    break;

                case SteeringMode.Pursuit:
                case SteeringMode.Evade:
                    Gizmos.color = Color.magenta;
                    DrawCircle(_dbgPredicted, 0.35f);
                    Gizmos.DrawLine(pos, _dbgPredicted);
                    break;
            }
        }

        private static void DrawCircle(Vector2 center, float radius, int segs = 32)
        {
            float step = Mathf.PI * 2f / segs;
            for (int i = 0; i < segs; i++)
            {
                var a = center + new Vector2(Mathf.Cos(i * step),       Mathf.Sin(i * step))       * radius;
                var b = center + new Vector2(Mathf.Cos((i + 1) * step), Mathf.Sin((i + 1) * step)) * radius;
                Gizmos.DrawLine(a, b);
            }
        }

        // ── Steering dispatch ────────────────────────────────────────────────

        private Vector2 ComputeSteering(Vector2 pos, Vector2 targetPos)
        {
            return Mode switch
            {
                SteeringMode.Seek    => SteeringBehaviors.Seek(pos, targetPos, Velocity, maxSpeed, maxForce),
                SteeringMode.Flee    => SteeringBehaviors.Flee(pos, targetPos, Velocity, maxSpeed, maxForce),
                SteeringMode.Arrive  => SteeringBehaviors.Arrive(pos, targetPos, Velocity, maxSpeed, maxForce, slowingRadius),
                SteeringMode.Wander  => SteeringBehaviors.Wander(pos, Velocity, maxSpeed, maxForce,
                                            wanderCircleDist, wanderCircleRadius,
                                            ref _wanderAngle, wanderJitter,
                                            out _dbgCircleCenter, out _dbgWanderPoint),
                SteeringMode.Pursuit => SteeringBehaviors.Pursuit(pos, Velocity, targetPos, TargetVelocity,
                                            maxSpeed, maxForce, out _dbgPredicted),
                SteeringMode.Evade   => SteeringBehaviors.Evade(pos, Velocity, targetPos, TargetVelocity,
                                            maxSpeed, maxForce, out _dbgPredicted),
                _                    => Vector2.zero,
            };
        }
    }
}
