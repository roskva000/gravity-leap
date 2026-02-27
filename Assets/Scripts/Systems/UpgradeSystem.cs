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
                    ApplyUpgrade(request.Type, upgrade);
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
                _ => 200f
            };
        }

        private void ApplyUpgrade(UpgradeType type, RefRW<UpgradeData> upgrade)
        {
            switch (type)
            {
                case UpgradeType.DroneSpeed: upgrade.ValueRW.DroneSpeedLevel++; break;
                case UpgradeType.DockCapacity: upgrade.ValueRW.DockLevel++; break;
            }
        }
    }
}
