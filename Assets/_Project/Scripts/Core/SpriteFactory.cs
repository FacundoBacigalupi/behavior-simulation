using UnityEngine;

namespace BehaviorSimulation.Core
{
    // Runtime sprite generators — no external assets required.
    public static class SpriteFactory
    {
        // Triangle pointing up (tip at local +Y). ppu=size → 1 world unit tall.
        public static Sprite Arrow(int size = 32, float ppu = -1f)
        {
            if (ppu < 0) ppu = size;
            var tex = NewTex(size);
            var pix = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                float t = 1f - (float)y / (size - 1); // 0 at tip (top), 1 at base
                int half = Mathf.RoundToInt(t * (size * 0.44f));
                int cx = size / 2;
                for (int x = cx - half; x <= cx + half; x++)
                    if (x >= 0 && x < size)
                        pix[y * size + x] = White;
            }
            return Apply(tex, pix, ppu);
        }

        // Filled circle.
        public static Sprite Circle(int size = 32, float ppu = -1f)
        {
            if (ppu < 0) ppu = size;
            var tex = NewTex(size);
            var pix = new Color32[size * size];
            float r = size * 0.48f, cx = size * 0.5f, cy = size * 0.5f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = x + 0.5f - cx, dy = y + 0.5f - cy;
                    pix[y * size + x] = dx * dx + dy * dy <= r * r ? White : Clear;
                }
            return Apply(tex, pix, ppu);
        }

        // Hollow ring.
        public static Sprite Ring(int size = 32, float thickness = 3f, float ppu = -1f)
        {
            if (ppu < 0) ppu = size;
            var tex = NewTex(size);
            var pix = new Color32[size * size];
            float outerR = size * 0.48f, innerR = outerR - thickness;
            float cx = size * 0.5f, cy = size * 0.5f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = x + 0.5f - cx, dy = y + 0.5f - cy;
                    float d2 = dx * dx + dy * dy;
                    pix[y * size + x] = d2 <= outerR * outerR && d2 >= innerR * innerR ? White : Clear;
                }
            return Apply(tex, pix, ppu);
        }

        // ── Helpers ──────────────────────────────────────────────────────────
        static readonly Color32 White = new(255, 255, 255, 255);
        static readonly Color32 Clear = new(0, 0, 0, 0);

        static Texture2D NewTex(int size) =>
            new(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };

        static Sprite Apply(Texture2D tex, Color32[] pix, float ppu)
        {
            tex.SetPixels32(pix);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                Vector2.one * 0.5f, ppu);
        }
    }
}
