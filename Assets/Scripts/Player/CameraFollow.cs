using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0, 0, 0);

    private Camera _camera;

    void Awake()
    {
        _camera = GetComponent<Camera>();
        if (SettingsManager.Instance != null)
        {
            _camera.orthographicSize = SettingsManager.Instance.Current.cameraZoom;
            SettingsManager.Instance.OnCameraZoomChanged += OnZoomChanged;
        }
    }

    void OnDestroy()
    {
        if (SettingsManager.Instance != null)
            SettingsManager.Instance.OnCameraZoomChanged -= OnZoomChanged;
    }

    void OnZoomChanged(float zoom)
    {
        _camera.orthographicSize = zoom;
    }

    void LateUpdate()
    {
        if (target == null) return;
        transform.position = new Vector3(
            target.position.x + offset.x,
            target.position.y + offset.y,
            -10f
        );
    }
}