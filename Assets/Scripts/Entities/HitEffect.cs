using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HitEffect : MonoBehaviour
{
    [Header("Flash")]
    [SerializeField] private float flashDuration = 0.12f;
    [SerializeField] private Color flashColor = new Color(1f, 0.3f, 0.3f, 1f);

    private SpriteRenderer[] _renderers;
    private Color _originalColor;

    void Awake()
    {
        _renderers = GetComponentsInChildren<SpriteRenderer>();
        if (_renderers.Length > 0) _originalColor = _renderers[0].color;
    }

    public void TriggerHit(Vector2 hitSourcePosition)
    {
        if (_renderers.Length > 0) StartCoroutine(DoFlash());
    }

    private IEnumerator DoFlash()
    {
        foreach (var sr in _renderers) sr.color = flashColor;
        yield return new WaitForSeconds(flashDuration);
        foreach (var sr in _renderers) sr.color = _originalColor;
    }
}