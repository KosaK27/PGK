using UnityEngine;

public class MapTrackable : MonoBehaviour
{
    [SerializeField] private MapMarkerType _markerType;

    void OnEnable()
    {
        if (MapSystem.Instance != null)
            MapSystem.Instance.RegisterMarker(transform, _markerType);
        else
            MapSystem.Instance.RegisterMarker(transform, _markerType);

        if (MinimapSystem.Instance != null)
            MinimapSystem.Instance.RegisterMarker(transform, _markerType);
        else
            MinimapSystem.Instance.RegisterMarker(transform, _markerType);
    }

    void OnDisable()
    {
        if (MapSystem.Instance != null) MapSystem.Instance.UnregisterMarker(transform);
        if (MinimapSystem.Instance != null) MinimapSystem.Instance.UnregisterMarker(transform);
    }

    void OnDestroy()
    {
        if (MapSystem.Instance != null) MapSystem.Instance.UnregisterMarker(transform);
        if (MinimapSystem.Instance != null) MinimapSystem.Instance.UnregisterMarker(transform);
    }
}