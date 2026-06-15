using System.Collections.Generic;
using UnityEngine;
using Unity.Profiling;
using BehaviorSimulation.Core;
using Random = UnityEngine.Random;

namespace BehaviorSimulation.Optimization
{
    // Boids flocking with two runtime-toggleable optimizations:
    //   1. Spatial grid : O(n·k) neighbor lookup instead of O(n²) brute force
    //   2. Object pool  : 1000 agents pre-allocated; count changes produce zero GC
    public sealed class OptimizationManager : MonoBehaviour, ISimulation
    {
        [SerializeField] OptimizationSettings settings;
        [SerializeField] GridOverlay          gridOverlay;
        [SerializeField] Color agentColor = new(0.35f, 0.90f, 0.55f);

        // ── Object pool ───────────────────────────────────────────────────────
        const int MaxAgents = 1000;

        readonly GameObject[] _pool   = new GameObject[MaxAgents];
        readonly Transform[]  _xforms = new Transform[MaxAgents];

        // ── Physics state — parallel arrays (no GetComponent per tick) ─────────
        readonly Vector2[] _pos  = new Vector2[MaxAgents];
        readonly Vector2[] _vel  = new Vector2[MaxAgents];
        readonly Vector2[] _nPos = new Vector2[MaxAgents];
        readonly Vector2[] _nVel = new Vector2[MaxAgents];

        // ── Spatial grid ──────────────────────────────────────────────────────
        readonly SpatialGrid _grid   = new();
        readonly List<int>   _nearby = new(64);

        // ── Profiler markers (visible in Window → Analysis → Profiler) ────────
        static readonly ProfilerMarker s_tick      = new(ProfilerCategory.Scripts, "Opt.Tick");
        static readonly ProfilerMarker s_neighbors = new(ProfilerCategory.Scripts, "Opt.Neighbors");
        static readonly ProfilerMarker s_apply     = new(ProfilerCategory.Scripts, "Opt.Apply");

        bool _isPlaying;
        int  _count;

        // ── Public stats ──────────────────────────────────────────────────────
        public bool  UseSpatialGrid       { get; private set; }
        public bool  IsGridOverlayVisible => gridOverlay && gridOverlay.IsVisible;
        public int   ActiveCount          => _count;
        public int   GridCells            => UseSpatialGrid ? _grid.ActiveCells : 0;
        public float TickMs               { get; private set; }
        public float FPS                  { get; private set; }

        float _fpsTimer;
        int   _fpsFrames;
        float _tickAccum;
        int   _tickSamples;

        // ── Unity ─────────────────────────────────────────────────────────────

        void Awake()
        {
            PrewarmPool();  // before any Start() so SetCount() is safe immediately
        }

        void Start()
        {
            SimulationController.Instance?.Register(this);
            ResetSimulation();
            gridOverlay?.Init(settings.perceptionRadius);
        }

        void Update()
        {
            _fpsFrames++;
            _fpsTimer += Time.unscaledDeltaTime;
            if (_fpsTimer >= 0.5f)
            {
                FPS        = _fpsFrames / _fpsTimer;
                _fpsFrames = 0;
                _fpsTimer  = 0f;
            }

            if (!_isPlaying) return;

            float t0 = Time.realtimeSinceStartup;
            using (s_tick.Auto()) Tick(Time.deltaTime);

            _tickAccum += (Time.realtimeSinceStartup - t0) * 1000f;
            if (++_tickSamples >= 10)
            {
                TickMs       = _tickAccum / _tickSamples;
                _tickAccum   = 0f;
                _tickSamples = 0;
            }
        }

        // ── ISimulation ───────────────────────────────────────────────────────

        public void Play()  => _isPlaying = true;
        public void Pause() => _isPlaying = false;

        public void Step()
        {
            if (!_isPlaying) using (s_tick.Auto()) Tick(Time.fixedDeltaTime);
        }

        public void ResetSimulation()
        {
            _isPlaying = false;
            _count     = Mathf.Clamp(settings.agentCount, 1, MaxAgents);
            RandomizeState(_count);
            SetPoolActive(_count);
        }

        // ── Public controls ───────────────────────────────────────────────────

        public void SetCount(int n)
        {
            n = Mathf.Clamp(n, 1, MaxAgents);
            if (n > _count)
            {
                float hw = settings.boundsHalfW * 0.9f, hh = settings.boundsHalfH * 0.9f;
                for (int i = _count; i < n; i++)
                {
                    _pos[i] = new Vector2(Random.Range(-hw, hw), Random.Range(-hh, hh));
                    _vel[i] = Random.insideUnitCircle.normalized
                              * settings.maxSpeed * Random.Range(0.3f, 0.7f);
                    ApplyTransform(i);
                }
            }
            _count = n;
            SetPoolActive(n);
        }

        public void ToggleSpatialGrid() => UseSpatialGrid = !UseSpatialGrid;

