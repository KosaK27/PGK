using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WorldPanel : MonoBehaviour
{
    [SerializeField] private GameObject _selectionPanel;
    [SerializeField] private GameObject _creationPanel;
    [SerializeField] private Transform _listContainer;
    [SerializeField] private GameObject _rowPrefab;
    [SerializeField] private TMP_InputField _nameInput;
    [SerializeField] private TMP_Dropdown _sizeDropdown;
    [SerializeField] private TMP_InputField _seedInput;
    [SerializeField] private TextMeshProUGUI _errorLabel;

    private static readonly (int w, int h)[] _presets =
    {
        (800, 300),
        (1600, 500),
        (3200, 800)
    };

    private MenuController _menu;

    public void Open(MenuController menu)
    {
        _menu = menu;
        ShowSelection();
    }

    void ShowSelection()
    {
        _selectionPanel.SetActive(true);
        _creationPanel.SetActive(false);
        Rebuild();
    }

    void Rebuild()
    {
        foreach (Transform child in _listContainer)
            Destroy(child.gameObject);

        var sm = SaveManager.Instance;

        foreach (var w in sm.Worlds)
        {
            var go = Instantiate(_rowPrefab, _listContainer);
            go.GetComponent<WorldRowUI>()
                .Setup(w, w.id == sm.Profile.selectedWorldId, OnSelect, OnDelete);
        }
    }

    void OnSelect(string id)
    {
        SaveManager.Instance.SelectWorld(id);
        Rebuild();
        _menu.RefreshLabels();
    }

    void OnDelete(string id)
    {
        SaveManager.Instance.DeleteWorld(id);
        Rebuild();
        _menu.RefreshLabels();
    }

    public void OnNewClicked()
    {
        _nameInput.text = "";
        _errorLabel.text = "";
        _sizeDropdown.value = 1;
        _seedInput.text = "";

        _selectionPanel.SetActive(false);
        _creationPanel.SetActive(true);
    }

    public void OnConfirmCreate()
    {
        _errorLabel.text = "";

        string worldName = _nameInput.text.Trim();
        if (string.IsNullOrEmpty(worldName))
        {
            _errorLabel.text = "Name cannot be empty.";
            return;
        }

        if (worldName.Length > 12)
        {
            _errorLabel.text = "Max 12 characters.";
            return;
        }

        int idx = _sizeDropdown.value;
        if (idx < 0 || idx >= _presets.Length)
            idx = 1;

        var (width, height) = _presets[idx];

        int seed;
        if (string.IsNullOrWhiteSpace(_seedInput.text))
            seed = Random.Range(0, 999999);
        else if (!int.TryParse(_seedInput.text, out seed))
        {
            _errorLabel.text = "Seed must be a number.";
            return;
        }

        var w = SaveManager.Instance.CreateWorld(worldName, width, height, seed);
        SaveManager.Instance.SelectWorld(w.id);

        _menu.RefreshLabels();
        ShowSelection();
    }

    public void OnCancelCreate() => ShowSelection();

    public void OnBackClicked()
    {
        gameObject.SetActive(false);
        _menu.ShowHub();
    }
}