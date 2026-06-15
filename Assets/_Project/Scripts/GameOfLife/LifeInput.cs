using UnityEngine;

namespace BehaviorSimulation.GameOfLife
{
    // Translates mouse clicks into grid cell toggles.
    // Left-click: toggle cell under cursor.
    // Left-drag:  paint cells alive.
    // Right-drag: paint cells dead.
    public class LifeInput : MonoBehaviour
    {
        [SerializeField] private LifeGrid grid;
        [SerializeField] private Camera cam;

        // Width and height in world units must match the renderer's sprite size.
        private int _lastX = -1;
        private int _lastY = -1;

        private void Awake()
        {
            if (cam == null) cam = Camera.main;
        }

        private void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                _lastX = -1;
                _lastY = -1;
            }

            if (Input.GetMouseButton(0) || Input.GetMouseButton(1))
                HandleClick(Input.GetMouseButton(1));
        }

        private void HandleClick(bool erase)
        {
            Vector3 world = cam.ScreenToWorldPoint(Input.mousePosition);

            // Grid origin is centered, so shift by half grid size.
            int gx = Mathf.FloorToInt(world.x + grid.Width  * 0.5f);
            int gy = Mathf.FloorToInt(world.y + grid.Height * 0.5f);

            if (gx < 0 || gx >= grid.Width || gy < 0 || gy >= grid.Height) return;

            // Avoid toggling the same cell multiple times in one drag.
            if (erase)
            {
                grid.SetCell(gx, gy, false);
            }
            else if (gx != _lastX || gy != _lastY)
            {
                if (_lastX == -1 && _lastY == -1)
                    grid.ToggleCell(gx, gy);
                else
                    grid.SetCell(gx, gy, true);
            }

            _lastX = gx;
            _lastY = gy;
        }
    }
}
