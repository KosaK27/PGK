using UnityEngine;

public class UIAudioManager : MonoBehaviour
{
    public static UIAudioManager Instance { get; private set; }

    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip hoverSound;
    [SerializeField] private AudioClip clickSound;

    private float _volume = 1f;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        if (SettingsManager.Instance != null)
        {
            _volume = SettingsManager.Instance.Current.menuSoundVolume;
            SettingsManager.Instance.OnMenuSoundVolumeChanged += v => _volume = v;
        }
    }

    public void PlayHover() => audioSource.PlayOneShot(hoverSound, _volume);
    public void PlayClick() => audioSource.PlayOneShot(clickSound, _volume);
}