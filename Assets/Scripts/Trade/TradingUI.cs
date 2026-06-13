using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class TradingUI : MonoBehaviour
{
    public static TradingUI Instance { get; private set; }

    [Header("UI References")]
    public GameObject tradingWindow;
    public Transform tradesContainer;
    public GameObject tradeSlotPrefab;

    private void Awake()
    {
        Instance = this;
        tradingWindow.SetActive(false);
    }

    private void Update()
    {
        if (tradingWindow.activeSelf && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            CloseTrade();
        }
    }

    public void OpenTrade(TradeDefinition tradeDef)
    {
        foreach (Transform child in tradesContainer)
        {
            Destroy(child.gameObject);
        }

        foreach (var offer in tradeDef.offers)
        {
            GameObject newSlot = Instantiate(tradeSlotPrefab, tradesContainer);
            newSlot.GetComponent<TradeSlotUI>().Setup(offer);
        }

        tradingWindow.SetActive(true);

        if (Mouse.current != null)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
    }

    public void CloseTrade()
    {
        tradingWindow.SetActive(false);
    }
}