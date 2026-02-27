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

        // Monetization
        public bool IsNoAdsPurchased;
        public float AdBoostRemainingSeconds;

        // Offline timing
        public long LastSaveTimestamp;
    }
}
