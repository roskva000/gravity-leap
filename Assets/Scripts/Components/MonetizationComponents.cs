using Unity.Entities;

namespace GalacticNexus.Scripts.Components
{
    public struct MonetizationData : IComponentData
    {
        public bool IsNoAdsPurchased;
        public int RewardedGems;
        public float LastAdMultiplier;
    }
}
