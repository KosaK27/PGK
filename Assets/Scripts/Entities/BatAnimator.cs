using UnityEngine;

public class BatWingAnimator : MonoBehaviour
{
    [Header("Assign in Inspector")]
    public SpriteRenderer spriteRenderer;
    public Sprite wingsUp;
    public Sprite wingsMiddle;
    public Sprite wingsDown;

    [Header("Animation Settings")]
    public float framesPerSecond = 10f;

    private float timer;
    private int index;

    private Sprite[] frames;

    void Start()
    {
        frames = new Sprite[4];
        frames[0] = wingsUp;
        frames[1] = wingsMiddle;
        frames[2] = wingsDown;
        frames[3] = wingsMiddle;

        index = 0;
        timer = 0f;

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        timer += Time.deltaTime;

        if (timer >= 1f / framesPerSecond)
        {
            timer = 0f;

            index++;
            if (index >= frames.Length)
                index = 0;

            spriteRenderer.sprite = frames[index];
        }
    }
}