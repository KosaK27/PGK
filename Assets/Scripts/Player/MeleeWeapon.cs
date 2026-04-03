using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerArmAnimator))]
public class MeleeWeapon : MonoBehaviour
{
    [SerializeField] private float swingDuration = 0.25f;
    [SerializeField] private float arcDegrees = 120f;
    [SerializeField] private GameObject hitboxPrefab;

    private const float SPRITE_ANGLE_OFFSET = 45f;

    private PlayerArmAnimator _arm;
    private SpriteRenderer _toolRenderer;
    private Transform _toolRoot;
    private Camera _cam;

    private bool _isSwinging;
    private float _swingTimer;
    private float _localStartAngle;
    private float _localEndAngle;
    private float _hitboxAngle;
    private bool _swingFromTop = true;

    private GameObject _activeHitbox;
    private Vector3 _hitboxOffset;
    private int _currentDamage;
    private Vector2 _currentHitboxSize;
    private float _currentHitboxDistance;

    void Awake()
    {
        _arm = GetComponent<PlayerArmAnimator>();
        _cam = Camera.main;
    }

    public void Setup(Transform toolRoot, SpriteRenderer toolRenderer)
    {
        _toolRoot = toolRoot;
        _toolRenderer = toolRenderer;
    }

    public bool IsSwinging => _isSwinging;

    public void UpdateWeapon(ItemDefinition item, bool lmbPressed, bool lmbHeld)
    {
        _currentDamage = item.damage;
        _currentHitboxSize = item.hitboxSize;
        _currentHitboxDistance = item.hitboxDistance;

        _arm.UpdateZone();
        _arm.FaceTowardCursor();

        var zone = _arm.CurrentZone;
        Vector2 pos = zone switch
        {
            PlayerArmAnimator.ArmZone.Up => item.holdPositionUp,
            PlayerArmAnimator.ArmZone.Down => item.holdPositionDown,
            _ => item.holdPositionForward
        };
        if (_toolRoot != null) _toolRoot.localPosition = pos;

        if (_isSwinging)
        {
            _arm.SetArmSprite();

            _swingTimer += Time.deltaTime;
            float t = Mathf.Clamp01(_swingTimer / swingDuration);
            float localAngle = Mathf.Lerp(_localStartAngle, _localEndAngle, t);

            if (_toolRoot != null)
                _toolRoot.localRotation = Quaternion.Euler(0, 0, localAngle + SPRITE_ANGLE_OFFSET);

            if (t >= 0.5f && _activeHitbox == null)
                SpawnHitbox(_hitboxAngle);

            if (_activeHitbox != null)
                _activeHitbox.transform.position = transform.position + _hitboxOffset;

            if (t >= 1f)
            {
                _isSwinging = false;
                ClearHitbox();
                if (_toolRenderer != null) _toolRenderer.enabled = false;
                if (lmbHeld) StartSwing(item);
            }
            return;
        }

        if (_toolRenderer != null) _toolRenderer.enabled = false;
        if (lmbPressed) StartSwing(item);
    }

    public void Cancel()
    {
        _isSwinging = false;
        _swingFromTop = true;
        ClearHitbox();
        if (_toolRenderer != null) _toolRenderer.enabled = false;
    }

    private void StartSwing(ItemDefinition item)
    {
        _arm.FaceTowardCursor();

        Vector3 mouseWorld = _cam.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        mouseWorld.z = 0f;

        Vector2 dir = ((Vector2)(mouseWorld - transform.position)).normalized;
        _hitboxAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        if (transform.localScale.x > 0f) dir.x = -dir.x;
        dir.y = -dir.y;
        float visualAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        if (_swingFromTop)
        {
            _localStartAngle = visualAngle - arcDegrees * 0.5f;
            _localEndAngle = visualAngle + arcDegrees * 0.5f;
        }
        else
        {
            _localStartAngle = visualAngle + arcDegrees * 0.5f;
            _localEndAngle = visualAngle - arcDegrees * 0.5f;
        }

        _swingFromTop = !_swingFromTop;
        _swingTimer = 0f;
        _isSwinging = true;

        if (_toolRenderer != null)
        {
            _toolRenderer.enabled = true;
            _toolRenderer.sprite = item.sprite;
        }
    }

    private void SpawnHitbox(float worldAngle)
    {
        if (hitboxPrefab == null) return;

        Vector2 worldDir = new Vector2(Mathf.Cos(worldAngle * Mathf.Deg2Rad), Mathf.Sin(worldAngle * Mathf.Deg2Rad));
        _hitboxOffset = (Vector3)(worldDir * (_currentHitboxDistance + _currentHitboxSize.x * 0.5f));

        _activeHitbox = Instantiate(hitboxPrefab, transform.position + _hitboxOffset, Quaternion.Euler(0, 0, worldAngle));
        _activeHitbox.transform.localScale = new Vector3(_currentHitboxSize.x, _currentHitboxSize.y, 1f);

        var col = _activeHitbox.GetComponent<BoxCollider2D>();
        if (col != null) col.size = Vector2.one;

        var hitbox = _activeHitbox.GetComponent<SwordHitbox>();
        if (hitbox != null) hitbox.Init(_currentDamage);
    }

    private void ClearHitbox()
    {
        if (_activeHitbox != null) { Destroy(_activeHitbox); _activeHitbox = null; }
    }
}