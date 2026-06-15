using System.Collections.Generic;
using UnityEngine;
using BehaviorSimulation.Core;
using Random = UnityEngine.Random;

namespace BehaviorSimulation.Crowd
{
    public enum CrowdScenario { Bidirectional, Bottleneck, Crossflow }

    public class CrowdManager : MonoBehaviour, ISimulation
    {
        [SerializeField] private CrowdSettings    settings;
        [SerializeField] private CrowdDensityGrid densityGrid;
        [SerializeField] private LineRenderer     wallRendererBot;
        [SerializeField] private LineRenderer     wallRendererTop;
        [SerializeField] private Color colorA = new(0.35f, 0.60f, 0.95f);
        [SerializeField] private Color colorB = new(0.95f, 0.50f, 0.20f);

        readonly List<CrowdAgent>             _agents = new();
        readonly List<(Vector2 A, Vector2 B)> _walls  = new();

        Vector2[] _nextPos = System.Array.Empty<Vector2>();
        Vector2[] _nextVel = System.Array.Empty<Vector2>();

        CrowdScenario _scenario = CrowdScenario.Bidirectional;
        bool  _isPlaying;

        // ── Expansion state ───────────────────────────────────────────────────
        float   _panicMultiplier  = 1f;
        bool    _isPanic;
        int     _throughputCount;
        float   _throughputTimer;
        int     _densityFrame;
        float[] _prevX = System.Array.Empty<float>();   // for gap-crossing detection
        const float ThroughputWindow = 3f;

        public bool          IsPanic          => _isPanic;
        public float         Throughput       { get; private set; }
        public bool          IsDensityVisible => densityGrid && densityGrid.IsVisible;

        public void TogglePanic()
        {
            _isPanic         = !_isPanic;
            _panicMultiplier = _isPanic ? 2.5f : 1f;
        }

        public void ToggleDensity()
        {
            if (densityGrid) densityGrid.SetVisible(!densityGrid.IsVisible);
        }

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

        // ── Public ────────────────────────────────────────────────────────────

        public void SetScenario(CrowdScenario s)
        {
            _scenario = s;
            ResetSimulation();
        }

        // ── ISimulation ───────────────────────────────────────────────────────

        public void Play()  => _isPlaying = true;
        public void Pause() => _isPlaying = false;
        public void Step()  { if (!_isPlaying) Tick(Time.fixedDeltaTime); }

        public void ResetSimulation()
        {
            _isPlaying       = false;
            _isPanic         = false;
            _panicMultiplier = 1f;
            Throughput       = 0f;
            _throughputCount = 0;
            _throughputTimer = 0f;
            _prevX           = System.Array.Empty<float>();
            ClearAgents();
            BuildWalls();
            SpawnAgents();
        }

        // ── Simulation ────────────────────────────────────────────────────────

