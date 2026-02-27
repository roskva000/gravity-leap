using Unity.Entities;
using GalacticNexus.Scripts.Components;
using UnityEngine;

namespace GalacticNexus.Scripts.Authoring
{
    public class ShipAuthoring : MonoBehaviour
    {
        public float Health = 100f;
        public float Fuel = 1.0f;
        public float CargoCapacity = 500f;
        public Fraction OwnerFraction = Fraction.Sindicato;

        public class ShipBaker : Baker<ShipAuthoring>
        {
            public override void Bake(ShipAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new ShipTag());
                AddComponent(entity, new ShipData
                {
                    Health = authoring.Health,
                    Fuel = authoring.Fuel,
                    CargoCapacity = authoring.CargoCapacity,
                    CurrentState = ShipState.Approaching,
                    OwnerFraction = authoring.OwnerFraction,
                    RepairProgress = 0f
                });
            }
        }
    }
}
