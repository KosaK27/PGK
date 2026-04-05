using UnityEngine;

public class ContainerUIManager : MonoBehaviour
{
    public static ContainerUIManager Instance { get; private set; }

    private IContainer _openContainer;
    private MultitileObject _openObject;
    public MultitileObject OpenObject => _openObject;
    private float _openRange = 5f;
    private Transform _player;

    public IContainer CurrentContainer => _openContainer;
    public bool IsOpen => _openContainer != null;

    public event System.Action<IContainer> OnContainerOpened;
    public event System.Action OnContainerClosed;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Update()
    {
        if (!IsOpen) return;
        if (_player == null) _player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (_player == null) return;

        if (_openObject == null)
        {
            CloseContainer();
            return;
        }

        var center = new Vector2(
            _openObject.Origin.x + _openObject.Definition.size.x * 0.5f,
            _openObject.Origin.y + _openObject.Definition.size.y * 0.5f);

        if (Vector2.Distance(_player.position, center) > _openRange)
            CloseContainer();
    }

    public void OpenContainer(IContainer container, MultitileObject source)
    {
        if (_openContainer != null) CloseContainer();

        _openContainer = container;
        _openObject = source;
        _player = GameObject.FindGameObjectWithTag("Player")?.transform;

        OnContainerOpened?.Invoke(container);
    }

    public void CloseContainer()
    {
        _openContainer = null;
        _openObject = null;
        OnContainerClosed?.Invoke();
    }
}