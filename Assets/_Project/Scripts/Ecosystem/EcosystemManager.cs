using System;
using System.Collections.Generic;
using UnityEngine;
using BehaviorSimulation.Core;
using Random = UnityEngine.Random;

namespace BehaviorSimulation.Ecosystem
{
    public class EcosystemManager : MonoBehaviour, ISimulation
    {
        [SerializeField] private PredatorPreySettings settings;
        [SerializeField] private Color preyColor = new(0.30f, 0.90f, 0.40f);
        [SerializeField] private Color predColor = new(0.90f, 0.25f, 0.25f);
        [SerializeField] private Color foodColor = new(0.40f, 0.75f, 0.20f);

        readonly List<EcosystemAgent> _prey  = new();
        readonly List<EcosystemAgent> _preds = new();
        readonly List<FoodPatch>      _food  = new();

        readonly List<(AgentType type, Vector2 pos, float energy)> _spawns = new();
        int _pendingPrey, _pendingPred;

        bool _isPlaying;

        public int PreyCount => _prey.Count;
        public int PredCount => _preds.Count;
        public event Action<int, int> OnPopulationChanged;

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
        public void Step()  { if (!_isPlaying) Tick(0.1f); }

        public void ResetSimulation()
        {
            _isPlaying = false;
            ClearAll();
            SpawnFood();
            SpawnInitial();
            OnPopulationChanged?.Invoke(PreyCount, PredCount);
        }

        // ── Simulation tick ───────────────────────────────────────────────────

        void Tick(float dt)
        {
            _pendingPrey = 0;
            _pendingPred = 0;

            TickPredators(dt);
            TickPrey(dt);
            foreach (var f in _food) f.Tick(dt);

            RemoveDead();
            FlushSpawns();

            OnPopulationChanged?.Invoke(PreyCount, PredCount);
        }

        void TickPredators(float dt)
        {
            foreach (var pred in _preds)
            {
                if (!pred.IsAlive) continue;

                Vector2 pos = pred.transform.position;
                Vector2 vel = pred.Velocity;

                // Find nearest living prey within hunt radius
                EcosystemAgent target = null;
                float best = settings.predHuntRadius;
                foreach (var p in _prey)
                {
                    if (!p.IsAlive) continue;
                    float d = WrappedDist(pos, p.transform.position);
                    if (d < best) { best = d; target = p; }
                }

                Vector2 steer;
                if (target != null)
                {
                    if (best <= settings.predEatRadius)
                    {
                        target.IsAlive = false;
                        pred.Energy = Mathf.Min(pred.Energy + settings.predEatGain,
                                                settings.predReproduceE * 1.2f);
                    }
                    steer = SteerTo(vel,
                        WrappedDir(pos, target.transform.position),
                        settings.predSpeed, settings.predMaxForce);
                }
                else
                {
                    float a = pred.WanderAngle;
                    steer = Wander(vel, ref a, settings.predSpeed, settings.predMaxForce);
                    pred.WanderAngle = a;
                }

                vel = Vector2.ClampMagnitude(vel + steer * dt, settings.predSpeed);
                pred.ApplyMotion(Wrap(pos + vel * dt), vel);

                pred.Energy -= settings.predEnergyDecay * dt;
                pred.ReproductionCooldown -= dt;

                if (pred.Energy <= 0f)
                    pred.IsAlive = false;
                else if (pred.Energy >= settings.predReproduceE
                         && pred.ReproductionCooldown <= 0f
                         && _preds.Count + _pendingPred < settings.predMax)
                {
                    pred.Energy *= 0.5f;
                    pred.ReproductionCooldown = settings.predReproduceCool;
                    _spawns.Add((AgentType.Predator, pos, settings.predEnergyStart * 0.5f));
                    _pendingPred++;
                }
            }
        }

