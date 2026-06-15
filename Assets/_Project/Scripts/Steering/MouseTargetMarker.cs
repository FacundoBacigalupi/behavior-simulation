using UnityEngine;
using BehaviorSimulation.Core;

namespace BehaviorSimulation.Steering
{
    // Generates a yellow ring sprite at runtime and assigns it to the SpriteRenderer.
    // The SteeringDemoController moves this object to the mouse world position.
    [RequireComponent(typeof(SpriteRenderer))]
    public class MouseTargetMarker : MonoBehaviour
    {
        private void Awake()
        {
            var sr = GetComponent<SpriteRenderer>();
            if (sr.sprite == null)
                sr.sprite = SpriteFactory.Ring(32, 3f, 32f); // 1 world unit diameter
        }
    }
}
