using UnityEngine;

namespace BehaviorSimulation.Core
{
    // 2D camera: scroll wheel to zoom, WASD or right-click drag to pan.
    [RequireComponent(typeof(Camera))]
    public class CameraController2D : MonoBehaviour
    {
        [Header("Zoom")]
        [SerializeField] private float zoomSpeed = 3f;
        [SerializeField] private float minZoom = 1f;
        [SerializeField] private float maxZoom = 50f;

        [Header("Pan")]
        [SerializeField] private float panSpeed = 10f;

        private Camera _cam;
        private Vector3 _dragOrigin;
        private bool _isDragging;

        private void Awake()
        {
            _cam = GetComponent<Camera>();
        }

        private void Update()
        {
            HandleZoom();
            HandlePan();
        }

        private void HandleZoom()
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Approximately(scroll, 0f)) return;

            _cam.orthographicSize -= scroll * zoomSpeed * _cam.orthographicSize;
            _cam.orthographicSize = Mathf.Clamp(_cam.orthographicSize, minZoom, maxZoom);
        }

        private void HandlePan()
        {
            // WASD keyboard pan
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");
            if (h != 0f || v != 0f)
            {
                float speed = panSpeed * _cam.orthographicSize * Time.unscaledDeltaTime;
                transform.Translate(new Vector3(h, v, 0f) * speed);
                return;
            }

            // Right-click drag pan
            if (Input.GetMouseButtonDown(1))
            {
                _dragOrigin = _cam.ScreenToWorldPoint(Input.mousePosition);
                _isDragging = true;
            }
            if (Input.GetMouseButtonUp(1))
                _isDragging = false;

            if (_isDragging && Input.GetMouseButton(1))
            {
                Vector3 diff = _dragOrigin - _cam.ScreenToWorldPoint(Input.mousePosition);
                transform.position += diff;
            }
        }
    }
}