        void TickPrey(float dt)
        {
            foreach (var prey in _prey)
            {
                if (!prey.IsAlive) continue;

                Vector2 pos = prey.transform.position;
                Vector2 vel = prey.Velocity;

                // Nearest predator (wrap-aware)
                float predDist = float.MaxValue;
                Vector2 nearestPredPos = default;
                foreach (var pred in _preds)
                {
                    if (!pred.IsAlive) continue;
                    float d = WrappedDist(pos, pred.transform.position);
                    if (d < predDist) { predDist = d; nearestPredPos = pred.transform.position; }
                }

                bool fleeing = predDist <= settings.preyFleeRadius;
                // Prey get a speed burst when fleeing so they can outrun predators.
                float maxSpd = fleeing
                    ? settings.preySpeed * settings.preyFleeSpeedMult
                    : settings.preySpeed;

                Vector2 steer;
                if (fleeing)
                {
                    steer = SteerTo(vel,
                        -WrappedDir(pos, nearestPredPos),   // flee = opposite direction
                        maxSpd, settings.preyMaxForce * settings.preyFleeSpeedMult);
                }
                else
                {
                    // Seek nearest available food (wrap-aware)
                    FoodPatch nearestFood = null;
                    float bestFood = float.MaxValue;
                    foreach (var f in _food)
                    {
                        if (!f.IsAvailable) continue;
                        float d = WrappedDist(pos, f.transform.position);
                        if (d < bestFood) { bestFood = d; nearestFood = f; }
                    }

                    if (nearestFood != null && bestFood <= settings.foodEatRadius)
                    {
                        nearestFood.Eat(settings.foodRegrowTime);
                        prey.Energy = Mathf.Min(prey.Energy + settings.foodEatGain,
                                                settings.preyReproduceE * 1.2f);
                        steer = Vector2.zero;
                    }
                    else if (nearestFood != null)
                    {
                        steer = SteerTo(vel,
                            WrappedDir(pos, nearestFood.transform.position),
                            settings.preySpeed, settings.preyMaxForce);
                    }
                    else
                    {
                        float a = prey.WanderAngle;
                        steer = Wander(vel, ref a, settings.preySpeed, settings.preyMaxForce);
                        prey.WanderAngle = a;
                    }
                }

                vel = Vector2.ClampMagnitude(vel + steer * dt, maxSpd);
                prey.ApplyMotion(Wrap(pos + vel * dt), vel);

                prey.Energy -= settings.preyEnergyDecay * dt;
                prey.ReproductionCooldown -= dt;

                if (prey.Energy <= 0f)
                    prey.IsAlive = false;
                else if (prey.Energy >= settings.preyReproduceE
                         && prey.ReproductionCooldown <= 0f
                         && _prey.Count + _pendingPrey < settings.preyMax)
                {
                    prey.Energy *= 0.5f;
                    prey.ReproductionCooldown = settings.preyReproduceCool;
                    _spawns.Add((AgentType.Prey, pos, settings.preyEnergyStart * 0.5f));
                    _pendingPrey++;
                }
            }
        }

        // ── Lifecycle helpers ─────────────────────────────────────────────────

        void RemoveDead()
        {
            for (int i = _prey.Count - 1; i >= 0; i--)
                if (!_prey[i].IsAlive) { Destroy(_prey[i].gameObject); _prey.RemoveAt(i); }
            for (int i = _preds.Count - 1; i >= 0; i--)
                if (!_preds[i].IsAlive) { Destroy(_preds[i].gameObject); _preds.RemoveAt(i); }
        }

        void FlushSpawns()
        {
            foreach (var (type, pos, energy) in _spawns)
            {
                Vector2 p = Wrap(pos + (Vector2)Random.insideUnitCircle * 1.5f);
                Vector2 v = Random.insideUnitCircle.normalized;
                if (type == AgentType.Prey)
                    MakePrey(p, v * settings.preySpeed * 0.5f, energy);
                else
                    MakePredator(p, v * settings.predSpeed * 0.5f, energy);
            }
            _spawns.Clear();
        }

        void ClearAll()
        {
            foreach (var a in _prey)  if (a) Destroy(a.gameObject);
            foreach (var a in _preds) if (a) Destroy(a.gameObject);
            foreach (var f in _food)  if (f) Destroy(f.gameObject);
            _prey.Clear(); _preds.Clear(); _food.Clear(); _spawns.Clear();
        }

