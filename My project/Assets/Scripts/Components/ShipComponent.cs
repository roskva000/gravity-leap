using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;

namespace GalacticNexus.Scripts.Components
{
    public struct ShipTag : IComponentData { }
    
    public struct TutorialArrow : IComponentData
    {
        public Entity TargetEntity;
    }

    [MaterialProperty("_RustAmount")]
    public struct RustAmountOverride : IComponentData
    {
        public float Value;
    }

    [MaterialProperty("_NeonPower")]
    public struct NeonPowerOverride : IComponentData
    {
        public float Value;
    }

    [MaterialProperty("_NeonColor")]
    public struct NeonColorOverride : IComponentData
    {
        public float4 Value;
    }

    [MaterialProperty("_PulseSpeed")]
    public struct PulseSpeedOverride : IComponentData
    {
        public float Value;
    }

    [MaterialProperty("_GlitchIntensity")]
    public struct HologramGlitchOverride : IComponentData
    {
        public float Value;
    }

    [MaterialProperty("_RGBSplit")]
    public struct HologramSplitOverride : IComponentData
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
        public ShipCondition Condition;
        public float HullIntegrity;
        public float MoveSpeed;
        public int RequiredDroneCount;
        
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
        Departing,
        Wreck
    }

    public enum ShipCondition
    {
        Normal,
        Critical,
        Legendary
    }

    public enum Fraction
    {
        Sindicato,
        TheCore,
        VoidWalkers
    }
}
