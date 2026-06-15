using UnityEngine;

namespace BehaviorSimulation.AntColony
{
    public enum AntState { Searching, Carrying }

    [RequireComponent(typeof(SpriteRenderer))]
    public class Ant : MonoBehaviour
    {
        public AntState State     { get; private set; } = AntState.Searching;
        public float    Direction { get; set; }          // radians

        static readonly Color SearchColor = new(0.95f, 0.85f, 0.45f);  // yellow
        static readonly Color CarryColor  = new(0.25f, 0.90f, 0.35f);  // green

        SpriteRenderer _sr;

        void Awake()
        {
            _sr       = GetComponent<SpriteRenderer>();
            _sr.color = SearchColor;
        }

        public void SetState(AntState s)
        {
            State     = s;
            _sr.color = s == AntState.Searching ? SearchColor : CarryColor;
        }

        public void ApplyMotion(Vector2 pos, float dir)
        {
            Direction          = dir;
            transform.position = new Vector3(pos.x, pos.y, 0f);
            // Arrow sprite points up (+Y); rotate so tip points in travel direction.
            transform.rotation = Quaternion.Euler(0f, 0f, dir * Mathf.Rad2Deg - 90f);
        }
    }
}
