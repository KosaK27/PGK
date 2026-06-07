using UnityEngine;

public class BlockAudioManager : MonoBehaviour
{
    public static BlockAudioManager Instance { get; private set; }

    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip placeSound;
    [SerializeField] private AudioClip breakSound;
    [SerializeField] private AudioClip explosionSound;

    private float _volume = 1f;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        if (SettingsManager.Instance != null)
        {
            _volume = SettingsManager.Instance.Current.gameSoundVolume;
            SettingsManager.Instance.OnGameSoundVolumeChanged += v => _volume = v;
        }
    }

    public void PlayPlace() => audioSource.PlayOneShot(placeSound, _volume);
    public void PlayBreak() => audioSource.PlayOneShot(breakSound, _volume);
    public void PlayExplosion() => audioSource.PlayOneShot(explosionSound, _volume);
}