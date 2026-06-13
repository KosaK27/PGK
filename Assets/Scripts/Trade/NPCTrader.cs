using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Collider2D))]
public class NPCTrader : MonoBehaviour
{
    public TradeDefinition tradeDefinition;
    private Collider2D _myCollider;

    private void Start()
    {
        _myCollider = GetComponent<Collider2D>();
    }

    private void Update()
    {
        if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
        {
            Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
            Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(mouseScreenPos);

            if (_myCollider == _myCollider.OverlapPoint(mouseWorldPos))
            {
                if (tradeDefinition != null && tradeDefinition.offers.Count > 0 && TradingUI.Instance != null)
                {
                    TradingUI.Instance.OpenTrade(tradeDefinition);
                }
            }
        }
    }
}