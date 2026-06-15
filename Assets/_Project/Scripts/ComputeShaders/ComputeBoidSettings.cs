using UnityEngine;

namespace BehaviorSimulation.ComputeShaders
{
    [CreateAssetMenu(fileName = "ComputeBoidSettings",
                     menuName  = "Behavior Simulation/ComputeBoidSettings")]
    public class ComputeBoidSettings : ScriptableObject
    {
        [Header("World")]
        public float boundsHalfW      = 19f;
        public float boundsHalfH      = 11f;
        public int   agentCount       = 3000;

        [Header("Boid physics")]
        public float maxSpeed         = 5f;
        public float maxForce         = 12f;

        [Header("Perception")]
        public float perceptionRadius = 3f;
        public float separationRadius = 1.2f;

        [Header("Weights")]
        public float separationWeight = 1.8f;
        public float alignmentWeight  = 1.0f;
        public float cohesionWeight   = 1.0f;
    }
}
