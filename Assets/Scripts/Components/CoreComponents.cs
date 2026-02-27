using Unity.Entities;
using Unity.Mathematics;

namespace GalacticNexus.Scripts.Components
{
    public struct DroneTag : IComponentData { }

    public enum DroneState
    {
        Idle,
        Working,
        Charging
    }

    public struct DroneData : IComponentData
    {
        public float BatteryLevel;
        public DroneState CurrentState;
        public float Speed;
        public float3 TargetPosition;
        public Entity CurrentTargetEntity;
        public bool IsBusy;
    }

    public struct DockTag : IComponentData { }

    public struct DockData : IComponentData
    {
        public bool IsOccupied;
        public Entity OccupyingShip;
        public float ServiceMultiplier;
    }
}
