using System.Collections.Generic;
using UnityEngine;
using BehaviorSimulation.Core;

namespace BehaviorSimulation.Boids
{
    // Owns all boids and runs the O(n²) flocking update each frame.
    public class BoidManager : MonoBehaviour, ISimulation
    {
        [SerializeField] private BoidSettings settings;
        [SerializeField] private Color        boidColor = new(0.35f, 0.9f, 0.55f);
        [SerializeField] private int          startCount = 50;

        private readonly List<Boid> _boids = new();
        private bool     _isPlaying;
        private Vector2[] _nextPos = System.Array.Empty<Vector2>();
        private Vector2[] _nextVel = System.Array.Empty<Vector2>();

        public int BoidCount => _boids.Count;

        private void Start()
        {
            SimulationController.Instance?.Register(this);
            SpawnBoids(startCount);
        }

        private void Update()
        {
            if (!_isPlaying || _boids.Count == 0) return;
            Tick(Time.deltaTime);
        }

        // ── Public API ────────────────────────────────────────────────────────

        public void SpawnBoids(int count)
        {
            ClearBoids();
            for (int i = 0; i < count; i++)
            {
                var go = new GameObject($"Boid_{i}");
                go.transform.SetParent(transform, false);

                var sr    = go.AddComponent<SpriteRenderer>();
                sr.sprite = SpriteFactory.Arrow();
                sr.color  = boidColor;

                var boid     = go.AddComponent<Boid>();
                boid.Settings = settings;

                float x = Random.Range(-settings.boundsHalfW * 0.9f, settings.boundsHalfW * 0.9f);
                float y = Random.Range(-settings.boundsHalfH * 0.9f, settings.boundsHalfH * 0.9f);
                var   vel = Random.insideUnitCircle.normalized * (settings.maxSpeed * Random.Range(0.3f, 0.7f));

                boid.ApplyMotion(new Vector2(x, y), vel);
                _boids.Add(boid);
            }

            GrowBuffers(_boids.Count);
        }

        public void ClearBoids()
        {
            foreach (var b in _boids)
                if (b != null) Destroy(b.gameObject);
            _boids.Clear();
        }

        // ── ISimulation ───────────────────────────────────────────────────────

        public void Play()  { _isPlaying = true; }
        public void Pause() { _isPlaying = false; }

        public void Step()
        {
            if (!_isPlaying) Tick(Time.fixedDeltaTime);
        }

        public void ResetSimulation()
        {
            _isPlaying = false;
            int prev = _boids.Count > 0 ? _boids.Count : startCount;
            ClearBoids();
            SpawnBoids(prev);
        }

        // ── Flocking (O(n²)) ──────────────────────────────────────────────────

        private void Tick(float dt)
        {
            int n = _boids.Count;

            for (int i = 0; i < n; i++)
            {
                Vector2 pos = _boids[i].transform.position;
                Vector2 vel = _boids[i].Velocity;

                Vector2 sep      = Vector2.zero;
                Vector2 aliSum   = Vector2.zero;
                Vector2 cohSum   = Vector2.zero;
                int     seenCount = 0;
                int     sepCount  = 0;

                for (int j = 0; j < n; j++)
                {
                    if (i == j) continue;
                    Vector2 oPos = _boids[j].transform.position;
                    Vector2 diff = pos - oPos;
                    float   dist = diff.magnitude;

                    if (dist < settings.perceptionRadius && dist > 0.001f)
                    {
                        aliSum += _boids[j].Velocity;
                        cohSum += oPos;
                        seenCount++;

                        if (dist < settings.separationRadius)
                        {
                            sep += diff / dist; // weight by proximity
                            sepCount++;
                        }
                    }
                }

                Vector2 steer = Vector2.zero;

                if (seenCount > 0)
                {
                    // Alignment: steer toward average heading of neighbors
                    steer += Steer(vel, aliSum / seenCount, settings.maxSpeed, settings.maxForce)
                             * settings.alignmentWeight;

                    // Cohesion: steer toward center of mass of neighbors
                    Vector2 toCenter = cohSum / seenCount - pos;
                    steer += Steer(vel, toCenter, settings.maxSpeed, settings.maxForce)
                             * settings.cohesionWeight;
                }

                if (sepCount > 0)
                {
                    // Separation: push away from close neighbors
                    steer += Steer(vel, sep, settings.maxSpeed, settings.maxForce)
                             * settings.separationWeight;
                }

                Vector2 newVel = Vector2.ClampMagnitude(vel + steer * dt, settings.maxSpeed);
                Vector2 newPos = (Vector2)_boids[i].transform.position + newVel * dt;

                // Wrap at world bounds
                float hw = settings.boundsHalfW, hh = settings.boundsHalfH;
                newPos.x = Mathf.Repeat(newPos.x + hw, hw * 2f) - hw;
                newPos.y = Mathf.Repeat(newPos.y + hh, hh * 2f) - hh;

                _nextPos[i] = newPos;
                _nextVel[i] = newVel;
            }

            // Apply after all reads are done (double-buffer pattern)
            for (int i = 0; i < n; i++)
                _boids[i].ApplyMotion(_nextPos[i], _nextVel[i]);
        }

        // desiredVelocity = dir(target) * maxSpeed → steeringForce = desired - current
        static Vector2 Steer(Vector2 vel, Vector2 desired, float maxSpeed, float maxForce)
        {
            if (desired.sqrMagnitude < 0.0001f) return Vector2.zero;
            return Vector2.ClampMagnitude(desired.normalized * maxSpeed - vel, maxForce);
        }

        void GrowBuffers(int n)
        {
            if (_nextPos.Length < n)
            {
                _nextPos = new Vector2[n];
                _nextVel = new Vector2[n];
            }
        }
    }
}
