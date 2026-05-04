using UnityEngine;

public class BlockAudioManager : MonoBehaviour
{
    public static BlockAudioManager Instance { get; private set; }

    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip placeSound;
    [SerializeField] private AudioClip breakSound;
    [SerializeField] private float placeVolume = 1f;
    [SerializeField] private float breakVolume = 1f;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void PlayPlace() => audioSource.PlayOneShot(placeSound, placeVolume);
    public void PlayBreak() => audioSource.PlayOneShot(breakSound, breakVolume);
}