using UnityEngine;

[CreateAssetMenu(menuName = "Quiz/Economy Config", fileName = "EconomyConfig")]
public class EconomyConfig : ScriptableObject
{
    [Header("One-time bonuses")]
    public int signupBonusCoins = 100;

    [Header("Costs")]
    public int levelEntryCost = 5;

    [Header("Ad Rewards")]
    public int adCoinsReward = 20;
    public int adRetryCooldownSeconds = 0; // 0 = unlimited

    [Header("Shop")]
    public int packSmallCoins  = 100;
    public int packMediumCoins = 300;
    public int packLargeCoins  = 1000;
}