        void Tick(float dt)
        {
            int n = _agents.Count;
            if (n == 0) return;
            if (_nextPos.Length < n) { _nextPos = new Vector2[n]; _nextVel = new Vector2[n]; }

            // ── Init _prevX on first bottleneck tick ───────────────────────────
            if (_scenario == CrowdScenario.Bottleneck && _prevX.Length < n)
            {
                _prevX = new float[n];
                for (int i = 0; i < n; i++)
                    _prevX[i] = _agents[i].transform.position.x;
            }

            float r2 = settings.agentRadius * 2f;

            for (int i = 0; i < n; i++)
            {
                Vector2 pos  = _agents[i].transform.position;
                Vector2 vel  = _agents[i].Velocity;
                Vector2 goal = _agents[i].GoalPos;

                // ── Goal-seeking (desired velocity relaxation) ─────────────────
                Vector2 toGoal     = goal - pos;
                Vector2 desiredVel = toGoal.sqrMagnitude > 0.25f
                    ? toGoal.normalized * settings.desiredSpeed * _panicMultiplier
                    : Vector2.zero;
                Vector2 force = (desiredVel - vel) / settings.tau;

                // ── Agent–agent social repulsion (O(n²)) ──────────────────────
                for (int j = 0; j < n; j++)
                {
                    if (i == j) continue;
                    Vector2 diff = pos - (Vector2)_agents[j].transform.position;
                    float   dist = diff.magnitude;
                    if (dist < 0.001f) continue;

                    Vector2 nij = diff / dist;
                    force += nij * settings.agentA * Mathf.Exp((r2 - dist) / settings.agentB);

                    float overlap = r2 - dist;
                    if (overlap > 0f) force += nij * settings.bodyForce * overlap;
                }

                // ── Wall repulsion ────────────────────────────────────────────
                foreach (var (wa, wb) in _walls)
                {
                    Vector2 nearest = NearestOnSeg(pos, wa, wb);
                    Vector2 diff    = pos - nearest;
                    float   dist    = diff.magnitude;
                    if (dist < 0.001f) continue;

                    Vector2 niW = diff / dist;
                    force += niW * settings.wallA
                             * Mathf.Exp((settings.agentRadius - dist) / settings.wallB);

                    float overlap = settings.agentRadius - dist;
                    if (overlap > 0f) force += niW * settings.bodyForce * overlap;
                }

                // ── Integrate ─────────────────────────────────────────────────
                vel = Vector2.ClampMagnitude(vel + force * dt, settings.maxSpeed);
                float hw = settings.boundsHalfW - settings.agentRadius;
                float hh = settings.boundsHalfH - settings.agentRadius;
                _nextPos[i] = new Vector2(
                    Mathf.Clamp(pos.x + vel.x * dt, -hw, hw),
                    Mathf.Clamp(pos.y + vel.y * dt, -hh, hh));
                _nextVel[i] = vel;
            }

            // ── Count gap crossings BEFORE applying motion ────────────────────
            // Measure agents crossing x = bottleneckX + small offset left→right.
            // This captures actual bottleneck throughput independent of how fast
            // agents traverse the open right side after passing through the gap.
            if (_scenario == CrowdScenario.Bottleneck && _prevX.Length >= n)
            {
                float crossLine = settings.bottleneckX + 0.4f;
                for (int i = 0; i < n; i++)
                    if (_prevX[i] < crossLine && _nextPos[i].x >= crossLine)
                        _throughputCount++;
            }

            for (int i = 0; i < n; i++)
                _agents[i].ApplyMotion(_nextPos[i], _nextVel[i]);

            HandleGoals();

            // ── Update _prevX AFTER HandleGoals (captures any teleportation) ──
            if (_scenario == CrowdScenario.Bottleneck && _prevX.Length >= n)
                for (int i = 0; i < n; i++)
                    _prevX[i] = _agents[i].transform.position.x;

            // ── Throughput rolling window (Bottleneck only) ────────────────────
            if (_scenario == CrowdScenario.Bottleneck)
            {
                _throughputTimer += dt;
                if (_throughputTimer >= ThroughputWindow)
                {
                    Throughput        = _throughputCount / ThroughputWindow;
                    _throughputCount  = 0;
                    _throughputTimer -= ThroughputWindow;
                }
            }

            // ── Density heatmap (every 3 frames to save CPU) ──────────────────
            if (densityGrid && densityGrid.IsVisible && ++_densityFrame % 3 == 0)
                densityGrid.UpdateDensity(_agents);
        }

        void HandleGoals()
        {
            float hw = settings.boundsHalfW, hh = settings.boundsHalfH;
            const float margin = 1.5f;
            float r = settings.agentRadius;

            foreach (var a in _agents)
            {
                if (Vector2.Distance(a.transform.position, a.GoalPos) > 1.5f) continue;

                Vector2 pos;
                switch (_scenario)
                {
                    case CrowdScenario.Bidirectional:
                        if (a.GroupId == 0)
                        {
                            float y = Random.Range(-hh + margin, hh - margin);
                            pos = new Vector2(Random.Range(-hw + margin, -hw * 0.5f), y);
                            a.GoalPos = new Vector2(hw - r, y + Random.Range(-1.5f, 1.5f));
                        }
                        else
                        {
                            float y = Random.Range(-hh + margin, hh - margin);
                            pos = new Vector2(Random.Range(hw * 0.5f, hw - margin), y);
                            a.GoalPos = new Vector2(-hw + r, y + Random.Range(-1.5f, 1.5f));
                        }
                        a.transform.position = new Vector3(pos.x, pos.y, 0f);
                        a.Velocity = Vector2.zero;
                        break;

                    case CrowdScenario.Bottleneck:
                        pos = new Vector2(
                            Random.Range(-hw + margin, settings.bottleneckX - 2f),
                            Random.Range(-hh + margin, hh - margin));
                        a.transform.position = new Vector3(pos.x, pos.y, 0f);
                        a.Velocity = Vector2.zero;
                        break;

                    case CrowdScenario.Crossflow:
                        if (a.GroupId == 0)
                        {
                            float x = Random.Range(-hw + margin, hw - margin);
                            pos = new Vector2(x, Random.Range(-hh + margin, -hh * 0.4f));
                            a.GoalPos = new Vector2(x + Random.Range(-2f, 2f), hh - r);
                        }
                        else
                        {
                            float y = Random.Range(-hh + margin, hh - margin);
                            pos = new Vector2(Random.Range(-hw + margin, -hw * 0.4f), y);
                            a.GoalPos = new Vector2(hw - r, y + Random.Range(-2f, 2f));
                        }
                        a.transform.position = new Vector3(pos.x, pos.y, 0f);
                        a.Velocity = Vector2.zero;
                        break;
                }
            }
        }

