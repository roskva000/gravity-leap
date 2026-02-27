using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using GalacticNexus.Scripts.Components;

namespace GalacticNexus.Scripts.Systems
{
    [BurstCompile]
    public partial struct DroneUtilityAISystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            // Boştaki drone'ları bul
            foreach (var (droneData, droneEntity, droneTransform) in SystemAPI.Query<RefRW<DroneData>, Entity, RefRO<LocalTransform>>().WithAll<DroneTag>())
            {
                if (droneData.ValueRO.IsBusy) continue;

                Entity bestTarget = Entity.Null;
                float bestScore = -1f;

                // Hizmet bekleyen gemileri tara
                foreach (var (shipData, shipEntity, shipTransform) in SystemAPI.Query<RefRO<ShipData>, Entity, RefRO<LocalTransform>>().WithAll<ShipTag>())
                {
                    if (shipData.ValueRO.CurrentState != ShipState.Docked) continue;

                    float score = CalculateUtilityScore(droneTransform.ValueRO.Position, shipTransform.ValueRO.Position, shipData.ValueRO);
                    
                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestTarget = shipEntity;
                    }
                }

                if (bestTarget != Entity.Null && bestScore > 0.5f) // Eşiği geçen en iyi hedefi ata
                {
                    droneData.ValueRW.CurrentTargetEntity = bestTarget;
                    droneData.ValueRW.IsBusy = true;
                    // Hedef geminin koordinatını al (basitleştirilmiş)
                    droneData.ValueRW.TargetPosition = SystemAPI.GetComponent<LocalTransform>(bestTarget).Position;
                }
            }
        }

        [BurstCompile]
        private float CalculateUtilityScore(float3 dronePos, float3 shipPos, ShipData ship)
        {
            float distance = math.distance(dronePos, shipPos);
            float distanceFactor = math.clamp(1.0f - (distance / 100f), 0, 1);
            
            // Fraksiyonel öncelik (Lore bazlı)
            float fractionPriority = ship.OwnerFraction == Fraction.VoidWalkers ? 1.2f : 1.0f;
            
            // Aciliyet (Yakıt azsa veya tamir çok lazımsa)
            float urgency = (1.0f - ship.Fuel) + (1.0f - ship.RepairProgress);
            
            return (distanceFactor * 0.4f) + (urgency * 0.4f) * fractionPriority;
        }
    }
}
