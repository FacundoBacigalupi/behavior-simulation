using UnityEngine;

namespace BehaviorSimulation.AntColony
{
    [CreateAssetMenu(fileName = "AntSettings",
                     menuName  = "Behavior Simulation/Ant Settings")]
    public class AntSettings : ScriptableObject
    {
        [Header("World")]
        public float boundsHalfW    = 19f;
        public float boundsHalfH    = 11f;

        [Header("Pheromone grid  (gridW/gridH must give the same PPU)")]
        public int   gridW          = 190;  // 190 / 38 = 5 cells per world-unit
        public int   gridH          = 110;  // 110 / 22 = 5 cells per world-unit
        [Range(0.01f, 1f)]
        public float evaporateRate  = 0.12f; // fraction lost per second
        public float depositAmount  = 0.08f; // pheromone deposited per ant per second

        [Header("Ants")]
        public int   antCount       = 120;
        public float antSpeed       = 3.5f;
        public float turnSpeed      = 5f;    // rad / s  (pheromone-driven)
        public float wanderNoise    = 0.8f;  // rad / s  (always-present random noise)
        public float sensorDist     = 1.5f;  // world units ahead to sample
        public float sensorAngle    = 40f;   // degrees left / right

        [Header("Interactions")]
        public float pickupRadius   = 1.4f;
        public float nestRadius     = 2.0f;

        [Header("Food")]
        public int   foodPerSource  = 150;
    }
}