        // ── Scene setup ───────────────────────────────────────────────────────

        void BuildWalls()
        {
            _walls.Clear();
            float hw = settings.boundsHalfW, hh = settings.boundsHalfH;

            // World boundary (4 sides)
            _walls.Add((new Vector2(-hw, -hh), new Vector2( hw, -hh)));
            _walls.Add((new Vector2(-hw,  hh), new Vector2( hw,  hh)));
            _walls.Add((new Vector2(-hw, -hh), new Vector2(-hw,  hh)));
            _walls.Add((new Vector2( hw, -hh), new Vector2( hw,  hh)));

            if (_scenario == CrowdScenario.Bottleneck)
            {
                float bx  = settings.bottleneckX;
                float gap = settings.bottleneckGap;
                _walls.Add((new Vector2(bx, -hh), new Vector2(bx, -gap)));
                _walls.Add((new Vector2(bx,  gap), new Vector2(bx,  hh)));
                UpdateWallRenderer(bx, gap, hh);
            }
            else
            {
                if (wallRendererBot) wallRendererBot.enabled = false;
                if (wallRendererTop) wallRendererTop.enabled = false;
            }
        }

        void SpawnAgents()
        {
            float hw = settings.boundsHalfW, hh = settings.boundsHalfH;
            const float margin = 1.0f;
            int half = settings.agentCount / 2;

            switch (_scenario)
            {
                case CrowdScenario.Bidirectional:
                    for (int i = 0; i < half; i++)
                    {
                        float y = Random.Range(-hh + margin, hh - margin);
                        var pos  = new Vector2(Random.Range(-hw + margin, -hw * 0.4f), y);
                        var goal = new Vector2(hw - settings.agentRadius, y + Random.Range(-1.5f, 1.5f));
                        MakeAgent(pos, goal, 0);
                    }
                    for (int i = 0; i < half; i++)
                    {
                        float y = Random.Range(-hh + margin, hh - margin);
                        var pos  = new Vector2(Random.Range(hw * 0.4f, hw - margin), y);
                        var goal = new Vector2(-hw + settings.agentRadius, y + Random.Range(-1.5f, 1.5f));
                        MakeAgent(pos, goal, 1);
                    }
                    break;

                case CrowdScenario.Bottleneck:
                    for (int i = 0; i < settings.agentCount; i++)
                    {
                        var pos  = new Vector2(
                            Random.Range(-hw + margin, settings.bottleneckX - 2f),
                            Random.Range(-hh + margin, hh - margin));
                        var goal = new Vector2(hw - settings.agentRadius, 0f);
                        MakeAgent(pos, goal, 0);
                    }
                    break;

                case CrowdScenario.Crossflow:
                    for (int i = 0; i < half; i++)
                    {
                        float x = Random.Range(-hw + margin, hw - margin);
                        var pos  = new Vector2(x, Random.Range(-hh + margin, -hh * 0.4f));
                        var goal = new Vector2(x + Random.Range(-1f, 1f), hh - settings.agentRadius);
                        MakeAgent(pos, goal, 0);
                    }
                    for (int i = 0; i < half; i++)
                    {
                        float y = Random.Range(-hh + margin, hh - margin);
                        var pos  = new Vector2(Random.Range(-hw + margin, -hw * 0.4f), y);
                        var goal = new Vector2(hw - settings.agentRadius, y + Random.Range(-1f, 1f));
                        MakeAgent(pos, goal, 1);
                    }
                    break;
            }
        }

        void MakeAgent(Vector2 pos, Vector2 goal, int group)
        {
            var go = new GameObject("Agent");
            go.transform.SetParent(transform, false);
            var sr    = go.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteFactory.Circle(14, 28f);  // 0.5 WU diameter
            sr.color  = group == 0 ? colorA : colorB;

            var a = go.AddComponent<CrowdAgent>();
            a.GroupId = group;
            a.GoalPos = goal;
            a.ApplyMotion(pos, Vector2.zero);
            _agents.Add(a);
        }

        void ClearAgents()
        {
            foreach (var a in _agents) if (a) Destroy(a.gameObject);
            _agents.Clear();
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        void UpdateWallRenderer(float bx, float gap, float hh)
        {
            if (wallRendererBot)
            {
                wallRendererBot.enabled       = true;
                wallRendererBot.positionCount = 2;
                wallRendererBot.SetPositions(new Vector3[]
                    { new(bx, -hh, 0f), new(bx, -gap, 0f) });
            }
            if (wallRendererTop)
            {
                wallRendererTop.enabled       = true;
                wallRendererTop.positionCount = 2;
                wallRendererTop.SetPositions(new Vector3[]
                    { new(bx, gap, 0f), new(bx, hh, 0f) });
            }
        }

        static Vector2 NearestOnSeg(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float   t  = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
            return a + ab * t;
        }
    }
}
