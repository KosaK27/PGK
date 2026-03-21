using System.Collections;
using UnityEngine;

public class HitEffect : MonoBehaviour
{
    [Header("Flash")]
    [SerializeField] private float flashDuration = 0.12f;
    [SerializeField] private Color flashColor = new Color(1f, 0.3f, 0.3f, 1f);

    private SpriteRenderer _sr;
    private Color _originalColor;

    void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        if (_sr != null) _originalColor = _sr.color;
    }

    public void TriggerHit(Vector2 hitSourcePosition)
    {
        if (_sr != null) StartCoroutine(DoFlash());
    }

    private IEnumerator DoFlash()
    {
        _sr.color = flashColor;
        yield return new WaitForSeconds(flashDuration);
        _sr.color = _originalColor;
    }
}