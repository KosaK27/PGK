using UnityEngine;
using UnityEngine.EventSystems;

public class ButtonEffects : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] private float hoverScale = 1.1f;
    [SerializeField] private float clickScale = 0.9f;
    [SerializeField] private float scaleSpeed = 10f;

    private Vector3 _baseScale;
    private Vector3 _targetScale;

    void Awake()
    {
        _baseScale = transform.localScale;
        _targetScale = _baseScale;
    }

    void Update()
    {
        transform.localScale = Vector3.Lerp(transform.localScale, _targetScale, Time.deltaTime * scaleSpeed);
    }

    public void OnPointerEnter(PointerEventData e)
    {
        UIAudioManager.Instance?.PlayHover();
        _targetScale = _baseScale * hoverScale;
    }

    public void OnPointerExit(PointerEventData e)
    {
        _targetScale = _baseScale;
    }

    public void OnPointerDown(PointerEventData e)
    {
        _targetScale = _baseScale * clickScale;
    }

    public void OnPointerUp(PointerEventData e)
    {
        _targetScale = _baseScale * hoverScale;
    }

    public void OnPointerClick(PointerEventData e)
    {
        UIAudioManager.Instance?.PlayClick();
    }
}