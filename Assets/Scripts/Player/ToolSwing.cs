using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerArmAnimator))]
public class ToolSwing : MonoBehaviour
{
    [SerializeField] private float angleOffsetUp = -45f;
    [SerializeField] private float angleOffsetForward = 0f;
    [SerializeField] private float angleOffsetDown = 45f;

    private PlayerArmAnimator _arm;
    private SpriteRenderer _toolRenderer;
    private Transform _toolRoot;

    private float _mineTimer;
    private bool _mineForward = true;

    void Awake() => _arm = GetComponent<PlayerArmAnimator>();

    public void Setup(Transform toolRoot, SpriteRenderer toolRenderer)
    {
        _toolRoot = toolRoot;
        _toolRenderer = toolRenderer;
    }

    public void UpdateSwing(ItemDefinition item, bool lmbHeld)
    {
        _arm.UpdateZone();
        _arm.FaceTowardCursor();

        var zone = _arm.CurrentZone;
        Vector2 pos = zone switch
        {
            PlayerArmAnimator.ArmZone.Up => item.holdPositionUp,
            PlayerArmAnimator.ArmZone.Down => item.holdPositionDown,
            _ => item.holdPositionForward
        };
        float baseRot = zone switch
        {
            PlayerArmAnimator.ArmZone.Up => angleOffsetUp,
            PlayerArmAnimator.ArmZone.Down => angleOffsetDown,
            _ => angleOffsetForward
        };

        if (_toolRoot != null)
        {
            _toolRoot.localPosition = pos;
            _toolRoot.localRotation = Quaternion.Euler(0, 0, baseRot);
        }

        if (!lmbHeld)
        {
            _mineTimer = 0f;
            _mineForward = true;
            if (_toolRenderer != null) _toolRenderer.enabled = false;
            return;
        }

        if (_toolRenderer != null)
        {
            _toolRenderer.enabled = true;
            _toolRenderer.sprite = item.sprite;
        }
        _arm.SetArmSprite();

        float halfDur = (1f / item.breakingSpeed) * 0.5f;
        _mineTimer += Time.deltaTime;

        float t;
        if (_mineForward)
        {
            t = Mathf.Clamp01(_mineTimer / halfDur);
            if (_mineTimer >= halfDur) { _mineTimer = 0f; _mineForward = false; }
        }
        else
        {
            t = 1f - Mathf.Clamp01(_mineTimer / halfDur);
            if (_mineTimer >= halfDur) { _mineTimer = 0f; _mineForward = true; }
        }

        if (_toolRoot != null)
            _toolRoot.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(baseRot, baseRot + 90f, t));
    }

    public void Cancel()
    {
        _mineTimer = 0f;
        _mineForward = true;
        if (_toolRenderer != null) _toolRenderer.enabled = false;
    }
}