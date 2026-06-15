using UnityEngine;

namespace BehaviorSimulation.AntColony
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class FoodSource : MonoBehaviour
    {
        [SerializeField] private int maxAmount = 150;

        public int Remaining { get; private set; }

        SpriteRenderer _sr;
        Color          _baseColor;
        Vector3        _baseScale;

        void Awake()
        {
            _sr        = GetComponent<SpriteRenderer>();
            _baseColor = _sr.color;
            _baseScale = transform.localScale;
            Remaining  = maxAmount;
        }

        public void Reinit(int amount)
        {
            maxAmount = amount;
            Remaining = amount;
            UpdateVisual();
        }

        // Returns true and consumes one unit if ant is in range and food remains.
        public bool TryPickup(Vector2 antPos, float radius)
        {
            if (Remaining <= 0) return false;
            if (Vector2.Distance(antPos, transform.position) > radius) return false;
            Remaining--;
            UpdateVisual();
            return true;
        }

        void UpdateVisual()
        {
            float t = (float)Remaining / Mathf.Max(1, maxAmount);
            _sr.color           = Color.Lerp(new Color(0.1f, 0.1f, 0.1f), _baseColor, t);
            transform.localScale = _baseScale * Mathf.Lerp(0.35f, 1f, t);
        }
    }
}
