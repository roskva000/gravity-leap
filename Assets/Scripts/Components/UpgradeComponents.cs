using Unity.Entities;

namespace GalacticNexus.Scripts.Components
{
    public struct UpgradeData : IComponentData
    {
        public int DockLevel;
        public int DroneSpeedLevel;
        public int DroneBatteryLevel;
        public int SolarCollectorLevel;

        public float GetDroneSpeedBonus() => DroneSpeedLevel * 0.5f;
        public float GetBatteryEfficiency() => DroneBatteryLevel * 0.1f;
    }

    public struct UpgradeRequest : IComponentData
    {
        public UpgradeType Type;
    }

    public enum UpgradeType
    {
        DockCapacity,
        DroneSpeed,
        DroneBattery,
        SolarCollector
    }
}
