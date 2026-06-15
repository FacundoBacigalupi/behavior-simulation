using UnityEngine;

namespace BehaviorSimulation.Crowd
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class CrowdAgent : MonoBehaviour
    {
        public Vector2 Velocity { get; set; }
        public Vector2 GoalPos  { get; set; }
        public int     GroupId  { get; set; }

        public void ApplyMotion(Vector2 pos, Vector2 vel)
        {
            Velocity           = vel;
            transform.position = new Vector3(pos.x, pos.y, 0f);
        }
    }
}
