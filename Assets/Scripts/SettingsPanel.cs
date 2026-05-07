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
    [SerializeField] private float zoomMin = 5f;
    [SerializeField] private float zoomMax = 25f;

    private List<Vector2Int> _availableResolutions = new();
    private List<int> _availableResolutionIndices = new();

    void Awake()
    {
        musicSlider.minValue = 0f;
        musicSlider.maxValue = 1f;
        gameSoundSlider.minValue = 0f;
        gameSoundSlider.maxValue = 1f;
        menuSoundSlider.minValue = 0f;
        menuSoundSlider.maxValue = 1f;
        cameraZoomSlider.minValue = zoomMin;
        cameraZoomSlider.maxValue = zoomMax;
        musicSlider.onValueChanged.AddListener(value => SettingsManager.Instance.SetMusicVolume(value));
        gameSoundSlider.onValueChanged.AddListener(value => SettingsManager.Instance.SetGameSoundVolume(value));
        menuSoundSlider.onValueChanged.AddListener(value => SettingsManager.Instance.SetMenuSoundVolume(value));
        cameraZoomSlider.onValueChanged.AddListener(value => SettingsManager.Instance.SetCameraZoom(zoomMax + zoomMin - value));
    }

    void OnEnable()
    {
        BuildAvailableResolutions();
        RefreshUI();
    }

    void BuildAvailableResolutions()
    {
        _availableResolutions.Clear();
        _availableResolutionIndices.Clear();
        var screenResolutions = Screen.resolutions;
        for (int i = 0; i < screenResolutions.Length; i++)
        {
            var sr = screenResolutions[i];
            foreach (var allowed in allowedResolutions)
            {
                if (sr.width == allowed.x && sr.height == allowed.y)
                {
                    if (!_availableResolutions.Contains(allowed))
                    {
                        _availableResolutions.Add(allowed);
                        _availableResolutionIndices.Add(i);
                    }
                    break;
                }
            }
        }
        if (_availableResolutions.Count == 0)
        {
            var fallback = Screen.currentResolution;
            _availableResolutions.Add(new Vector2Int(fallback.width, fallback.height));
            _availableResolutionIndices.Add(0);
        }
    }

    void RefreshUI()
    {
        musicSlider.SetValueWithoutNotify(SettingsManager.Instance.Current.musicVolume);
        gameSoundSlider.SetValueWithoutNotify(SettingsManager.Instance.Current.gameSoundVolume);
        menuSoundSlider.SetValueWithoutNotify(SettingsManager.Instance.Current.menuSoundVolume);
        cameraZoomSlider.SetValueWithoutNotify(zoomMax + zoomMin - SettingsManager.Instance.Current.cameraZoom);
        fullscreenButtonLabel.text = SettingsManager.Instance.Current.fullscreen ? "Fullscreen" : "Windowed";
        resolutionButtonLabel.text = GetCurrentResolutionString();
    }

    private string GetCurrentResolutionString()
    {
        int savedIndex = SettingsManager.Instance.Current.resolutionIndex;
        for (int i = 0; i < _availableResolutionIndices.Count; i++)
            if (_availableResolutionIndices[i] == savedIndex)
                return $"{_availableResolutions[i].x}x{_availableResolutions[i].y}";
        if (_availableResolutions.Count > 0)
            return $"{_availableResolutions[0].x}x{_availableResolutions[0].y}";
        return "";
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
        int currentLocalIndex = 0;
        for (int i = 0; i < _availableResolutionIndices.Count; i++)
            if (_availableResolutionIndices[i] == s.resolutionIndex) { currentLocalIndex = i; break; }
        int nextLocalIndex = (currentLocalIndex + 1) % _availableResolutions.Count;
        int screenIndex = _availableResolutionIndices[nextLocalIndex];
        SettingsManager.Instance.SetResolution(screenIndex, s.fullscreen);
        resolutionButtonLabel.text = $"{_availableResolutions[nextLocalIndex].x}x{_availableResolutions[nextLocalIndex].y}";
    }
}