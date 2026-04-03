using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerArmAnimator : MonoBehaviour
{
    public enum ArmZone { Up, Forward, Down }

    [SerializeField] private SpriteRenderer frontArmRenderer;
    [SerializeField] private Sprite armUp;
    [SerializeField] private Sprite armForward;
    [SerializeField] private Sprite armDown;

    [SerializeField] public Vector2 toolPosUp = new(0f, 0.5f);
    [SerializeField] public Vector2 toolPosForward = new(0.5f, 0.2f);
    [SerializeField] public Vector2 toolPosDown = new(0.3f, -0.5f);

    [SerializeField] public float toolRotUp = -45f;
    [SerializeField] public float toolRotForward = 0f;
    [SerializeField] public float toolRotDown = 45f;

    private Camera _cam;

    public ArmZone CurrentZone { get; private set; } = ArmZone.Forward;

    void Awake() => _cam = Camera.main;

    public void UpdateZone() => CurrentZone = GetZoneFromCursor();

    public void SetArmSprite()
    {
        if (frontArmRenderer == null) return;
        frontArmRenderer.sprite = CurrentZone switch
        {
            ArmZone.Up => armUp,
            ArmZone.Down => armDown,
            _ => armForward
        };
    }

    public void FaceTowardCursor()
    {
        Vector2 playerScreen = _cam.WorldToScreenPoint(transform.position);
        Vector2 mouseScreen = Mouse.current.position.ReadValue();
        bool cursorRight = mouseScreen.x > playerScreen.x;
        Vector3 scale = transform.localScale;
        float wanted = cursorRight ? -1f : 1f;
        if (Mathf.Approximately(scale.x, wanted)) return;
        scale.x = wanted;
        transform.localScale = scale;
    }

    public (Vector2 pos, float rot) GetZoneTransform() => CurrentZone switch
    {
        ArmZone.Up => (toolPosUp, toolRotUp),
        ArmZone.Down => (toolPosDown, toolRotDown),
        _ => (toolPosForward, toolRotForward)
    };

    private ArmZone GetZoneFromCursor()
    {
        Vector2 playerScreen = _cam.WorldToScreenPoint(transform.position);
        Vector2 mouseScreen = Mouse.current.position.ReadValue();
        Vector2 delta = mouseScreen - playerScreen;
        if (transform.localScale.x > 0) delta.x = -delta.x;
        float angle = Mathf.Atan2(delta.y, Mathf.Abs(delta.x)) * Mathf.Rad2Deg;
        if (angle > 45f) return ArmZone.Up;
        if (angle < -45f) return ArmZone.Down;
        return ArmZone.Forward;
    }
}