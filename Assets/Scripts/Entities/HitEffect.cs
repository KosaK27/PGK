using System.Collections;
using UnityEngine;

public class HitEffect : MonoBehaviour
{
    [SerializeField] private float flashDuration = 0.12f;
    [SerializeField] private Color flashColor = new Color(1f, 0.3f, 0.3f, 1f);
    [SerializeField] private float iframeBlinkInterval = 0.1f;

    private SpriteRenderer[] _renderers;
    private Color _originalColor;
    private Coroutine _blinkCoroutine;

    void Awake()
    {
        _renderers = GetComponentsInChildren<SpriteRenderer>();
        if (_renderers.Length > 0) _originalColor = _renderers[0].color;
    }

    public void TriggerHit(Vector2 sourcePosition)
    {
        StartCoroutine(DoFlash());
    }

    public void StartIframes()
    {
        if (_blinkCoroutine != null) StopCoroutine(_blinkCoroutine);
        _blinkCoroutine = StartCoroutine(DoBlink());
    }

    public void StopIframes()
    {
        if (_blinkCoroutine != null)
        {
            StopCoroutine(_blinkCoroutine);
            _blinkCoroutine = null;
        }
        foreach (var sr in _renderers) sr.enabled = true;
    }

    private IEnumerator DoFlash()
    {
        foreach (var sr in _renderers) sr.color = flashColor;
        yield return new WaitForSeconds(flashDuration);
        foreach (var sr in _renderers) sr.color = _originalColor;
    }

    private IEnumerator DoBlink()
    {
        while (true)
        {
            foreach (var sr in _renderers) sr.enabled = false;
            yield return new WaitForSeconds(iframeBlinkInterval);
            foreach (var sr in _renderers) sr.enabled = true;
            yield return new WaitForSeconds(iframeBlinkInterval);
        }
    }
}