using UnityEngine;

public class PlayerAudioManager : MonoBehaviour
{
    public static PlayerAudioManager Instance { get; private set; }

    [Header("Audio Source")]
    [SerializeField] private AudioSource audioSource;

    [Header("Footsteps")]
    [SerializeField] private AudioClip[] footstepSounds;
    [SerializeField] private float footstepInterval = 0.35f;
    [SerializeField] private float footstepVolume = 1f;

    [Header("Movement")]
    [SerializeField] private AudioClip jumpSound;
    [SerializeField] private AudioClip doubleJumpSound;
    [SerializeField] private AudioClip dashSound;
    [SerializeField] private float movementVolume = 1f;

    [Header("Combat")]
    [SerializeField] private AudioClip swordSwingSound;
    [SerializeField] private AudioClip toolSwingSound;
    [SerializeField] private AudioClip bowShootSound;
    [SerializeField] private AudioClip playerHitSound;
    [SerializeField] private float combatVolume = 1f;

    [Header("Enemy")]
    [SerializeField] private AudioClip enemyHitSound;
    [SerializeField] private float enemyHitVolume = 1f;

    private int _footstepIndex;
    private float _footstepTimer;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
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

        audioSource.PlayOneShot(footstepSounds[_footstepIndex], footstepVolume);
        _footstepIndex = (_footstepIndex + 1) % footstepSounds.Length;
        _footstepTimer = footstepInterval;
    }

    public void PlayJump() => audioSource.PlayOneShot(jumpSound, movementVolume);
    public void PlayDoubleJump() => audioSource.PlayOneShot(doubleJumpSound, movementVolume);
    public void PlayDash() => audioSource.PlayOneShot(dashSound, movementVolume);
    public void PlaySwordSwing() => audioSource.PlayOneShot(swordSwingSound, combatVolume);
    public void PlayToolSwing() => audioSource.PlayOneShot(toolSwingSound, combatVolume);
    public void PlayBowShoot() => audioSource.PlayOneShot(bowShootSound, combatVolume);
    public void PlayPlayerHit() => audioSource.PlayOneShot(playerHitSound, combatVolume);
    public void PlayEnemyHit() => audioSource.PlayOneShot(enemyHitSound, enemyHitVolume);
}