using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAnimation : MonoBehaviour
{
    [Header("Base (unarmored) sprites")]
    public Sprite headDefault;
    public Sprite torsoDefault;
    public Sprite legsIdle;
    public Sprite[] legsWalk = new Sprite[4];
    public Sprite legsJump;
    public Sprite frontArmIdle;
    public Sprite[] frontArmWalk = new Sprite[4];
    public Sprite frontArmJump;
    public Sprite backArmIdle;
    public Sprite[] backArmWalk = new Sprite[4];
    public Sprite backArmJump;
    [Header("Renderers")]
    public SpriteRenderer headRenderer;
    public SpriteRenderer hairRenderer;
    public SpriteRenderer torsoRenderer;
    public SpriteRenderer legsRenderer;
    public SpriteRenderer frontArmRenderer;
    public SpriteRenderer backArmRenderer;
    public float walkFrameRate = 8f;
    private PlayerMovement _movement;
    private float _walkTimer;
    private int _walkFrame;
    private HeadArmorDefinition _headArmor;
    private ChestArmorDefinition _chestArmor;
    private LegsArmorDefinition _legsArmor;
    void Start()
    {
        _movement = GetComponent<PlayerMovement>();
        ArmorSystem.Instance.OnArmorChanged += RefreshArmorCache;
        RefreshArmorCache();
    }
    void OnDestroy()
    {
        if (ArmorSystem.Instance != null)
            ArmorSystem.Instance.OnArmorChanged -= RefreshArmorCache;
    }
    public void RefreshArmorCache()
    {
        _headArmor = ArmorSystem.Instance.GetSlot<HeadArmorDefinition>(ArmorSlot.Head);
        _chestArmor = ArmorSystem.Instance.GetSlot<ChestArmorDefinition>(ArmorSlot.Chest);
        _legsArmor = ArmorSystem.Instance.GetSlot<LegsArmorDefinition>(ArmorSlot.Legs);
        if (hairRenderer != null)
            hairRenderer.enabled = _headArmor == null;
        if (headRenderer != null)
            headRenderer.sprite = _headArmor?.headSprite ?? headDefault;
        if (torsoRenderer != null)
            torsoRenderer.sprite = _chestArmor?.torsoSprite ?? torsoDefault;
    }
    void Update()
    {
        if (_movement.ActionState == PlayerActionState.Dead) return;
        bool movingLeft = Keyboard.current.aKey.isPressed;
        bool movingRight = Keyboard.current.dKey.isPressed;
        if (movingRight) transform.localScale = new Vector3(-1f, 1f, 1f);
        else if (movingLeft) transform.localScale = new Vector3(1f, 1f, 1f);
        bool inAir = _movement.AirState == PlayerAirState.Jumping;
        bool walking = _movement.LocomotionState == PlayerLocomotionState.Walk;
        if (inAir || _movement.IsDashing)
        {
            SetLegs(_legsArmor?.legsJump ?? legsJump);
            SetArms(_chestArmor?.armFJump ?? frontArmJump, _chestArmor?.armBJump ?? backArmJump);
            _walkTimer = 0f;
            _walkFrame = 0;
            return;
        }
        if (walking)
        {
            _walkTimer += Time.deltaTime;
            if (_walkTimer >= 1f / walkFrameRate)
            {
                _walkTimer = 0f;
                _walkFrame = (_walkFrame + 1) % 4;
            }
            SetLegs(_legsArmor?.legsWalk[_walkFrame] ?? legsWalk[_walkFrame]);
            SetArms(_chestArmor?.armFWalk[_walkFrame] ?? frontArmWalk[_walkFrame], _chestArmor?.armBWalk[_walkFrame] ?? backArmWalk[_walkFrame]);
            return;
        }
        _walkTimer = 0f;
        _walkFrame = 0;
        SetLegs(_legsArmor?.legsIdle ?? legsIdle);
        SetArms(_chestArmor?.armFIdle ?? frontArmIdle, _chestArmor?.armBIdle ?? backArmIdle);
    }
    private void SetLegs(Sprite sprite)
    {
        if (legsRenderer != null) legsRenderer.sprite = sprite;
    }
    private void SetArms(Sprite front, Sprite back)
    {
        if (_movement.ActionState != PlayerActionState.UsingTool &&
            _movement.ActionState != PlayerActionState.UsingWeapon)
            if (frontArmRenderer != null) frontArmRenderer.sprite = front;
        if (backArmRenderer != null) backArmRenderer.sprite = back;
    }
}