using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class MenuController : MonoBehaviour
{
    [SerializeField] private GameObject _hubPanel;
    [SerializeField] private GameObject _characterPanel;
    [SerializeField] private GameObject _worldPanel;
    [SerializeField] private GameObject _settingsPanel;
    [SerializeField] private TextMeshProUGUI _characterLabel;
    [SerializeField] private TextMeshProUGUI _worldLabel;
    [SerializeField] private Button _playButton;
    [SerializeField] private string _loadingSceneName = "Loading";

    void Start()
    {
        ShowHub();
    }

    public void ShowHub()
    {
        _hubPanel.SetActive(true);
        _characterPanel.SetActive(false);
        _worldPanel.SetActive(false);
        _settingsPanel.SetActive(false);
        RefreshLabels();
    }

    public void RefreshLabels()
    {
        var sm = SaveManager.Instance;
        _characterLabel.text = sm.SelectedCharacter?.characterName ?? "None";
        _worldLabel.text = sm.SelectedWorld?.worldName ?? "None";
        _playButton.interactable = sm.SelectedCharacter != null && sm.SelectedWorld != null;
    }

    public void OnPlayClicked()
    {
        var sm = SaveManager.Instance;
        if (sm.SelectedCharacter == null || sm.SelectedWorld == null) return;
        var worldSave = sm.SelectedWorld;
        LoadingContext.IsNewWorld = worldSave.lastPlayedAt == 0;
        SceneManager.LoadScene(_loadingSceneName);
    }

    public void OnCharacterClicked()
    {
        _settingsPanel.SetActive(false);
        _worldPanel.SetActive(false);
        _characterPanel.SetActive(true);
        _characterPanel.GetComponent<CharacterPanel>().Open(this);
    }

    public void OnWorldClicked()
    {
        _settingsPanel.SetActive(false);
        _characterPanel.SetActive(false);
        _worldPanel.SetActive(true);
        _worldPanel.GetComponent<WorldPanel>().Open(this);
    }

    public void OnSettingsClicked()
    {
        _characterPanel.SetActive(false);
        _worldPanel.SetActive(false);
        _settingsPanel.SetActive(!_settingsPanel.activeSelf);
    }

    public void OnQuitClicked()
    {
        Application.Quit();
    }
}