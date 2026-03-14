using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public float smoothSpeed = 5f;
    public float ppu = 16f;
    public Vector3 offset = new Vector3(0, 0, -10);

    void LateUpdate()
    {
        Vector3 desiredPosition = target.position + offset;
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
        transform.position = new Vector3(
            Mathf.Round(target.position.x * ppu) / ppu,
            Mathf.Round(target.position.y * ppu) / ppu,
            -10f
        );
    }
}