using UnityEngine;

namespace BehaviorSimulation.DecisionAI
{
    [CreateAssetMenu(fileName = "NPCSettings",
                     menuName  = "Behavior Simulation/NPC Settings")]
    public class NPCSettings : ScriptableObject
    {
        [Header("World")]
        public float boundsHalfW  = 19f;
        public float boundsHalfH  = 11f;

        [Header("Agents")]
        public int   fsmCount     = 5;
        public int   btCount      = 5;

        [Header("HP")]
        public float maxHP        = 100f;
        public float regenRate    = 10f;    // HP/s when not attacking
        public float attackDamage = 28f;    // HP/s taken while in attack range of target
        public float fleeHpPct   = 0.30f;  // start fleeing below this fraction
        public float recoverHpPct = 0.70f; // resume patrol above this fraction

        [Header("Speed")]
        public float patrolSpeed  = 2.2f;
        public float chaseSpeed   = 3.8f;
        public float fleeSpeed    = 4.5f;

        [Header("Ranges")]
        public float chaseRange   = 8f;
        public float attackRange  = 1.6f;

        [Header("Target motion (Lissajous figure-8)")]
        public float targetA      = 12f;    // x amplitude (world units)
        public float targetB      = 6f;     // y amplitude
        public float targetSpeed  = 0.55f;  // radians/s
    }
}