        void SpawnFood()
        {
            for (int i = 0; i < settings.foodCount; i++)
            {
                var go = new GameObject("Food");
                go.transform.SetParent(transform, false);
                go.transform.position = (Vector3)RandPos() + Vector3.forward * 0.05f;
                var sr    = go.AddComponent<SpriteRenderer>();
                sr.sprite = SpriteFactory.Circle(8, 40f);
                sr.color  = foodColor;
                _food.Add(go.AddComponent<FoodPatch>());
            }
        }

        void SpawnInitial()
        {
            for (int i = 0; i < settings.preyStart; i++)
                MakePrey(RandPos(),
                         Random.insideUnitCircle.normalized * settings.preySpeed * 0.5f,
                         settings.preyEnergyStart);
            for (int i = 0; i < settings.predStart; i++)
                MakePredator(RandPos(),
                             Random.insideUnitCircle.normalized * settings.predSpeed * 0.5f,
                             settings.predEnergyStart);
        }

        EcosystemAgent MakePrey(Vector2 pos, Vector2 vel, float energy)
        {
            var go = new GameObject("Prey");
            go.transform.SetParent(transform, false);
            var sr    = go.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteFactory.Circle(16, 32f);
            sr.color  = preyColor;
            var a = go.AddComponent<EcosystemAgent>();
            a.Type   = AgentType.Prey;
            a.Energy = energy;
            a.IsAlive = true;
            a.ReproductionCooldown = Random.Range(0f, settings.preyReproduceCool);
            a.WanderAngle = Random.Range(0f, Mathf.PI * 2f);
            a.ApplyMotion(pos, vel);
            _prey.Add(a);
            return a;
        }

        EcosystemAgent MakePredator(Vector2 pos, Vector2 vel, float energy)
        {
            var go = new GameObject("Predator");
            go.transform.SetParent(transform, false);
            var sr    = go.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteFactory.Arrow(20, 20f);
            sr.color  = predColor;
            var a = go.AddComponent<EcosystemAgent>();
            a.Type   = AgentType.Predator;
            a.Energy = energy;
            a.IsAlive = true;
            a.ReproductionCooldown = Random.Range(0f, settings.predReproduceCool);
            a.WanderAngle = Random.Range(0f, Mathf.PI * 2f);
            a.ApplyMotion(pos, vel);
            _preds.Add(a);
            return a;
        }

        // ── Steering helpers ──────────────────────────────────────────────────

        // Steer toward an already-computed direction (use WrappedDir for wrap-safe inputs).
        static Vector2 SteerTo(Vector2 vel, Vector2 dir, float speed, float force)
        {
            return dir.sqrMagnitude < 0.001f ? Vector2.zero
                   : Vector2.ClampMagnitude(dir.normalized * speed - vel, force);
        }

        static Vector2 Wander(Vector2 vel, ref float angle, float speed, float force)
        {
            angle += Random.Range(-0.6f, 0.6f);
            return Vector2.ClampMagnitude(
                new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed - vel, force);
        }

        // ── World helpers ─────────────────────────────────────────────────────

        // Shortest-path direction vector across wrap boundaries.
        Vector2 WrappedDir(Vector2 from, Vector2 to)
        {
            float hw = settings.boundsHalfW, hh = settings.boundsHalfH;
            var d = to - from;
            if (d.x >  hw) d.x -= hw * 2f;
            if (d.x < -hw) d.x += hw * 2f;
            if (d.y >  hh) d.y -= hh * 2f;
            if (d.y < -hh) d.y += hh * 2f;
            return d;
        }

        float WrappedDist(Vector2 a, Vector2 b) => WrappedDir(a, b).magnitude;

        Vector2 Wrap(Vector2 p)
        {
            float hw = settings.boundsHalfW, hh = settings.boundsHalfH;
            p.x = Mathf.Repeat(p.x + hw, hw * 2f) - hw;
            p.y = Mathf.Repeat(p.y + hh, hh * 2f) - hh;
            return p;
        }

        Vector2 RandPos() =>
            new(Random.Range(-settings.boundsHalfW * 0.9f, settings.boundsHalfW * 0.9f),
                Random.Range(-settings.boundsHalfH * 0.9f, settings.boundsHalfH * 0.9f));
    }
}
