using UnityEngine;

namespace BehaviorSimulation.Ecosystem
{
    public enum AgentType { Prey, Predator }

    [RequireComponent(typeof(SpriteRenderer))]
    public class EcosystemAgent : MonoBehaviour
    {
        public AgentType Type                { get; set; }
        public Vector2   Velocity            { get; set; }
        public float     Energy              { get; set; }
        public bool      IsAlive             { get; set; } = true;
        public float     WanderAngle         { get; set; }
        public float     ReproductionCooldown { get; set; }

        public void ApplyMotion(Vector2 pos, Vector2 vel)
        {
            Velocity = vel;
            transform.position = new Vector3(pos.x, pos.y, 0f);
            if (vel.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.Euler(0f, 0f,
                    Mathf.Atan2(vel.y, vel.x) * Mathf.Rad2Deg - 90f);
        }
    }
}
