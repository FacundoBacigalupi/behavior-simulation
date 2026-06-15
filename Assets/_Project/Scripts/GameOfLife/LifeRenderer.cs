using UnityEngine;

namespace BehaviorSimulation.GameOfLife
{
    // Renders the LifeGrid as a Texture2D displayed on a SpriteRenderer.
    // One pixel = one cell. FilterMode.Point keeps pixels sharp.
    // Attach to the same GameObject as a SpriteRenderer.
    [RequireComponent(typeof(SpriteRenderer))]
    public class LifeRenderer : MonoBehaviour
    {
        [SerializeField] private LifeGrid grid;

        [Header("Colors")]
        [SerializeField] private Color aliveColor = Color.white;
        [SerializeField] private Color deadColor  = new Color(0.13f, 0.13f, 0.16f); // dark but distinct from camera bg

        private Texture2D _texture;
        private SpriteRenderer _renderer;
        private Color[] _pixels;

        private void Start()
        {
            _renderer = GetComponent<SpriteRenderer>();

            _texture = new Texture2D(grid.Width, grid.Height, TextureFormat.RGB24, false)
            {
                filterMode = FilterMode.Point,
                wrapMode   = TextureWrapMode.Clamp,
            };

            _pixels = new Color[grid.Width * grid.Height];

            // 1 pixel per world unit so the grid fills exactly width x height units.
            _renderer.sprite = Sprite.Create(
                _texture,
                new Rect(0, 0, grid.Width, grid.Height),
                new Vector2(0.5f, 0.5f),
                pixelsPerUnit: 1f);

            grid.OnGridChanged += Redraw;
            Redraw();
        }

        private void OnDestroy()
        {
            if (grid != null) grid.OnGridChanged -= Redraw;
        }

        private void Redraw()
        {
            int w = grid.Width;
            int h = grid.Height;
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    _pixels[y * w + x] = grid.GetCell(x, y) ? aliveColor : deadColor;

            _texture.SetPixels(_pixels);
            _texture.Apply();
        }
    }
}
