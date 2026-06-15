using UnityEngine;

namespace BehaviorSimulation.AntColony
{
    // Manages two float[] pheromone layers (nest & food), evaporates each tick,
    // and renders both into a single Texture2D on its SpriteRenderer.
    [RequireComponent(typeof(SpriteRenderer))]
    public class PheromoneGrid : MonoBehaviour
    {
        [SerializeField] private AntSettings settings;

        float[] _nest, _food;
        Color32[] _pixels;
        Texture2D _tex;
        int W, H;

        void Awake()
        {
            W = settings.gridW;
            H = settings.gridH;

            _nest   = new float[W * H];
            _food   = new float[W * H];
            _pixels = new Color32[W * H];

            _tex = new Texture2D(W, H, TextureFormat.RGB24, false)
                   { filterMode = FilterMode.Bilinear };

            // PPU so that the sprite fills exactly boundsHalfW*2 x boundsHalfH*2 world units.
            float ppu = W / (settings.boundsHalfW * 2f);
            var sr = GetComponent<SpriteRenderer>();
            sr.sprite       = Sprite.Create(_tex, new Rect(0, 0, W, H), Vector2.one * 0.5f, ppu);
            sr.sortingOrder = -10;

            Render();
        }

        // ── Public API ────────────────────────────────────────────────────────

        public void Deposit(float wx, float wy, bool isFood, float amount)
        {
            int idx = ToIdx(wx, wy);
            if (idx < 0) return;
            if (isFood) _food[idx] = Mathf.Min(1f, _food[idx] + amount);
            else        _nest[idx] = Mathf.Min(1f, _nest[idx] + amount);
        }

        public float Sample(float wx, float wy, bool isFood)
        {
            int idx = ToIdx(wx, wy);
            if (idx < 0) return 0f;
            return isFood ? _food[idx] : _nest[idx];
        }

        public void Tick(float dt)
        {
            float retain = Mathf.Max(0f, 1f - settings.evaporateRate * dt);
            for (int i = 0; i < W * H; i++)
            {
                _nest[i] *= retain;
                _food[i] *= retain;
            }
            Render();
        }

        public void Clear()
        {
            System.Array.Clear(_nest, 0, W * H);
            System.Array.Clear(_food, 0, W * H);
            Render();
        }

        // ── Internal ──────────────────────────────────────────────────────────

        void Render()
        {
            // food pheromone  → orange (R high, G medium)
            // nest pheromone  → cyan-blue (G+B high)
            // background      → camera bg colour (13, 13, 20)
            for (int i = 0; i < W * H; i++)
            {
                float n = _nest[i];
                float f = _food[i];
                _pixels[i] = new Color32(
                    (byte)(13 + f * 210),
                    (byte)(13 + f * 140 + n * 60),
                    (byte)(20 + n * 210),
                    255);
            }
            _tex.SetPixels32(_pixels);
            _tex.Apply(false);
        }

        int ToIdx(float wx, float wy)
        {
            float hw = settings.boundsHalfW, hh = settings.boundsHalfH;
            int gx = Mathf.FloorToInt((wx + hw) / (hw * 2f) * W);
            int gy = Mathf.FloorToInt((wy + hh) / (hh * 2f) * H);
            if (gx < 0 || gx >= W || gy < 0 || gy >= H) return -1;
            return gy * W + gx;
        }
    }
}
