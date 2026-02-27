using Unity.Entities;
using GalacticNexus.Scripts.Components;
using UnityEngine;

namespace GalacticNexus.Scripts.Authoring
{
    public class DroneAuthoring : MonoBehaviour
    {
        public float Speed = 5f;
        public float BatteryLevel = 1.0f;

        public class DroneBaker : Baker<DroneAuthoring>
        {
            public override void Bake(DroneAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new DroneTag());
                AddComponent(entity, new DroneData
                {
                    Speed = authoring.Speed,
                    BatteryLevel = authoring.BatteryLevel,
                    IsBusy = false
                });
            }
        }
    }
}
