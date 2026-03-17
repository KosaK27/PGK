using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public float smoothSpeed = 5f;
    public float pixelsPerUnit = 16f;
    public Vector3 offset = new Vector3(0, 5, 0);

    void LateUpdate()
    {
        transform.position = new Vector3(
        Mathf.Round((target.position.x + offset.x) * pixelsPerUnit) / pixelsPerUnit,
        Mathf.Round((target.position.y + offset.y) * pixelsPerUnit) / pixelsPerUnit,
        -10f
    );
    }
}