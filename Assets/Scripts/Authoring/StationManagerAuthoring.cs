using Unity.Entities;
using GalacticNexus.Scripts.Components;
using UnityEngine;

namespace GalacticNexus.Scripts.Authoring
{
    public class StationManagerAuthoring : MonoBehaviour
    {
        public GameObject DockPrefab;

        public class StationManagerBaker : Baker<StationManagerAuthoring>
        {
            public override void Bake(StationManagerAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                
                AddComponent(entity, new UpgradeData
                {
                    DockLevel = 1,
                    DroneSpeedLevel = 1,
                    DroneBatteryLevel = 1,
                    SolarCollectorLevel = 1,
                    DockPrefab = GetEntity(authoring.DockPrefab, TransformUsageFlags.Dynamic)
                });

                AddComponent(entity, new MonetizationData
                {
                    IsNoAdsPurchased = false,
                    RewardedGems = 0,
                    LastAdMultiplier = 1.0f
                });
            }
        }
    }
}
