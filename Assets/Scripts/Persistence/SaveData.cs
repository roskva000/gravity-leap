using System;

namespace GalacticNexus.Scripts.Persistence
{
    [Serializable]
    public class GameSaveData
    {
        public double ScrapCurrency;
        public int TotalShipsServiced;
        
        // Upgrade Levels
        public int DockLevel;
        public int DroneSpeedLevel;
        public int DroneBatteryLevel;

        // Prestige
        public double DarkMatter;
        public int PrestigeCount;

        // Progress & Tutorial
        public int TutorialStep;
        public float NexusProgress;
        public bool NexusComplete;

        // Combat & Marketplace
        public float ShieldIntegrity;
        public float SindicatoMultiplier;
        public float TheCoreMultiplier;
        public float VoidWalkersMultiplier;

        // Monetization
        public bool IsNoAdsPurchased;
        public float AdBoostRemainingSeconds;

        // Offline timing
        public long LastSaveTimestamp;
    }
}
