using Unity.Entities;
using Unity.Mathematics;

namespace GalacticNexus.Scripts.Components
{
    public struct ShipTag : IComponentData { }

    [UnityEngine.Rendering.MaterialProperty("_RustAmount")]
    public struct RustAmountOverride : IComponentData
    {
        public float Value;
    }

    [UnityEngine.Rendering.MaterialProperty("_NeonPower")]
    public struct NeonPowerOverride : IComponentData
    {
        public float Value;
    }

    public struct ShipData : IComponentData
    {
        public float Health;
        public float Fuel;
        public float CargoCapacity;
        public ShipState CurrentState;
        public Fraction OwnerFraction;
        public float RepairProgress;
        
        // Yanaşılacak dock'un koordinatları ve referansı
        public float3 TargetDockPosition;
        public Entity AssignedDockEntity;
    }

    public enum ShipState
    {
        Waiting,
        Approaching,
        Docked,
        Servicing,
        Taxes,
        Departing
    }

    public enum Fraction
    {
        Sindicato,
        TheCore,
        VoidWalkers
    }
}
