using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public struct TradeOffer
{
    [Header("Cena (Co gracz p³aci)")]
    public ItemDefinition costItem;
    public int costAmount;

    [Header("Nagroda (Co gracz dostaje)")]
    public ItemDefinition rewardItem;
    public int rewardAmount;
}

[CreateAssetMenu(fileName = "NewTradeDef", menuName = "World/Trade Definition")]
public class TradeDefinition : ScriptableObject
{
    public List<TradeOffer> offers = new List<TradeOffer>();
}