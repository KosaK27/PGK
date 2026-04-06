using UnityEngine;

public class CraftingUIManager : MonoBehaviour
{
    public static CraftingUIManager Instance { get; private set; }

    private CraftingStationDefinition _openStation;
    private MultitileObject _openObject;
    private Transform _player;
    private const float OpenRange = 5f;

    public bool IsOpen => _openStation != null;
    public MultitileObject OpenObject => _openObject;

    public event System.Action<CraftingStationDefinition> OnStationOpened;
    public event System.Action OnStationClosed;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Update()
    {
        if (!IsOpen) return;
        if (_player == null)
            _player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (_openObject == null) { CloseStation(); return; }

        var center = new Vector2(
            _openObject.Origin.x + _openObject.Definition.size.x * 0.5f,
            _openObject.Origin.y + _openObject.Definition.size.y * 0.5f);

        if (Vector2.Distance(_player.position, center) > OpenRange)
            CloseStation();
    }

    public void OpenStation(CraftingStationDefinition def, MultitileObject source)
    {
        if (IsOpen) CloseStation();
        _openStation = def;
        _openObject = source;
        _player = GameObject.FindGameObjectWithTag("Player")?.transform;
        OnStationOpened?.Invoke(def);
    }

    public void CloseStation()
    {
        _openStation = null;
        _openObject = null;
        OnStationClosed?.Invoke();
    }
}