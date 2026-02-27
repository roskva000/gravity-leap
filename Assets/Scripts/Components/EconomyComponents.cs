using Unity.Entities;

namespace GalacticNexus.Scripts.Components
{
    public struct EconomyData : IComponentData
    {
        public double ScrapCurrency;
        public int TotalShipsServiced;
        public long LastSaveTimestamp;
        public double DarkMatter;
        public int PrestigeCount;
    }

    public struct RewardData : IComponentData
    {
        public float BaseReward;
        public float FractionMultiplier;
    }
}
