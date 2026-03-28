using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerToolHolder : MonoBehaviour
{
    [SerializeField] private SpriteRenderer frontArmRenderer;

    [SerializeField] private Sprite armUp;
    [SerializeField] private Sprite armForward;
    [SerializeField] private Sprite armDown;

    [SerializeField] private Vector2 toolPosUp      = new(0f,    0.5f);
    [SerializeField] private Vector2 toolPosForward = new(0.5f,  0.2f);
    [SerializeField] private Vector2 toolPosDown    = new(0.3f, -0.5f);

    [SerializeField] private float toolRotUp      = -45f;
    [SerializeField] private float toolRotForward =   0f;
    [SerializeField] private float toolRotDown    =  45f;

    [SerializeField] private Transform toolRoot;

    [SerializeField] private GameObject swordHitboxPrefab;
    [SerializeField] private Vector2    hitboxSize = new(2.5f, 0.6f);

    [SerializeField] private float swordSwingDuration = 0.25f;
    [SerializeField] private float swordArcDegrees    = 120f;

    private SpriteRenderer _toolRenderer;
    private Camera         _cam;

    private float _mineTimer   = 0f;
    private bool  _mineForward = true;

    private bool  _isSwinging      = false;
    private float _swingTimer      = 0f;
    private float _swingStartAngle = 0f;
    private float _swingEndAngle   = 0f;

    private enum ArmZone { Up, Forward, Down }
    private ArmZone _currentZone = ArmZone.Forward;

    private GameObject _activeHitbox;

    void Awake()
    {
        _cam = Camera.main;
        if (toolRoot != null)
            _toolRenderer = toolRoot.GetComponentInChildren<SpriteRenderer>();
    }

    void Update()
    {
        var selected = InventorySystem.Instance.SelectedItem;
        bool hasTool = selected != null && !selected.IsEmpty && selected.item.isTool;

        if (!hasTool)
        {
            HideTool();
            return;
        }

        bool lmbHeld     = Mouse.current.leftButton.isPressed;
        bool lmbPressed  = Mouse.current.leftButton.wasPressedThisFrame;
        bool lmbReleased = Mouse.current.leftButton.wasReleasedThisFrame;

        if (lmbReleased)
        {
            _mineTimer   = 0f;
            _mineForward = true;
        }

        if (lmbHeld || _isSwinging)
        {
            if (lmbHeld) FaceTowardCursor();
            _currentZone = GetZoneFromCursor();

            if (_toolRenderer != null)
                _toolRenderer.sprite = selected.item.sprite;

            ApplyZoneArmAndPosition(true);

            if (selected.item.toolType == ToolType.Sword)
                UpdateSwordAnimation(lmbPressed, lmbHeld);
            else
                UpdateMineAnimation(selected.item, lmbHeld);
        }
        else
        {
            if (_toolRenderer != null) _toolRenderer.enabled = false;
            if (selected.item.toolType == ToolType.Sword)
                UpdateSwordAnimation(lmbPressed, lmbHeld);
        }
    }

    private void FaceTowardCursor()
    {
        Vector2 playerScreen = _cam.WorldToScreenPoint(transform.position);
        Vector2 mouseScreen  = Mouse.current.position.ReadValue();
        bool cursorRight     = mouseScreen.x > playerScreen.x;
        Vector3 scale        = transform.localScale;
        float wanted         = cursorRight ? -1f : 1f;
        if (Mathf.Approximately(scale.x, wanted)) return;
        scale.x = wanted;
        transform.localScale = scale;
    }

    private ArmZone GetZoneFromCursor()
    {
        Vector2 playerScreen = _cam.WorldToScreenPoint(transform.position);
        Vector2 mouseScreen  = Mouse.current.position.ReadValue();
        Vector2 delta        = mouseScreen - playerScreen;

        if (transform.localScale.x > 0) delta.x = -delta.x;

        float angle = Mathf.Atan2(delta.y, Mathf.Abs(delta.x)) * Mathf.Rad2Deg;

        if (angle > 45f)  return ArmZone.Up;
        if (angle < -45f) return ArmZone.Down;
        return ArmZone.Forward;
    }

    private void ApplyZoneArmAndPosition(bool toolVisible)
    {
        Vector2 pos;
        float   baseRot;
        Sprite  armSprite;

        switch (_currentZone)
        {
            case ArmZone.Up:
                pos = toolPosUp; baseRot = toolRotUp; armSprite = armUp;
                break;
            case ArmZone.Down:
                pos = toolPosDown; baseRot = toolRotDown; armSprite = armDown;
                break;
            default:
                pos = toolPosForward; baseRot = toolRotForward; armSprite = armForward;
                break;
        }

        if (frontArmRenderer != null && armSprite != null)
            frontArmRenderer.sprite = armSprite;

        if (toolRoot != null)
        {
            toolRoot.localPosition = pos;
            if (!_isSwinging && _mineTimer == 0f)
                toolRoot.localRotation = Quaternion.Euler(0, 0, baseRot);
        }

        if (_toolRenderer != null)
            _toolRenderer.enabled = toolVisible;
    }

    private void HideTool()
    {
        if (_toolRenderer != null) _toolRenderer.enabled = false;
        _isSwinging  = false;
        _mineTimer   = 0f;
        _mineForward = true;
        ClearHitbox();
    }

    private void UpdateMineAnimation(ItemDefinition item, bool lmbHeld)
    {
        if (!lmbHeld)
        {
            _mineTimer   = 0f;
            _mineForward = true;
            if (toolRoot != null)
                toolRoot.localRotation = Quaternion.Euler(0, 0, GetBaseRot());
            return;
        }

        float halfDur    = (1f / item.breakingSpeed) * 0.5f;
        float baseRot    = GetBaseRot();
        float startAngle = baseRot;
        float endAngle   = baseRot + 90f;

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

        float angle = Mathf.Lerp(startAngle, endAngle, t);
        if (toolRoot != null)
            toolRoot.localRotation = Quaternion.Euler(0, 0, angle);
    }

    private void UpdateSwordAnimation(bool lmbPressed, bool lmbHeld)
    {
        if (_isSwinging)
        {
            _swingTimer += Time.deltaTime;
            float t = Mathf.Clamp01(_swingTimer / swordSwingDuration);
            float angle = Mathf.Lerp(_swingStartAngle, _swingEndAngle, t);
            if (toolRoot != null)
                toolRoot.localRotation = Quaternion.Euler(0, 0, angle);

            if (t >= 0.5f && _activeHitbox == null)
                SpawnSwordHitbox();

            if (t >= 1f)
            {
                _isSwinging = false;
                ClearHitbox();
                if (lmbHeld) StartSwing();
            }
            return;
        }

        if (lmbPressed) StartSwing();
    }

    private void StartSwing()
    {
        FaceTowardCursor();

        Vector2 playerScreen = _cam.WorldToScreenPoint(transform.position);
        Vector2 mouseScreen  = Mouse.current.position.ReadValue();
        Vector2 delta        = mouseScreen - playerScreen;

        if (transform.localScale.x > 0) delta.x = -delta.x;

        float aimAngle   = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
        _swingStartAngle = aimAngle - swordArcDegrees * 0.5f;
        _swingEndAngle   = aimAngle + swordArcDegrees * 0.5f;
        _swingTimer      = 0f;
        _isSwinging      = true;
    }

    private float GetBaseRot() => _currentZone switch
    {
        ArmZone.Up   => toolRotUp,
        ArmZone.Down => toolRotDown,
        _            => toolRotForward,
    };

    private void SpawnSwordHitbox()
    {
        if (swordHitboxPrefab == null) return;

        Vector3    pos = toolRoot != null ? toolRoot.position : transform.position;
        Quaternion rot = toolRoot != null ? toolRoot.rotation : Quaternion.identity;

        _activeHitbox = Instantiate(swordHitboxPrefab, pos, rot);

        var col = _activeHitbox.GetComponent<BoxCollider2D>();
        if (col != null) col.size = hitboxSize;

        var sr = _activeHitbox.GetComponent<SpriteRenderer>();
        if (sr != null) sr.size = hitboxSize;

        Destroy(_activeHitbox, swordSwingDuration * 0.5f);
    }

    private void ClearHitbox()
    {
        if (_activeHitbox != null) { Destroy(_activeHitbox); _activeHitbox = null; }
    }
}