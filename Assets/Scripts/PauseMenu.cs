using UnityEngine;
using UnityEngine.InputSystem;

public class PauseMenu : MonoBehaviour
{
    [SerializeField] private GameObject _panel;
    [SerializeField] private GameBootstrap _gameBootstrap;

    private bool _paused = false;

    void Update()
    {
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
            SetPaused(!_paused);
    }

    void SetPaused(bool paused)
    {
        _paused = paused;
        Time.timeScale = paused ? 0f : 1f;
        _panel.SetActive(paused);
    }

    public void OnResumeClicked() => SetPaused(false);

    public void OnSaveAndExitClicked()
    {
        Time.timeScale = 1f;
        _gameBootstrap.OnReturnToMenu();
    }
}