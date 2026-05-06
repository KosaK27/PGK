using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class SettingsPanel : MonoBehaviour
{
    [Header("Display")]
    [SerializeField] private Button fullscreenButton;
    [SerializeField] private TextMeshProUGUI fullscreenButtonLabel;
    [SerializeField] private Button resolutionButton;
    [SerializeField] private TextMeshProUGUI resolutionButtonLabel;

    [Header("Allowed Resolutions")]
    [SerializeField] private Vector2Int[] allowedResolutions = new Vector2Int[]
    {
        new Vector2Int(1280, 720),
        new Vector2Int(1600, 900),
        new Vector2Int(1920, 1080),
        new Vector2Int(2560, 1440),
        new Vector2Int(3840, 2160)
    };

    [Header("Volume")]
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider gameSoundSlider;
    [SerializeField] private Slider menuSoundSlider;

    [Header("Camera")]
    [SerializeField] private Slider cameraZoomSlider;
    [SerializeField] private float zoomMin = 3f;
    [SerializeField] private float zoomMax = 12f;

    private List<Vector2Int> _availableResolutions = new();

    void Awake()
    {
        musicSlider.onValueChanged.AddListener(value => SettingsManager.Instance.SetMusicVolume(value));
        gameSoundSlider.onValueChanged.AddListener(value => SettingsManager.Instance.SetGameSoundVolume(value));
        menuSoundSlider.onValueChanged.AddListener(value => SettingsManager.Instance.SetMenuSoundVolume(value));
        cameraZoomSlider.onValueChanged.AddListener(value => SettingsManager.Instance.SetCameraZoom(value));

        musicSlider.minValue = 0f;
        musicSlider.maxValue = 1f;
        gameSoundSlider.minValue = 0f;
        gameSoundSlider.maxValue = 1f;
        menuSoundSlider.minValue = 0f;
        menuSoundSlider.maxValue = 1f;
        cameraZoomSlider.minValue = zoomMin;
        cameraZoomSlider.maxValue = zoomMax;
    }

    void OnEnable()
    {
        BuildAvailableResolutions();
        RefreshUI();
    }

    void BuildAvailableResolutions()
    {
        _availableResolutions.Clear();
        var screenResolutions = Screen.resolutions;
        foreach (var allowed in allowedResolutions)
        {
            foreach (var screen in screenResolutions)
            {
                if (screen.width == allowed.x && screen.height == allowed.y)
                {
                    _availableResolutions.Add(allowed);
                    break;
                }
            }
        }
        if (_availableResolutions.Count == 0)
        {
            var fallback = Screen.currentResolution;
            _availableResolutions.Add(new Vector2Int(fallback.width, fallback.height));
        }
    }

    void RefreshUI()
    {
        musicSlider.SetValueWithoutNotify(SettingsManager.Instance.Current.musicVolume);
        gameSoundSlider.SetValueWithoutNotify(SettingsManager.Instance.Current.gameSoundVolume);
        menuSoundSlider.SetValueWithoutNotify(SettingsManager.Instance.Current.menuSoundVolume);
        cameraZoomSlider.SetValueWithoutNotify(SettingsManager.Instance.Current.cameraZoom);

        fullscreenButtonLabel.text = SettingsManager.Instance.Current.fullscreen ? "Fullscreen" : "Windowed";

        int index = Mathf.Clamp(SettingsManager.Instance.Current.resolutionIndex, 0, _availableResolutions.Count - 1);
        resolutionButtonLabel.text = ResolutionToString(index);
    }

    private string ResolutionToString(int index)
    {
        index = Mathf.Clamp(index, 0, _availableResolutions.Count - 1);
        var r = _availableResolutions[index];
        return $"{r.x}x{r.y}";
    }

    public void OnFullscreenButtonClicked()
    {
        bool newValue = !SettingsManager.Instance.Current.fullscreen;
        SettingsManager.Instance.SetFullscreen(newValue);
        fullscreenButtonLabel.text = newValue ? "Fullscreen" : "Windowed";
    }

    public void OnResolutionButtonClicked()
    {
        var s = SettingsManager.Instance.Current;
        int index = (s.resolutionIndex + 1) % _availableResolutions.Count;
        var r = _availableResolutions[index];
        Screen.SetResolution(r.x, r.y, s.fullscreen ? FullScreenMode.ExclusiveFullScreen : FullScreenMode.Windowed);
        s.resolutionIndex = index;
        SaveManager.Instance.SaveSettings();
        resolutionButtonLabel.text = ResolutionToString(index);
    }
}