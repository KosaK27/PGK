using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ButtonEffects : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerClickHandler,
    IPointerDownHandler,
    IPointerUpHandler
{
    [SerializeField] private float hoverScale = 1.1f;
    [SerializeField] private float clickScale = 0.9f;
    [SerializeField] private float scaleSpeed = 10f;

    private Vector3 _baseScale;
    private Vector3 _targetScale;

    private Button _button;

    void Awake()
    {
        _baseScale = transform.localScale;
        _targetScale = _baseScale;
        _button = GetComponent<Button>();
    }

    void Update()
    {
        transform.localScale = Vector3.Lerp(transform.localScale, _targetScale, Time.deltaTime * scaleSpeed);
    }

    private bool IsBlocked()
    {
        return _button != null && !_button.interactable;
    }

    public void OnPointerEnter(PointerEventData e)
    {
        if (IsBlocked()) return;

        UIAudioManager.Instance?.PlayHover();
        _targetScale = _baseScale * hoverScale;
    }

    public void OnPointerExit(PointerEventData e)
    {
        if (IsBlocked()) return;

        _targetScale = _baseScale;
    }

    public void OnPointerDown(PointerEventData e)
    {
        if (IsBlocked()) return;

        _targetScale = _baseScale * clickScale;
    }

    public void OnPointerUp(PointerEventData e)
    {
        if (IsBlocked()) return;

        _targetScale = _baseScale * hoverScale;
    }

    public void OnPointerClick(PointerEventData e)
    {
        if (IsBlocked()) return;

        UIAudioManager.Instance?.PlayClick();
    }
}