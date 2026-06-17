using UnityEngine;

public class PlayerAudioManager : MonoBehaviour
{
    public static PlayerAudioManager Instance { get; private set; }

    [SerializeField] private AudioSource audioSource;

    [SerializeField] private AudioClip[] footstepSounds;
    [SerializeField] private float footstepInterval = 0.35f;

    [SerializeField] private AudioClip jumpSound;
    [SerializeField] private AudioClip doubleJumpSound;
    [SerializeField] private AudioClip dashSound;

    [SerializeField] private AudioClip swordSwingSound;
    [SerializeField] private AudioClip toolSwingSound;
    [SerializeField] private AudioClip bowShootSound;
    [SerializeField] private AudioClip playerHitSound;
    [SerializeField] private AudioClip enemyHitSound;

    [SerializeField] private AudioClip tornadoSound;
    [SerializeField] private AudioClip chargeSound;
    [SerializeField] private AudioClip lightningSound;

    private float _volume = 1f;
    private int _footstepIndex;
    private float _footstepTimer;

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

    void Update()
    {
        if (_footstepTimer > 0f)
            _footstepTimer -= Time.deltaTime;
    }

    public void TryPlayFootstep(bool isMoving, bool isGrounded)
    {
        if (!isMoving || !isGrounded) return;
        if (_footstepTimer > 0f) return;
        if (footstepSounds == null || footstepSounds.Length == 0) return;
        audioSource.PlayOneShot(footstepSounds[_footstepIndex], _volume);
        _footstepIndex = (_footstepIndex + 1) % footstepSounds.Length;
        _footstepTimer = footstepInterval;
    }

    public void PlayJump() => audioSource.PlayOneShot(jumpSound, _volume);
    public void PlayDoubleJump() => audioSource.PlayOneShot(doubleJumpSound, _volume);
    public void PlayDash() => audioSource.PlayOneShot(dashSound, _volume);
    public void PlaySwordSwing() => audioSource.PlayOneShot(swordSwingSound, _volume);
    public void PlayToolSwing() => audioSource.PlayOneShot(toolSwingSound, _volume);
    public void PlayBowShoot() => audioSource.PlayOneShot(bowShootSound, _volume);
    public void PlayPlayerHit() => audioSource.PlayOneShot(playerHitSound, _volume);
    public void PlayEnemyHit() => audioSource.PlayOneShot(enemyHitSound, _volume);
    public void PlayTornado() => audioSource.PlayOneShot(tornadoSound, _volume);
    public void PlayCharge() => audioSource.PlayOneShot(chargeSound, _volume);
    public void PlayLightning() => audioSource.PlayOneShot(lightningSound, _volume);
}