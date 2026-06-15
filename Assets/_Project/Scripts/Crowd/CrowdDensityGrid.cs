using System.Collections.Generic;
using UnityEngine;

namespace BehaviorSimulation.Crowd
{
    // Renders a per-cell agent-density heatmap behind the crowd.
    [RequireComponent(typeof(SpriteRenderer))]
    public class CrowdDensityGrid : MonoBehaviour
    {
        const int   GridW    = 76;   // 0.5 WU cells over 38 WU world width
        const int   GridH    = 44;   // 0.5 WU cells over 22 WU world height
        const float CellSize = 0.5f;
        const float HalfW    = 19f;
        const float HalfH    = 11f;

        readonly float[]   _raw    = new float[GridW * GridH];
        readonly float[]   _smooth = new float[GridW * GridH];
        readonly Color32[] _pixels = new Color32[GridW * GridH];
        Texture2D      _tex;
        SpriteRenderer _sr;
        bool           _visible;

        public bool IsVisible => _visible;

        void Awake()
        {
            _sr           = GetComponent<SpriteRenderer>();
            _tex          = new Texture2D(GridW, GridH, TextureFormat.RGBA32, false);
            _tex.filterMode = FilterMode.Bilinear;
            float ppu     = GridW / (HalfW * 2f);   // 2 px per world unit
            _sr.sprite    = Sprite.Create(_tex, new Rect(0, 0, GridW, GridH), Vector2.one * 0.5f, ppu);
            _sr.sortingOrder = -9;
            _sr.enabled   = false;   // hidden by default
        }

        public void SetVisible(bool v)
        {
            _visible    = v;
            _sr.enabled = v;
            if (!v) { System.Array.Clear(_smooth, 0, _smooth.Length); }
        }

        public void UpdateDensity(List<CrowdAgent> agents)
        {
            System.Array.Clear(_raw, 0, _raw.Length);

            foreach (var a in agents)
            {
                if (!a) continue;
                Vector2 p  = a.transform.position;
                int gx = Mathf.Clamp(Mathf.RoundToInt((p.x + HalfW) / CellSize), 0, GridW - 1);
                int gy = Mathf.Clamp(Mathf.RoundToInt((p.y + HalfH) / CellSize), 0, GridH - 1);

                // 3×3 Gaussian splat for smooth look
                for (int dy = -1; dy <= 1; dy++)
                for (int dx = -1; dx <= 1; dx++)
                {
                    int nx = gx + dx, ny = gy + dy;
                    if (nx < 0 || nx >= GridW || ny < 0 || ny >= GridH) continue;
                    _raw[ny * GridW + nx] += (dx == 0 && dy == 0) ? 1f : 0.25f;
                }
            }

            // Smooth towards current frame
            for (int i = 0; i < _smooth.Length; i++)
                _smooth[i] = Mathf.Lerp(_smooth[i], _raw[i], 0.20f);

            // Normalize: 4 agents per cell = fully saturated
            const float MaxD = 4f;

            for (int i = 0; i < _pixels.Length; i++)
            {
                float t = Mathf.Clamp01(_smooth[i] / MaxD);
                if (t < 0.01f) { _pixels[i] = new Color32(0, 0, 0, 0); continue; }

                // Hue 0.67 (blue) → 0.33 (green) → 0 (red) as density rises
                Color c = Color.HSVToRGB((1f - t) * 0.67f, 0.85f, 1f);
                byte  a = (byte)(Mathf.Lerp(40f, 200f, t));
                _pixels[i] = new Color32((byte)(c.r * 255), (byte)(c.g * 255), (byte)(c.b * 255), a);
            }

            _tex.SetPixels32(_pixels);
            _tex.Apply(false);
        }
    }
}
