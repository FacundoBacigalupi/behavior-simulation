using UnityEngine;

namespace BehaviorSimulation.Ecosystem
{
    [CreateAssetMenu(fileName = "PredatorPreySettings",
                     menuName  = "Behavior Simulation/Predator Prey Settings")]
    public class PredatorPreySettings : ScriptableObject
    {
        [Header("World")]
        public float boundsHalfW = 19f;
        public float boundsHalfH = 11f;

        [Header("Prey")]
        public int   preyStart          = 50;
        public int   preyMax            = 200;
        public float preySpeed          = 4f;
        public float preyMaxForce       = 10f;
        public float preyFleeSpeedMult  = 1.6f;  // burst multiplier when fleeing
        public float preyEnergyStart    = 12f;
        public float preyEnergyDecay    = 0.3f;  // per second
        public float preyFleeRadius     = 5f;
        public float preyEatRadius      = 0.7f;
        public float preyReproduceE     = 12f;
        public float preyReproduceCool  = 4f;    // seconds between offspring

        [Header("Predator")]
        public int   predStart          = 8;
        public int   predMax            = 60;
        public float predSpeed          = 5f;
        public float predMaxForce       = 12f;
        public float predHuntRadius     = 12f;   // stop chasing prey beyond this
        public float predEnergyStart    = 15f;
        public float predEnergyDecay    = 0.6f;
        public float predEatRadius      = 1.0f;
        public float predEatGain        = 8f;
        public float predReproduceE     = 26f;
        public float predReproduceCool  = 12f;

        [Header("Food")]
        public int   foodCount          = 80;
        public float foodRegrowTime     = 5f;
        public float foodEatGain        = 6f;
        public float foodEatRadius      = 0.7f;
    }
}
