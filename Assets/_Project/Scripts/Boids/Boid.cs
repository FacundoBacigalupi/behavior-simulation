using UnityEngine;

namespace BehaviorSimulation.Boids
{
    // Lightweight per-boid component. State and physics live in BoidManager.
    // Select this GameObject in the Hierarchy while playing to see gizmos.
    [RequireComponent(typeof(SpriteRenderer))]
    public class Boid : MonoBehaviour
    {
        public Vector2 Velocity { get; set; }

        // Injected by BoidManager after creation.
        internal BoidSettings Settings { private get; set; }

        // Called by BoidManager every tick to apply the computed motion.
        public void ApplyMotion(Vector2 pos, Vector2 vel)
        {
            Velocity = vel;
            transform.position = new Vector3(pos.x, pos.y, 0f);
            if (vel.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.Euler(0f, 0f,
                    Mathf.Atan2(vel.y, vel.x) * Mathf.Rad2Deg - 90f);
        }

        // Gizmos only draw when this boid is selected in the Hierarchy.
        private void OnDrawGizmosSelected()
        {
            if (!Application.isPlaying || Settings == null) return;

            // Perception radius (cyan)
            Gizmos.color = new Color(0f, 1f, 1f, 0.20f);
            DrawCircle(transform.position, Settings.perceptionRadius);

            // Separation radius (orange)
            Gizmos.color = new Color(1f, 0.45f, 0f, 0.30f);
            DrawCircle(transform.position, Settings.separationRadius);

            // Velocity arrow (green)
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position,
                transform.position + (Vector3)Velocity);
        }

        static void DrawCircle(Vector3 c, float r, int segs = 32)
        {
            float s = Mathf.PI * 2f / segs;
            for (int i = 0; i < segs; i++)
            {
                var a = c + new Vector3(Mathf.Cos(i * s),       Mathf.Sin(i * s))       * r;
                var b = c + new Vector3(Mathf.Cos((i + 1) * s), Mathf.Sin((i + 1) * s)) * r;
                Gizmos.DrawLine(a, b);
            }
        }
    }
}
