using UnityEngine;
using TMPro;

public class CharacterPanel : MonoBehaviour
{
    [SerializeField] private GameObject _selectionPanel;
    [SerializeField] private GameObject _creationPanel;
    [SerializeField] private Transform _listContainer;
    [SerializeField] private GameObject _rowPrefab;
    [SerializeField] private TMP_InputField _nameInput;
    [SerializeField] private TextMeshProUGUI _errorLabel;

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
        foreach (var c in sm.Characters)
        {
            var go = Instantiate(_rowPrefab, _listContainer);
            go.GetComponent<CharacterRowUI>().Setup(c, c.id == sm.Profile.selectedCharacterId, OnSelect, OnDelete);
        }
    }

    void OnSelect(string id)
    {
        SaveManager.Instance.SelectCharacter(id);
        Rebuild();
        _menu.RefreshLabels();
    }

    void OnDelete(string id)
    {
        SaveManager.Instance.DeleteCharacter(id);
        Rebuild();
        _menu.RefreshLabels();
    }

    public void OnNewClicked()
    {
        _nameInput.text = "";
        _errorLabel.text = "";
        _selectionPanel.SetActive(false);
        _creationPanel.SetActive(true);
    }

    public void OnConfirmCreate()
    {
        string name = _nameInput.text.Trim();
        if (string.IsNullOrEmpty(name)) { _errorLabel.text = "Name cannot be empty."; return; }
        if (name.Length > 24) { _errorLabel.text = "Max 24 characters."; return; }
        var c = SaveManager.Instance.CreateCharacter(name);
        SaveManager.Instance.SelectCharacter(c.id);
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