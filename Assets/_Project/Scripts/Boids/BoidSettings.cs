using UnityEngine;

namespace BehaviorSimulation.Boids
{
    [CreateAssetMenu(fileName = "BoidSettings",
                     menuName  = "Behavior Simulation/Boid Settings")]
    public class BoidSettings : ScriptableObject
    {
        [Header("Speed")]
        public float maxSpeed = 6f;
        public float maxForce = 14f;

        [Header("Perception radii")]
        public float perceptionRadius = 3f;    // alignment + cohesion
        public float separationRadius = 1.2f;  // separation (tighter)

        [Header("Behavior weights")]
        [Range(0, 5)] public float separationWeight = 1.8f;
        [Range(0, 5)] public float alignmentWeight  = 1.0f;
        [Range(0, 5)] public float cohesionWeight   = 1.0f;

        [Header("World bounds (wrap-around)")]
        public float boundsHalfW = 18f;
        public float boundsHalfH = 10f;
    }
}
