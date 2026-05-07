using System;
using UnityEngine;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }

    public SettingsData Current => SaveManager.Instance.Settings;

    public event Action<float> OnMusicVolumeChanged;
    public event Action<float> OnGameSoundVolumeChanged;
    public event Action<float> OnMenuSoundVolumeChanged;
    public event Action<float> OnCameraZoomChanged;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        Apply();
    }

    public void Apply()
    {
        ApplyResolution(Current.resolutionIndex, Current.fullscreen);
        OnMusicVolumeChanged?.Invoke(Current.musicVolume);
        OnGameSoundVolumeChanged?.Invoke(Current.gameSoundVolume);
        OnMenuSoundVolumeChanged?.Invoke(Current.menuSoundVolume);
        OnCameraZoomChanged?.Invoke(Current.cameraZoom);
    }

    public void SetFullscreen(bool value)
    {
        Current.fullscreen = value;
        ApplyResolution(Current.resolutionIndex, value);
        SaveManager.Instance.SaveSettings();
    }

    public void SetResolution(int index, bool fullscreen)
    {
        Current.resolutionIndex = index;
        Current.fullscreen = fullscreen;
        ApplyResolution(index, fullscreen);
        SaveManager.Instance.SaveSettings();
    }

    private void ApplyResolution(int index, bool fullscreen)
    {
        if (fullscreen)
        {
            var resolutions = Screen.resolutions;
            if (resolutions.Length == 0) return;
            index = Mathf.Clamp(index, 0, resolutions.Length - 1);
            var r = resolutions[index];
            Screen.SetResolution(r.width, r.height, FullScreenMode.FullScreenWindow);
        }
        else
        {
            var resolutions = Screen.resolutions;
            if (resolutions.Length == 0) return;
            index = Mathf.Clamp(index, 0, resolutions.Length - 1);
            var r = resolutions[index];
            Screen.SetResolution(r.width, r.height, FullScreenMode.Windowed);
        }
    }

    public void SetMusicVolume(float value)
    {
        Current.musicVolume = value;
        OnMusicVolumeChanged?.Invoke(value);
        SaveManager.Instance.SaveSettings();
    }

    public void SetGameSoundVolume(float value)
    {
        Current.gameSoundVolume = value;
        OnGameSoundVolumeChanged?.Invoke(value);
        SaveManager.Instance.SaveSettings();
    }

    public void SetMenuSoundVolume(float value)
    {
        Current.menuSoundVolume = value;
        OnMenuSoundVolumeChanged?.Invoke(value);
        SaveManager.Instance.SaveSettings();
    }

    public void SetCameraZoom(float value)
    {
        Current.cameraZoom = value;
        OnCameraZoomChanged?.Invoke(value);
        SaveManager.Instance.SaveSettings();
    }
}