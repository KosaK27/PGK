using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class MenuController : MonoBehaviour
{
    [SerializeField] private GameObject _hubPanel;
    [SerializeField] private GameObject _characterPanel;
    [SerializeField] private GameObject _worldPanel;
    [SerializeField] private TextMeshProUGUI _characterLabel;
    [SerializeField] private TextMeshProUGUI _worldLabel;
    [SerializeField] private Button _playButton;
    [SerializeField] private string _gameSceneName = "Game";

    void Start()
    {
        ShowHub();
    }

    public void ShowHub()
    {
        _hubPanel.SetActive(true);
        _characterPanel.SetActive(false);
        _worldPanel.SetActive(false);
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
        SceneManager.LoadScene(_gameSceneName);
    }

    public void OnCharacterClicked()
    {
        _characterPanel.SetActive(true);
        _characterPanel.GetComponent<CharacterPanel>().Open(this);
    }

    public void OnWorldClicked()
    {
        _worldPanel.SetActive(true);
        _worldPanel.GetComponent<WorldPanel>().Open(this);
    }
}