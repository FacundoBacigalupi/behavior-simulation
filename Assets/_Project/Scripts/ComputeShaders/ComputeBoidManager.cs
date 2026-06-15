using System.Runtime.InteropServices;
using UnityEngine;
using Unity.Profiling;
using BehaviorSimulation.Core;
using Random = UnityEngine.Random;

namespace BehaviorSimulation.ComputeShaders
{
    // Boids simulation running entirely on the GPU:
    //   • ComputeShader dispatches one thread per boid (64 threads/group)
    //   • Ping-pong ComputeBuffers avoid read/write hazards
    //   • DrawMeshInstancedProcedural renders all boids in a single draw call
    //   • Zero CPU-side physics, zero per-agent GameObjects, zero GC
    [StructLayout(LayoutKind.Sequential)]
    struct BoidGPUData
    {
        public Vector2 pos;   // 8 bytes
        public Vector2 vel;   // 8 bytes
        // total = 16 bytes — matches HLSL struct
    }

    public sealed class ComputeBoidManager : MonoBehaviour, ISimulation
    {
        [SerializeField] ComputeShader       computeShader;
        [SerializeField] Material            instanceMaterial;
        [SerializeField] ComputeBoidSettings settings;

        ComputeBuffer _bufA, _bufB;
        int           _kernel;
        Mesh          _boidMesh;
        Bounds        _drawBounds;
        bool          _isPlaying;
        int           _pingPong;   // 0 = A→in B→out, 1 = B→in A→out
        int           _count;

        static readonly ProfilerMarker s_dispatch = new(ProfilerCategory.Scripts, "GPU.Dispatch");
        static readonly ProfilerMarker s_draw     = new(ProfilerCategory.Scripts, "GPU.Draw");

        // ── Public stats ──────────────────────────────────────────────────────
        public int   BoidCount => _count;
        public float FPS       { get; private set; }
        public float TickMs    { get; private set; }

        float _fpsTimer;
        int   _fpsFrames;
        float _tickAccum;
        int   _tickSamples;

        // ── Unity ─────────────────────────────────────────────────────────────

        void Start()
        {
            if (!SystemInfo.supportsComputeShaders)
            {
                Debug.LogError("[ComputeBoids] Compute shaders not supported on this device.");
                enabled = false;
                return;
            }

            SimulationController.Instance?.Register(this);

            _kernel     = computeShader.FindKernel("CSBoids");
            _boidMesh   = MakeArrowMesh();
            _drawBounds = new Bounds(Vector3.zero, Vector3.one * 200f);
            _count      = Mathf.Clamp(settings.agentCount, 1, 20000);

            AllocBuffers(_count);
            FillRandom(_count);
        }

        void OnDestroy()
        {
            _bufA?.Release();
            _bufB?.Release();
        }

        void Update()
        {
            // FPS counter
            _fpsFrames++;
            _fpsTimer += Time.unscaledDeltaTime;
            if (_fpsTimer >= 0.5f)
            {
                FPS        = _fpsFrames / _fpsTimer;
                _fpsFrames = 0;
                _fpsTimer  = 0f;
            }

            if (_isPlaying)
            {
                float t0 = Time.realtimeSinceStartup;
                using (s_dispatch.Auto()) Dispatch(Time.deltaTime);
                _tickAccum += (Time.realtimeSinceStartup - t0) * 1000f;
                if (++_tickSamples >= 10)
                {
                    TickMs       = _tickAccum / _tickSamples;
                    _tickAccum   = 0f;
                    _tickSamples = 0;
                }
            }

            // _pingPong==0 → last dispatch wrote to _bufA; _pingPong==1 → wrote to _bufB
            ComputeBuffer renderBuf = _pingPong == 0 ? _bufA : _bufB;
            using (s_draw.Auto())
            {
                // SetGlobalBuffer is the only reliable way to bind a StructuredBuffer
                // for DrawMeshInstancedProcedural in Unity 6 + URP (MPB.SetBuffer is broken)
                Shader.SetGlobalBuffer("_BoidBuffer", renderBuf);
                Graphics.DrawMeshInstancedProcedural(
                    _boidMesh, 0, instanceMaterial, _drawBounds, _count);
            }
        }

