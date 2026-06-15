using UnityEngine;

namespace BehaviorSimulation.Steering
{
    // Pure static steering-force calculations.
    // All methods follow the same pattern:
    //   desiredVelocity = direction * maxSpeed
    //   steeringForce   = desiredVelocity - currentVelocity   (clamped to maxForce)
    public static class SteeringBehaviors
    {
        public static Vector2 Seek(Vector2 pos, Vector2 target, Vector2 vel,
            float maxSpeed, float maxForce)
        {
            Vector2 desired = (target - pos).normalized * maxSpeed;
            return Clamp(desired - vel, maxForce);
        }

        public static Vector2 Flee(Vector2 pos, Vector2 target, Vector2 vel,
            float maxSpeed, float maxForce)
        {
            Vector2 desired = (pos - target).normalized * maxSpeed;
            return Clamp(desired - vel, maxForce);
        }

        // Seek but ramp speed down inside the slowing radius.
        public static Vector2 Arrive(Vector2 pos, Vector2 target, Vector2 vel,
            float maxSpeed, float maxForce, float slowingRadius)
        {
            Vector2 toTarget = target - pos;
            float dist = toTarget.magnitude;
            if (dist < 0.001f) return Clamp(-vel, maxForce); // brake
            float speed = dist < slowingRadius ? maxSpeed * (dist / slowingRadius) : maxSpeed;
            return Clamp(toTarget / dist * speed - vel, maxForce);
        }

        // wanderAngle is caller-owned state mutated each call.
        // Returns the steering force; also outputs debug positions for gizmos.
        public static Vector2 Wander(Vector2 pos, Vector2 vel,
            float maxSpeed, float maxForce,
            float circleDist, float circleRadius,
            ref float wanderAngle, float jitter,
            out Vector2 circleCenter, out Vector2 wanderPoint)
        {
            wanderAngle += Random.Range(-jitter, jitter) * Time.deltaTime;
            Vector2 heading = vel.sqrMagnitude > 0.001f ? vel.normalized : Vector2.up;
            circleCenter = pos + heading * circleDist;
            wanderPoint  = circleCenter + new Vector2(Mathf.Cos(wanderAngle), Mathf.Sin(wanderAngle)) * circleRadius;
            return Clamp((wanderPoint - pos).normalized * maxSpeed - vel, maxForce);
        }

        // Seek the predicted future position of a moving target.
        public static Vector2 Pursuit(Vector2 pos, Vector2 vel,
            Vector2 targetPos, Vector2 targetVel,
            float maxSpeed, float maxForce, out Vector2 predicted)
        {
            float ahead = (targetPos - pos).magnitude / Mathf.Max(maxSpeed, 0.001f);
            predicted = targetPos + targetVel * ahead;
            return Seek(pos, predicted, vel, maxSpeed, maxForce);
        }

        // Flee from the predicted future position of a moving target.
        public static Vector2 Evade(Vector2 pos, Vector2 vel,
            Vector2 targetPos, Vector2 targetVel,
            float maxSpeed, float maxForce, out Vector2 predicted)
        {
            float ahead = (targetPos - pos).magnitude / Mathf.Max(maxSpeed, 0.001f);
            predicted = targetPos + targetVel * ahead;
            return Flee(pos, predicted, vel, maxSpeed, maxForce);
        }

        static Vector2 Clamp(Vector2 v, float max) =>
            v.sqrMagnitude > max * max ? v.normalized * max : v;
    }
}
