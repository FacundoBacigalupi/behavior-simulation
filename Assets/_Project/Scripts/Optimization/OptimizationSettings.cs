using UnityEngine;

namespace BehaviorSimulation.Optimization
{
    [CreateAssetMenu(fileName = "OptimizationSettings",
                     menuName  = "Behavior Simulation/OptimizationSettings")]
    public class OptimizationSettings : ScriptableObject
    {
        [Header("World")]
        public float boundsHalfW      = 19f;
        public float boundsHalfH      = 11f;
        public int   agentCount       = 300;

        [Header("Boid physics")]
        public float maxSpeed         = 5f;
        public float maxForce         = 12f;

        [Header("Perception")]
        public float perceptionRadius = 3f;    // alignment + cohesion; also spatial-grid cell size
        public float separationRadius = 1.2f;

        [Header("Weights")]
        public float separationWeight = 1.8f;
        public float alignmentWeight  = 1.0f;
        public float cohesionWeight   = 1.0f;
    }
}
