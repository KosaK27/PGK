using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

[RequireComponent(typeof(Image))]
public class InventorySlotUI : MonoBehaviour,
    IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image           slotBackground;
    [SerializeField] private Image           itemIcon;
    [SerializeField] private TextMeshProUGUI countLabel;
    [SerializeField] private Image           highlightOverlay;

    [SerializeField] private Color hoverTint = new Color(1f, 1f, 1f, 0.15f);

    private int         _slotIndex;
    private Action<int> _onClicked;

    public void Init(int index, Action<int> onClicked)
    {
        _slotIndex = index;
        _onClicked = onClicked;

        if (highlightOverlay != null)
        {
            highlightOverlay.color = Color.clear;
            highlightOverlay.gameObject.SetActive(true);
        }
    }

    public void Refresh(ItemStack stack)
    {
        bool hasItem = stack != null && !stack.IsEmpty;
        itemIcon.enabled   = hasItem;
        countLabel.enabled = hasItem && stack.amount > 1;

        if (hasItem)
        {
            itemIcon.sprite = stack.item.sprite;
            countLabel.text = stack.amount.ToString();
        }
    }

    public void SetIconVisible(bool visible) => itemIcon.enabled = visible;

    public void SetHighlight(bool selected, Color normal, Color selectedColor)
        => slotBackground.color = selected ? selectedColor : normal;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;
        _onClicked?.Invoke(_slotIndex);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (highlightOverlay != null) highlightOverlay.color = hoverTint;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (highlightOverlay != null) highlightOverlay.color = Color.clear;
    }
}