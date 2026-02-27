using Unity.Burst;
using Unity.Entities;
using GalacticNexus.Scripts.Components;
using GalacticNexus.Scripts.Juice;
using Unity.Mathematics;
using Unity.Transforms;

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
                    // Geliri hesaba ekle (Prestij Çarpanı Dahil)
                    double prestigeMultiplier = 1.0 + (economy.ValueRO.DarkMatter * 0.10);
                    double finalReward = reward.ValueRO.BaseReward * reward.ValueRO.FractionMultiplier * prestigeMultiplier;
                    
                    economy.ValueRW.ScrapCurrency += finalReward;
                    economy.ValueRW.TotalShipsServiced++;

                    // VFX Olayı Fırlat (Juice)
                    var eventEntity = ecb.CreateEntity();
                    ecb.AddComponent(eventEntity, new GameEvent
                    {
                        Type = GameEventType.ScrapEarned,
                        Position = SystemAPI.GetComponent<LocalTransform>(entity).Position,
                        Value = (float)finalReward
                    });

                    // Gemiyi ayrılma durumuna geçir
                    ship.ValueRW.CurrentState = ShipState.Departing;

                    // Dock'u boşalt
                    if (SystemAPI.HasComponent<DockData>(ship.ValueRO.AssignedDockEntity))
                    {
                        var dockData = SystemAPI.GetComponentRW<DockData>(ship.ValueRO.AssignedDockEntity);
                        dockData.ValueRW.IsOccupied = false;
                    }
                    
                    // Not: Yok etme işlemini artık ShipDespawnSystem yapacak
                }
            }
        }
    }
}
