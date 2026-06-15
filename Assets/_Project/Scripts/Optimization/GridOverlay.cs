using UnityEngine;

namespace BehaviorSimulation.Optimization
{
    // Visualizes spatial grid cell occupancy as a pixel-per-cell texture overlay.
    // One texel = one cell; color goes from dim green (1 agent) to red (many agents).
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class GridOverlay : MonoBehaviour
    {
        const float HalfW = 19f, HalfH = 11f;

        int        _gridW, _gridH;
        int[]      _counts;
        Color32[]  _pixels;
        Texture2D  _tex;
        SpriteRenderer _sr;
        bool       _visible;

        public bool IsVisible => _visible;

        void Awake()
        {
            _sr              = GetComponent<SpriteRenderer>();
            _sr.sortingOrder = -8;
            _sr.enabled      = false;
        }

        public void Init(float cellSize)
        {
            _gridW = Mathf.Max(1, Mathf.CeilToInt(HalfW * 2f / cellSize));
            _gridH = Mathf.Max(1, Mathf.CeilToInt(HalfH * 2f / cellSize));
            _counts = new int[_gridW * _gridH];
            _pixels = new Color32[_gridW * _gridH];

            if (_tex) Destroy(_tex);
            _tex            = new Texture2D(_gridW, _gridH, TextureFormat.RGBA32, false);
            _tex.filterMode = FilterMode.Point;   // crisp cell edges, not blurry

            float ppu  = _gridW / (HalfW * 2f);
            _sr.sprite = Sprite.Create(_tex,
                new Rect(0, 0, _gridW, _gridH), Vector2.one * 0.5f, ppu);
        }

        public void SetVisible(bool v)
        {
            _visible    = v;
            _sr.enabled = v;
        }

        // positions and count come from OptimizationManager's parallel array.
        public void UpdateOverlay(Vector2[] positions, int count, float cellSize)
        {
            if (!_visible || _pixels == null) return;

            System.Array.Clear(_counts, 0, _counts.Length);
            float invCell = 1f / cellSize;

            for (int i = 0; i < count; i++)
            {
                int gx = Mathf.Clamp(Mathf.FloorToInt((positions[i].x + HalfW) * invCell), 0, _gridW - 1);
                int gy = Mathf.Clamp(Mathf.FloorToInt((positions[i].y + HalfH) * invCell), 0, _gridH - 1);
                _counts[gy * _gridW + gx]++;
            }

            for (int i = 0; i < _pixels.Length; i++)
            {
                int n = _counts[i];
                if (n == 0) { _pixels[i] = new Color32(0, 0, 0, 0); continue; }

                // green → yellow → red as density rises (saturates at 8 agents)
                float t  = Mathf.Clamp01(n / 8f);
                byte  r  = (byte)(255 * t);
                byte  g  = (byte)(200 * (1f - t * 0.8f));
                byte  a  = (byte)(80 + 140 * Mathf.Clamp01(n / 4f));
                _pixels[i] = new Color32(r, g, 30, a);
            }

            _tex.SetPixels32(_pixels);
            _tex.Apply(false);
        }
    }
}