        // ── ISimulation ───────────────────────────────────────────────────────

        public void Play()  => _isPlaying = true;
        public void Pause() => _isPlaying = false;

        public void Step()
        {
            if (!_isPlaying) Dispatch(Time.fixedDeltaTime);
        }

        public void ResetSimulation()
        {
            _isPlaying = false;
            _pingPong  = 0;
            FillRandom(_count);
        }

        // ── Public controls ───────────────────────────────────────────────────

        public void SetCount(int n)
        {
            n      = Mathf.Clamp(n, 1, 20000);
            _count = n;
            // Only reallocate when larger than current capacity
            if (_bufA == null || _bufA.count < n)
            {
                AllocBuffers(n);
                FillRandom(n);
            }
        }

        // ── GPU dispatch ──────────────────────────────────────────────────────

        void Dispatch(float dt)
        {
            ComputeBuffer inBuf  = _pingPong == 0 ? _bufA : _bufB;
            ComputeBuffer outBuf = _pingPong == 0 ? _bufB : _bufA;

            computeShader.SetBuffer(_kernel, "_BoidsIn",  inBuf);
            computeShader.SetBuffer(_kernel, "_BoidsOut", outBuf);
            computeShader.SetInt  ("_Count",             _count);
            computeShader.SetFloat("_DeltaTime",         dt);
            computeShader.SetFloat("_MaxSpeed",          settings.maxSpeed);
            computeShader.SetFloat("_MaxForce",          settings.maxForce);
            computeShader.SetFloat("_PerceptionRadius",  settings.perceptionRadius);
            computeShader.SetFloat("_SeparationRadius",  settings.separationRadius);
            computeShader.SetFloat("_SeparationWeight",  settings.separationWeight);
            computeShader.SetFloat("_AlignmentWeight",   settings.alignmentWeight);
            computeShader.SetFloat("_CohesionWeight",    settings.cohesionWeight);
            computeShader.SetFloat("_BoundsHalfW",       settings.boundsHalfW);
            computeShader.SetFloat("_BoundsHalfH",       settings.boundsHalfH);

            int groups = Mathf.CeilToInt(_count / 64f);
            computeShader.Dispatch(_kernel, groups, 1, 1);

            _pingPong = 1 - _pingPong;
        }

        // ── Buffer management ─────────────────────────────────────────────────

        void AllocBuffers(int n)
        {
            _bufA?.Release();
            _bufB?.Release();
            int stride = Marshal.SizeOf<BoidGPUData>();  // 16 bytes
            _bufA = new ComputeBuffer(n, stride);
            _bufB = new ComputeBuffer(n, stride);
            _pingPong = 0;
        }

        void FillRandom(int n)
        {
            var data = new BoidGPUData[n];
            float hw = settings.boundsHalfW * 0.9f, hh = settings.boundsHalfH * 0.9f;
            for (int i = 0; i < n; i++)
            {
                data[i].pos = new Vector2(Random.Range(-hw, hw), Random.Range(-hh, hh));
                var dir     = Random.insideUnitCircle.normalized;
                data[i].vel = dir * settings.maxSpeed * Random.Range(0.3f, 0.7f);
            }
            _bufA.SetData(data);
            _bufB.SetData(data);
        }

        // ── Mesh ─────────────────────────────────────────────────────────────

        static Mesh MakeArrowMesh()
        {
            var mesh = new Mesh { name = "BoidArrow" };
            // Triangle pointing +Y (tip at top), matching Boid.cs -90° rotation
            mesh.vertices = new Vector3[]
            {
                new( 0f,    0.5f, 0f),
                new(-0.28f, -0.5f, 0f),
                new( 0.28f, -0.5f, 0f),
            };
            mesh.triangles = new[] { 0, 2, 1 };   // CW so Cull Off shows both sides
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
