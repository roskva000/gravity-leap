using Unity.Entities;

namespace GalacticNexus.Scripts.Components
{
    public struct UpgradeData : IComponentData
    {
        public int DockLevel;
        public int DroneSpeedLevel;
        public int DroneBatteryLevel;
        public int SolarCollectorLevel;
        public int ShieldLevel;
        public Entity DockPrefab;

        public float GetDroneSpeedBonus() => DroneSpeedLevel * 0.5f;
        public float GetBatteryEfficiency() => DroneBatteryLevel * 0.1f;
    }

    public struct ShieldData : IComponentData
    {
        public float Integrity;
        public float MaxIntegrity;
        public bool IsActive;
    }

    public struct UpgradeRequest : IComponentData
    {
        public UpgradeType Type;
    }

    public struct BlackMarketModule : IComponentData
    {
        public float Timer;
        public bool IsActive;
        public bool PenaltyActive;
    }

    public enum UpgradeType
    {
        DockCapacity,
        DroneSpeed,
        DroneBattery,
        SolarCollector,
        Shield
    }
}
