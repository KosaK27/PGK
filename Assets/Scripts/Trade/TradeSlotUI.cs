using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TradeSlotUI : MonoBehaviour
{
    [Header("Cost UI")]
    public Image costIcon;
    public TextMeshProUGUI costAmountText;

    [Header("Reward UI")]
    public Image rewardIcon;
    public TextMeshProUGUI rewardAmountText;

    [Header("Buttons")]
    public Button tradeButton;

    private TradeOffer _offer;

    public void Setup(TradeOffer offer)
    {
        _offer = offer;

        costIcon.sprite = offer.costItem.sprite;
        costAmountText.text = offer.costAmount.ToString();

        rewardIcon.sprite = offer.rewardItem.sprite;
        rewardAmountText.text = offer.rewardAmount.ToString();

        tradeButton.onClick.RemoveAllListeners();
        tradeButton.onClick.AddListener(AttemptTrade);
    }

    private void AttemptTrade()
    {
        if (InventorySystem.Instance.HasItemAmount(_offer.costItem, _offer.costAmount))
        {
            InventorySystem.Instance.ConsumeItem(_offer.costItem, _offer.costAmount);
            InventorySystem.Instance.AddItem(new ItemStack(_offer.rewardItem, _offer.rewardAmount));

            Debug.Log($"Zakupiono: {_offer.rewardAmount}x {_offer.rewardItem.name}");
        }
        else
        {
            Debug.LogWarning("Brak wystarczaj¹cych œrodków!");
        }
    }
}