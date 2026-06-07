using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ItemTooltipUI : MonoBehaviour
{
    public static ItemTooltipUI Instance { get; private set; }

    [SerializeField] private GameObject panel;
    [SerializeField] private TextMeshProUGUI titleLabel;
    [SerializeField] private TextMeshProUGUI amountLabel;
    [SerializeField] private TextMeshProUGUI typeLabel;
    [SerializeField] private TextMeshProUGUI statsLabel;
    [SerializeField] private TextMeshProUGUI descriptionLabel;
    [SerializeField] private RectTransform panelRect;
    [SerializeField] private Vector2 offset = new Vector2(12f, -12f);

    private Canvas _canvas;
    private RectTransform _canvasRect;
    private ItemStack _currentStack;
    private bool _layoutDirty;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        _canvas = GetComponentInParent<Canvas>();
        _canvasRect = _canvas.GetComponent<RectTransform>();

        panelRect.pivot = new Vector2(0f, 1f);
        DisableRaycastsOnPanel();
        panel.SetActive(false);
    }

    void LateUpdate()
    {
        if (!panel.activeSelf) return;

        if (_layoutDirty)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(panelRect);
            _layoutDirty = false;
        }

        UpdatePosition();
    }

    public void Show(ItemStack stack)
    {
        if (stack == null || stack.IsEmpty)
        {
            Hide();
            return;
        }

        if (_currentStack != null && !_currentStack.IsEmpty &&
            _currentStack.item == stack.item && _currentStack.amount == stack.amount)
        {
            if (panel.activeSelf) return;
        }

        _currentStack = stack;

        titleLabel.text = ItemTooltip.BuildTitle(stack);
        amountLabel.text = ItemTooltip.BuildAmount(stack);
        typeLabel.text = ItemTooltip.BuildType(stack);
        statsLabel.text = ItemTooltip.BuildStats(stack);
        descriptionLabel.text = ItemTooltip.BuildDescription(stack);

        amountLabel.gameObject.SetActive(!string.IsNullOrEmpty(amountLabel.text));
        statsLabel.gameObject.SetActive(!string.IsNullOrEmpty(statsLabel.text));
        descriptionLabel.gameObject.SetActive(!string.IsNullOrEmpty(descriptionLabel.text));

        panelRect.anchoredPosition = new Vector2(-9999f, -9999f);
        panel.SetActive(true);
        _layoutDirty = true;
    }

    public void Hide()
    {
        _currentStack = null;
        panel.SetActive(false);
        _layoutDirty = false;
    }

    private void UpdatePosition()
    {
        var mousePos = UnityEngine.InputSystem.Mouse.current.position.ReadValue();

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvasRect,
            mousePos,
            _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera,
            out var localPoint);

        var size = panelRect.rect.size;
        var pos = localPoint + offset;

        float rightEdge = pos.x + size.x;
        float bottomEdge = pos.y - size.y;
        float canvasW = _canvasRect.rect.width * 0.5f;
        float canvasH = _canvasRect.rect.height * 0.5f;

        if (rightEdge > canvasW) pos.x -= rightEdge - canvasW;
        if (bottomEdge < -canvasH) pos.y += -canvasH - bottomEdge;

        panelRect.anchoredPosition = pos;
    }

    private void DisableRaycastsOnPanel()
    {
        foreach (var graphic in panel.GetComponentsInChildren<Graphic>(true))
            graphic.raycastTarget = false;

        var panelGraphic = panel.GetComponent<Graphic>();
        if (panelGraphic != null)
            panelGraphic.raycastTarget = false;
    }
}