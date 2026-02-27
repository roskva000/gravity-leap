using Unity.Burst;
using Unity.Entities;
using GalacticNexus.Scripts.Components;

namespace GalacticNexus.Scripts.Systems
{
    [BurstCompile]
    public partial struct EconomySystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Tekil ekonomi verisini (Singleton) bul
            if (!SystemAPI.TryGetSingletonRW<EconomyData>(out var economy)) return;

            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            // Servisi bitmiş ve ödeme bekleyen gemileri tara (Taxes statesi)
            foreach (var (ship, reward, entity) in SystemAPI.Query<RefRW<ShipData>, RefRO<RewardData>, Entity>().WithAll<ShipTag>())
            {
                if (ship.ValueRO.CurrentState == ShipState.Taxes)
                {
                    // Geliri hesaba ekle
                    double finalReward = reward.ValueRO.BaseReward * reward.ValueRO.FractionMultiplier;
                    economy.ValueRW.ScrapCurrency += finalReward;
                    economy.ValueRW.TotalShipsServiced++;

                    // Gemiyi kalkışa gönder
                    ship.ValueRW.CurrentState = ShipState.Departing;
                }
            }
        }
    }
}
