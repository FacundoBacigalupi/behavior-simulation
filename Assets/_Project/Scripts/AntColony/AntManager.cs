using System;
using System.Collections.Generic;
using UnityEngine;
using BehaviorSimulation.Core;
using Random = UnityEngine.Random;

namespace BehaviorSimulation.AntColony
{
    public class AntManager : MonoBehaviour, ISimulation
    {
        [SerializeField] private AntSettings  settings;
        [SerializeField] private PheromoneGrid grid;
        [SerializeField] private FoodSource[] foodSources;
        [SerializeField] private Vector2      nestPosition = Vector2.zero;

        readonly List<Ant> _ants = new();
        bool _isPlaying;

        public int FoodCollected { get; private set; }
        public int AntCount      => _ants.Count;
        public event Action<int> OnFoodCollected;  // passes total collected

        // ── Unity ─────────────────────────────────────────────────────────────

        void Start()
        {
            SimulationController.Instance?.Register(this);
            ResetSimulation();
        }

        void Update()
        {
            if (_isPlaying) Tick(Time.deltaTime);
        }

        // ── ISimulation ───────────────────────────────────────────────────────

        public void Play()  => _isPlaying = true;
        public void Pause() => _isPlaying = false;
        public void Step()  { if (!_isPlaying) Tick(0.05f); }

        public void ResetSimulation()
        {
            _isPlaying    = false;
            FoodCollected = 0;
            ClearAnts();
            grid?.Clear();
            ResetFoodSources();
            SpawnAnts();
            OnFoodCollected?.Invoke(0);
        }

        // ── Simulation ────────────────────────────────────────────────────────

        void Tick(float dt)
        {
            foreach (var ant in _ants) TickAnt(ant, dt);
            grid?.Tick(dt);
        }

        void TickAnt(Ant ant, float dt)
        {
            Vector2 pos = ant.transform.position;
            float   dir = ant.Direction;

            bool seekFood = ant.State == AntState.Searching;

            // ── Sensor sampling (3 directions) ────────────────────────────────
            float sa = settings.sensorAngle * Mathf.Deg2Rad;
            Vector2 fP = pos + D(dir)      * settings.sensorDist;
            Vector2 lP = pos + D(dir - sa) * settings.sensorDist;
            Vector2 rP = pos + D(dir + sa) * settings.sensorDist;

            float fv = grid.Sample(fP.x, fP.y, seekFood);
            float lv = grid.Sample(lP.x, lP.y, seekFood);
            float rv = grid.Sample(rP.x, rP.y, seekFood);

            // ── Steering ──────────────────────────────────────────────────────
            if      (lv > fv && lv > rv) dir -= settings.turnSpeed * dt;
            else if (rv > fv && rv > lv) dir += settings.turnSpeed * dt;
            // else forward is best — go straight

            // Always-present random noise prevents lock-in
            dir += Random.Range(-1f, 1f) * settings.wanderNoise * dt;

            // ── Move ──────────────────────────────────────────────────────────
            Vector2 next = pos + D(dir) * settings.antSpeed * dt;

            // Bounce off world walls (with tiny random perturbation to break symmetry)
            float hw = settings.boundsHalfW, hh = settings.boundsHalfH;
            if (next.x >  hw) { next.x =  hw; dir = Mathf.PI - dir + Random.Range(-0.2f, 0.2f); }
            if (next.x < -hw) { next.x = -hw; dir = Mathf.PI - dir + Random.Range(-0.2f, 0.2f); }
            if (next.y >  hh) { next.y =  hh; dir = -dir + Random.Range(-0.2f, 0.2f); }
            if (next.y < -hh) { next.y = -hh; dir = -dir + Random.Range(-0.2f, 0.2f); }

            // ── Deposit pheromone at current cell ─────────────────────────────
            // Searching → lay nest-trail (so carrying ants follow it home)
            // Carrying  → lay food-trail (so searching ants follow it to food)
            grid.Deposit(pos.x, pos.y, !seekFood, settings.depositAmount * dt);

            // ── Interactions ──────────────────────────────────────────────────
            if (ant.State == AntState.Searching)
            {
                foreach (var src in foodSources)
                {
                    if (src == null) continue;
                    if (src.TryPickup(next, settings.pickupRadius))
                    {
                        ant.SetState(AntState.Carrying);
                        dir += Mathf.PI;   // turn around immediately
                        break;
                    }
                }
            }
            else
            {
                if (Vector2.Distance(next, nestPosition) <= settings.nestRadius)
                {
                    FoodCollected++;
                    ant.SetState(AntState.Searching);
                    dir += Mathf.PI;
                    OnFoodCollected?.Invoke(FoodCollected);
                }
            }

            ant.ApplyMotion(next, dir);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        void SpawnAnts()
        {
            for (int i = 0; i < settings.antCount; i++)
            {
                float initDir = Random.Range(0f, Mathf.PI * 2f);
                Vector2 p = nestPosition + D(initDir) * Random.Range(0f, settings.nestRadius * 0.8f);

                var go = new GameObject("Ant");
                go.transform.SetParent(transform, false);
                var sr    = go.AddComponent<SpriteRenderer>();
                sr.sprite = SpriteFactory.Arrow(8, 32f);  // 0.25 WU tiny arrow
                sr.sortingOrder = 0;

                var ant = go.AddComponent<Ant>();
                ant.ApplyMotion(p, initDir);
                _ants.Add(ant);
            }
        }

        void ClearAnts()
        {
            foreach (var a in _ants) if (a) Destroy(a.gameObject);
            _ants.Clear();
        }

        void ResetFoodSources()
        {
            foreach (var src in foodSources)
                src?.Reinit(settings.foodPerSource);
        }

        static Vector2 D(float angle) => new(Mathf.Cos(angle), Mathf.Sin(angle));
    }
}
