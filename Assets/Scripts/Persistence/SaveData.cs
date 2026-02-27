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

        // Monetization
        public bool IsNoAdsPurchased;

        // Offline timing
        public long LastSaveTimestamp;
    }
}
