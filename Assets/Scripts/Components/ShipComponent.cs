using Unity.Entities;
using Unity.Mathematics;

namespace GalacticNexus.Scripts.Components
{
    public struct ShipTag : IComponentData { }

    public struct ShipData : IComponentData
    {
        public float Health;
        public float Fuel;
        public float CargoCapacity;
        public float RepairProgress;
        public ShipState CurrentState;
        public Fraction OwnerFraction;
    }

    public enum ShipState
    {
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
