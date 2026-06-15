using System;
using UnityEngine;
using BehaviorSimulation.Core;

namespace BehaviorSimulation.GameOfLife
{
    // Holds the grid state using two bool[] buffers (current + next) so the
    // rules can read the current state while writing the next without interference.
    public class LifeGrid : MonoBehaviour, ISimulation
    {
        [Header("Grid Size")]
        [SerializeField] private int width = 80;
        [SerializeField] private int height = 60;

        [Header("Simulation")]
        [SerializeField, Range(1f, 30f)] private float ticksPerSecond = 5f;
        [SerializeField, Range(0f, 1f)] private float randomFillDensity = 0.35f;

        public int Width => width;
        public int Height => height;
        public int Generation { get; private set; }
        public int AliveCount { get; private set; }

        // Fired after every state change so the renderer can redraw.
        public event Action OnGridChanged;

        private bool[] _current;
        private bool[] _next;
        private bool _isPlaying;
        private float _timer;

        private void Awake()
        {
            _current = new bool[width * height];
            _next = new bool[width * height];
        }

        private void Start()
        {
            SimulationController.Instance?.Register(this);
            // Speed slider maps 1-30 ticks/s
            if (SimulationController.Instance != null)
                SimulationController.Instance.OnSpeedChanged += SetTicksPerSecond;
        }

        private void OnDestroy()
        {
            if (SimulationController.Instance != null)
                SimulationController.Instance.OnSpeedChanged -= SetTicksPerSecond;
        }

        private void Update()
        {
            if (!_isPlaying) return;
            _timer += Time.deltaTime;
            float interval = 1f / ticksPerSecond;
            while (_timer >= interval)
            {
                _timer -= interval;
                Tick();
            }
        }

        // --- Public API ---

        public bool GetCell(int x, int y) => _current[y * width + x];

        public void SetCell(int x, int y, bool alive)
        {
            if (!InBounds(x, y)) return;
            _current[y * width + x] = alive;
            RefreshAliveCount();
            OnGridChanged?.Invoke();
        }

        public void ToggleCell(int x, int y)
        {
            if (!InBounds(x, y)) return;
            _current[y * width + x] = !_current[y * width + x];
            RefreshAliveCount();
            OnGridChanged?.Invoke();
        }

        public void PlacePattern(Vector2Int[] pattern, int cx, int cy)
        {
            foreach (var offset in pattern)
            {
                int px = cx + offset.x;
                int py = cy + offset.y;
                if (InBounds(px, py))
                    _current[py * width + px] = true;
            }
            RefreshAliveCount();
            OnGridChanged?.Invoke();
        }

        public void RandomFill()
        {
            for (int i = 0; i < _current.Length; i++)
                _current[i] = UnityEngine.Random.value < randomFillDensity;
            Generation = 0;
            RefreshAliveCount();
            OnGridChanged?.Invoke();
        }

        public void ClearGrid()
        {
            Array.Clear(_current, 0, _current.Length);
            Generation = 0;
            AliveCount = 0;
            OnGridChanged?.Invoke();
        }

        public void SetTicksPerSecond(float value)
        {
            ticksPerSecond = Mathf.Clamp(value, 1f, 30f);
        }

        // --- ISimulation ---

        public void Play()  { _isPlaying = true;  _timer = 0f; }
        public void Pause() { _isPlaying = false; }
        public void Step()  { if (!_isPlaying) Tick(); }

        public void ResetSimulation()
        {
            _isPlaying = false;
            ClearGrid();
        }

        // --- Private ---

        private void Tick()
        {
            LifeRules.NextGeneration(_current, _next, width, height);
            (_current, _next) = (_next, _current); // swap buffers
            Generation++;
            RefreshAliveCount();
            OnGridChanged?.Invoke();
        }

        private void RefreshAliveCount()
        {
            int count = 0;
            for (int i = 0; i < _current.Length; i++)
                if (_current[i]) count++;
            AliveCount = count;
        }

        private bool InBounds(int x, int y) =>
            x >= 0 && x < width && y >= 0 && y < height;
    }
}
