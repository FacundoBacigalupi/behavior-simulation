using UnityEngine;

namespace BehaviorSimulation.Ecosystem
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class FoodPatch : MonoBehaviour
    {
        public bool  IsAvailable { get; private set; } = true;

        float _timer;
        SpriteRenderer _sr;

        void Awake() => _sr = GetComponent<SpriteRenderer>();

        public void Eat(float regrowTime)
        {
            IsAvailable   = false;
            _timer        = regrowTime;
            _sr.enabled   = false;
        }

        public void Tick(float dt)
        {
            if (IsAvailable) return;
            _timer -= dt;
            if (_timer <= 0f)
            {
                IsAvailable = true;
                _sr.enabled = true;
            }
        }
    }
}
