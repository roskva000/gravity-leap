using Unity.Burst;
using Unity.Entities;
using GalacticNexus.Scripts.Components;

namespace GalacticNexus.Scripts.Systems
{
    [BurstCompile]
    public partial struct UpgradeSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.TryGetSingletonRW<EconomyData>(out var economy)) return;
            if (!SystemAPI.TryGetSingletonRW<UpgradeData>(out var upgrade)) return;

            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            // Gelen yükseltme isteklerini işle
            foreach (var (request, entity) in SystemAPI.Query<UpgradeRequest, Entity>())
            {
                double cost = math.floor(CalculateCost(request.Type, upgrade.ValueRO));

                if (economy.ValueRO.ScrapCurrency >= cost)
                {
                    economy.ValueRW.ScrapCurrency -= cost;
                    ApplyUpgrade(request.Type, upgrade, ecb);
                }

                // İsteği sil (Consume request)
                ecb.RemoveComponent<UpgradeRequest>(entity);
            }
        }

        private float CalculateCost(UpgradeType type, UpgradeData current)
        {
            return type switch
            {
                UpgradeType.DroneSpeed => 100f * math.pow(1.15f, current.DroneSpeedLevel),
                UpgradeType.DockCapacity => 500f * math.pow(1.5f, current.DockLevel),
                UpgradeType.SolarCollector => 300f * math.pow(1.3f, current.SolarCollectorLevel),
                _ => 200f
            };
        }

        private void ApplyUpgrade(UpgradeType type, RefRW<UpgradeData> upgrade, EntityCommandBuffer ecb)
        {
            switch (type)
            {
                case UpgradeType.DroneSpeed: 
                    upgrade.ValueRW.DroneSpeedLevel++; 
                    break;
                case UpgradeType.DockCapacity: 
                    upgrade.ValueRW.DockLevel++;
                    
                    // Görev D: Instantiate new dock entity
                    if (upgrade.ValueRO.DockPrefab != Entity.Null)
                    {
                        var newDock = ecb.Instantiate(upgrade.ValueRO.DockPrefab);
                        
                        // Her yeni dock için X ekseninde +10 birim ofset
                        // İlk dock 0'dadır (Bake ile gelen), 2. level dock 10'da olmalı
                        float3 newPos = new float3((upgrade.ValueRO.DockLevel - 1) * 10f, 0, 0);
                        
                        ecb.SetComponent(newDock, Unity.Transforms.LocalTransform.FromPosition(newPos));
                        
                        // Dock verisini sıfırla (IsOccupied = false varsayılan gelir ama netleşsin)
                        ecb.SetComponent(newDock, new DockData 
                        { 
                            IsOccupied = false, 
                            ServiceMultiplier = 1.0f 
                        });
                    }
                    break;
                case UpgradeType.SolarCollector:
                    upgrade.ValueRW.SolarCollectorLevel++;
                    break;
            }
        }
    }
}
