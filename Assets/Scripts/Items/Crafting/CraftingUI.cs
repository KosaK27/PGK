using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CraftingUI : MonoBehaviour
{
    public static CraftingUI Instance { get; private set; }

    [Header("References")]
    [SerializeField] private GameObject panel;
    [SerializeField] private Transform scrollContent;
    [SerializeField] private GameObject recipeEntryPrefab;
    [SerializeField] private TextMeshProUGUI titleLabel;

    private readonly List<CraftingRecipeUI> _recipeUIs = new();

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        panel.SetActive(false);
        CraftingUIManager.Instance.OnStationOpened += Open;
        CraftingUIManager.Instance.OnStationClosed += Close;
        InventorySystem.Instance.OnInventoryChanged += RefreshAll;
    }

    void OnDestroy()
    {
        if (CraftingUIManager.Instance != null)
        {
            CraftingUIManager.Instance.OnStationOpened -= Open;
            CraftingUIManager.Instance.OnStationClosed -= Close;
        }
        if (InventorySystem.Instance != null)
            InventorySystem.Instance.OnInventoryChanged -= RefreshAll;
    }

    private void Open(CraftingStationDefinition def)
    {
        if (titleLabel != null) titleLabel.text = def.displayName;

        foreach (Transform child in scrollContent) Destroy(child.gameObject);
        _recipeUIs.Clear();

        foreach (var recipe in def.recipes)
        {
            if (recipe == null || recipe.outputItem == null) continue;
            var go  = Instantiate(recipeEntryPrefab, scrollContent);
            var rui = go.GetComponent<CraftingRecipeUI>();
            rui.Setup(recipe);
            _recipeUIs.Add(rui);
        }

        panel.SetActive(true);
        InventoryUI.Instance.OpenMainPanel();
    }

    private void Close()
    {
        panel.SetActive(false);
    }

    public void RefreshAll()
    {
        foreach (var rui in _recipeUIs) rui.Refresh();
    }
}