using UnityEngine;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }

    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioClip normalMusic;
    [SerializeField] private AudioClip bossMusic;
    [SerializeField] private float fadeSpeed = 1.5f;

    private AudioClip _targetClip;
    private bool _fading;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        PlayNormal();
    }

    void Update()
    {
        if (!_fading) return;

        musicSource.volume -= fadeSpeed * Time.deltaTime;
        if (musicSource.volume <= 0f)
        {
            musicSource.clip = _targetClip;
            musicSource.Play();
            musicSource.volume = 1f;
            _fading = false;
        }
    }

    public void PlayNormal() => SwitchTo(normalMusic);
    public void PlayBoss() => SwitchTo(bossMusic);

    private void SwitchTo(AudioClip clip)
    {
        if (musicSource.clip == clip) return;
        _targetClip = clip;
        _fading = true;
    }
}