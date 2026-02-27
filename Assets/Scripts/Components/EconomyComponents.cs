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
        public float NexusProgress;
        public bool NexusComplete;
        public int TutorialStep;
    }

    public struct GlobalMarketData : IComponentData
    {
        public float SindicatoMultiplier;
        public float TheCoreMultiplier;
        public float VoidWalkersMultiplier;
        public bool IsRaidActive;
        public float RaidTimer;
    }

    public struct RewardData : IComponentData
    {
        public float BaseReward;
        public float FractionMultiplier;
    }
}
