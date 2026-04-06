using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CraftingRecipeUI : MonoBehaviour
{
    [SerializeField] private Image itemIcon;
    [SerializeField] private TextMeshProUGUI itemNameLabel;
    [SerializeField] private TMP_InputField quantityInput;
    [SerializeField] private Button craftButton;
    [SerializeField] private Image craftButtonImage;
    [SerializeField] private Transform ingredientContainer;
    [SerializeField] private GameObject ingredientPrefab;

    private static readonly Color ButtonCanCraft    = new Color(0.1f, 0.75f, 0.1f);
    private static readonly Color ButtonCannotCraft = new Color(0.15f, 0.35f, 0.15f);
    private static readonly Color ColorHave         = new Color(0.2f, 0.85f, 0.2f);
    private static readonly Color ColorMissing      = new Color(0.85f, 0.2f, 0.2f);

    private CraftingRecipe _recipe;

    private struct IngredientWidgets
    {
        public Image icon;
        public Image border;
        public TextMeshProUGUI label;
    }

    private readonly List<IngredientWidgets> _widgets = new();

    public void Setup(CraftingRecipe recipe)
    {
        _recipe = recipe;
        itemIcon.sprite = recipe.outputItem.sprite;
        itemNameLabel.text = recipe.outputAmount > 1
            ? $"{recipe.outputItem.displayName} x{recipe.outputAmount}"
            : recipe.outputItem.displayName;
        quantityInput.text = "1";
        quantityInput.contentType = TMP_InputField.ContentType.IntegerNumber;
        quantityInput.onValueChanged.AddListener(_ => Refresh());
        craftButton.onClick.AddListener(OnCraftClicked);

        foreach (var ing in recipe.ingredients)
        {
            var go = Instantiate(ingredientPrefab, ingredientContainer);
            _widgets.Add(new IngredientWidgets
            {
                icon   = go.transform.Find("Icon").GetComponent<Image>(),
                border = go.transform.Find("Border").GetComponent<Image>(),
                label  = go.transform.Find("Label").GetComponent<TextMeshProUGUI>()
            });
        }

        Refresh();
    }

    public void Refresh()
    {
        int qty = GetQuantity();

        for (int i = 0; i < _recipe.ingredients.Count; i++)
        {
            var ing    = _recipe.ingredients[i];
            int needed = ing.amount * qty;
            int have   = CountInInventory(ing.item);
            bool ok    = have >= needed;

            var w = _widgets[i];
            w.icon.sprite    = ing.item.sprite;
            w.border.color   = ok ? ColorHave : ColorMissing;
            w.label.color    = ok ? ColorHave : ColorMissing;
            w.label.text     = $"{have}/{needed}";
        }

        bool canCraft            = CanCraft(qty);
        craftButtonImage.color   = canCraft ? ButtonCanCraft : ButtonCannotCraft;
        craftButton.interactable = canCraft;
    }

    private bool CanCraft(int qty)
    {
        if (qty <= 0) return false;
        foreach (var ing in _recipe.ingredients)
            if (!InventorySystem.Instance.HasItem(ing.item, ing.amount * qty)) return false;
        return true;
    }

    private void OnCraftClicked()
    {
        int qty = GetQuantity();
        if (!CanCraft(qty)) return;

        foreach (var ing in _recipe.ingredients)
            InventorySystem.Instance.RemoveItem(ing.item, ing.amount * qty);

        int overflow = InventorySystem.Instance.AddItem(
            new ItemStack(_recipe.outputItem, _recipe.outputAmount * qty));

        if (overflow > 0)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                ItemDropSystem.Instance.DropItem(
                    new ItemStack(_recipe.outputItem, overflow),
                    player.transform.position, 1f);
        }

        CraftingUI.Instance.RefreshAll();
    }

    private int GetQuantity()
    {
        return int.TryParse(quantityInput.text, out int v) && v > 0 ? v : 1;
    }

    private int CountInInventory(ItemDefinition item)
    {
        int count = 0;
        var inv = InventorySystem.Instance;
        for (int i = 0; i < inv.TotalSlots; i++)
        {
            var slot = inv.GetSlot(i);
            if (slot != null && !slot.IsEmpty && slot.item == item)
                count += slot.amount;
        }
        return count;
    }
}