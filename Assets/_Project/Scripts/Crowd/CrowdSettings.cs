using UnityEngine;

namespace BehaviorSimulation.Crowd
{
    [CreateAssetMenu(fileName = "CrowdSettings",
                     menuName  = "Behavior Simulation/Crowd Settings")]
    public class CrowdSettings : ScriptableObject
    {
        [Header("World")]
        public float boundsHalfW    = 19f;
        public float boundsHalfH    = 11f;

        [Header("Agent motion")]
        public float desiredSpeed   = 1.4f;   // m/s (world units / s)
        public float maxSpeed       = 3.0f;
        public float tau            = 0.5f;   // relaxation time to desired velocity

        [Header("Agent geometry")]
        public float agentRadius    = 0.25f;
        public int   agentCount     = 80;

        [Header("Social force (Helbing model)")]
        public float agentA         = 6f;     // social repulsion strength
        public float agentB         = 0.22f;  // social repulsion range
        public float bodyForce      = 40f;    // physical contact compression

        [Header("Wall force")]
        public float wallA          = 12f;
        public float wallB          = 0.10f;

        [Header("Bottleneck scenario")]
        public float bottleneckX    = 2f;
        public float bottleneckGap  = 1.0f;   // half-gap (full opening = 2× WU)
    }
}
