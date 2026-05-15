using UnityEngine;
public class DayNightSystem : MonoBehaviour
{
    public static DayNightSystem Instance { get; private set; }
    [Header("Czas dnia")]
    public float dayLengthSeconds = 120f;
    [Header("Reczne sterowanie")]
    [Range(0f, 1f)]
    public float timeOfDay = 0.25f;
    public bool autoAdvanceTime = true;
    [Header("Kolory nieba")]
    public Color dayColor = new Color(0.53f, 0.81f, 0.98f);
    public Color nightColor = new Color(0.02f, 0.02f, 0.08f);
    public Color dawnColor = new Color(0.90f, 0.55f, 0.25f);
    public bool IsDay => timeOfDay >= 0.22f && timeOfDay < 0.78f;
    public bool IsNight => !IsDay;
    public float AmbientBrightness { get; private set; }
    public event System.Action<bool> OnDayNightChanged;
    private bool _wasDayLastFrame;
    private Camera _mainCamera;
    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        _wasDayLastFrame = IsDay;
    }
    void Start()
    {
        _mainCamera = Camera.main;
    }
    void Update()
    {
        if (autoAdvanceTime)
        {
            timeOfDay += Time.deltaTime / dayLengthSeconds;
            if (timeOfDay >= 1f) timeOfDay -= 1f;
        }

        AmbientBrightness = CalculateBrightness();

        ApplySkyColor();

        bool isNowDay = IsDay;
        if (isNowDay != _wasDayLastFrame)
            OnDayNightChanged?.Invoke(isNowDay);
        _wasDayLastFrame = isNowDay;

        LightingSystem.Instance?.UpdateAmbientOnly();
    }
    private void ApplySkyColor()
    {
        if (_mainCamera == null) _mainCamera = Camera.main;
        if (_mainCamera == null) return;
        _mainCamera.backgroundColor = GetSkyColor();
    }
    public Color GetSkyColor()
    {
        if (timeOfDay < 0.22f)
            return Color.Lerp(nightColor, dawnColor, Mathf.SmoothStep(0f, 1f, timeOfDay / 0.22f));
        if (timeOfDay < 0.30f)
            return Color.Lerp(dawnColor, dayColor, Mathf.SmoothStep(0f, 1f, (timeOfDay - 0.22f) / 0.08f));
        if (timeOfDay < 0.70f)
            return dayColor;
        if (timeOfDay < 0.78f)
            return Color.Lerp(dayColor, dawnColor, Mathf.SmoothStep(0f, 1f, (timeOfDay - 0.70f) / 0.08f));
        return Color.Lerp(dawnColor, nightColor, Mathf.SmoothStep(0f, 1f, (timeOfDay - 0.78f) / 0.22f));
    }
    private float CalculateBrightness()
    {
        if (timeOfDay < 0.22f || timeOfDay >= 0.78f) return 0.05f;
        if (timeOfDay < 0.30f) return Mathf.SmoothStep(0.05f, 1f, (timeOfDay - 0.22f) / 0.08f);
        if (timeOfDay < 0.70f) return 1f;
        return Mathf.SmoothStep(1f, 0.05f, (timeOfDay - 0.70f) / 0.08f);
    }
    public float GetTimeOfDay() => timeOfDay;
    public void SetTimeOfDay(float t) => timeOfDay = Mathf.Repeat(t, 1f);
}