        public void ToggleGridOverlay()
        {
            if (!gridOverlay) return;
            bool next = !gridOverlay.IsVisible;
            gridOverlay.SetVisible(next);
            if (next) gridOverlay.Init(settings.perceptionRadius);
        }

        // ── Tick ──────────────────────────────────────────────────────────────

        void Tick(float dt)
        {
            int   n    = _count;
            float pR   = settings.perceptionRadius;
            float pR2  = pR * pR;
            float sR2  = settings.separationRadius * settings.separationRadius;

            if (UseSpatialGrid)
            {
                _grid.Reset(pR);
                for (int i = 0; i < n; i++)
                    _grid.Insert(i, _pos[i].x, _pos[i].y);
            }

            using (s_neighbors.Auto())
            {
                for (int i = 0; i < n; i++)
                {
                    Vector2 pos = _pos[i];
                    Vector2 vel = _vel[i];

                    Vector2 sep    = Vector2.zero;
                    Vector2 aliSum = Vector2.zero;
                    Vector2 cohSum = Vector2.zero;
                    int seen = 0, sepCnt = 0;

                    if (UseSpatialGrid)
                        _grid.Query(pos.x, pos.y, _nearby);
                    else
                    {
                        _nearby.Clear();
                        for (int j = 0; j < n; j++) _nearby.Add(j);
                    }

                    for (int k = 0; k < _nearby.Count; k++)
                    {
                        int j = _nearby[k];
                        if (j == i) continue;
                        Vector2 diff  = pos - _pos[j];
                        float   dist2 = diff.sqrMagnitude;
                        if (dist2 >= pR2 || dist2 < 0.0001f) continue;

                        aliSum += _vel[j];
                        cohSum += _pos[j];
                        seen++;

                        if (dist2 < sR2)
                        {
                            sep += diff / Mathf.Sqrt(dist2);
                            sepCnt++;
                        }
                    }

                    Vector2 steer = Vector2.zero;
                    if (seen > 0)
                    {
                        steer += Steer(vel, aliSum / seen,            settings) * settings.alignmentWeight;
                        steer += Steer(vel, cohSum / seen - pos,      settings) * settings.cohesionWeight;
                    }
                    if (sepCnt > 0)
                        steer += Steer(vel, sep,                      settings) * settings.separationWeight;

                    _nVel[i] = Vector2.ClampMagnitude(vel + steer * dt, settings.maxSpeed);

                    // Wrap-around
                    float hw = settings.boundsHalfW, hh = settings.boundsHalfH;
                    float nx = Mathf.Repeat(pos.x + _nVel[i].x * dt + hw, hw * 2f) - hw;
                    float ny = Mathf.Repeat(pos.y + _nVel[i].y * dt + hh, hh * 2f) - hh;
                    _nPos[i] = new Vector2(nx, ny);
                }
            }

            using (s_apply.Auto())
            {
                for (int i = 0; i < n; i++)
                {
                    _pos[i] = _nPos[i];
                    _vel[i] = _nVel[i];
                    ApplyTransform(i);
                }
            }

            if (gridOverlay && gridOverlay.IsVisible && Time.frameCount % 2 == 0)
                gridOverlay.UpdateOverlay(_pos, n, settings.perceptionRadius);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        void PrewarmPool()
        {
            var sprite = SpriteFactory.Arrow(16, 28f);
            for (int i = 0; i < MaxAgents; i++)
            {
                var go = new GameObject($"Opt_{i:000}");
                go.transform.SetParent(transform, false);
                var sr    = go.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                sr.color  = agentColor;
                _pool[i]   = go;
                _xforms[i] = go.transform;
                go.SetActive(false);
            }
        }

        void RandomizeState(int n)
        {
            float hw = settings.boundsHalfW * 0.9f, hh = settings.boundsHalfH * 0.9f;
            for (int i = 0; i < n; i++)
            {
                _pos[i] = new Vector2(Random.Range(-hw, hw), Random.Range(-hh, hh));
                _vel[i] = Random.insideUnitCircle.normalized
                          * settings.maxSpeed * Random.Range(0.3f, 0.7f);
                ApplyTransform(i);
            }
        }

        void SetPoolActive(int n)
        {
            for (int i = 0; i < MaxAgents; i++)
                _pool[i].SetActive(i < n);
        }

        void ApplyTransform(int i)
        {
            _xforms[i].position = new Vector3(_pos[i].x, _pos[i].y, 0f);
            if (_vel[i].sqrMagnitude > 0.01f)
                _xforms[i].rotation = Quaternion.Euler(0f, 0f,
                    Mathf.Atan2(_vel[i].y, _vel[i].x) * Mathf.Rad2Deg - 90f);
        }

        static Vector2 Steer(Vector2 vel, Vector2 desired, OptimizationSettings s)
        {
            if (desired.sqrMagnitude < 0.0001f) return Vector2.zero;
            return Vector2.ClampMagnitude(desired.normalized * s.maxSpeed - vel, s.maxForce);
        }
    }
}
