using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using GalacticNexus.Scripts.Components;
using Unity.Transforms;
using GalacticNexus.Scripts.Juice;

namespace GalacticNexus.Scripts.Systems
{
    [BurstCompile]
    public partial struct NexusCoreSystem : ISystem
    {
        private float nextMegastructureThreshold;

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.TryGetSingletonRW<EconomyData>(out var economy)) return;

            // Simple investment logic: if scrap > 1M, invest 1M into 1% progress
            // In a real scenario, this would be a UI button click trigger.
            // For now, we'll implement the progression side.
            
            if (economy.ValueRO.NexusProgress < 1.0f)
            {
                // Visual threshold check
                if (economy.ValueRO.NexusProgress >= nextMegastructureThreshold)
                {
                    SpawnMegastructurePart(ref state);
                    nextMegastructureThreshold += 0.1f;
                }

                if (economy.ValueRO.NexusProgress >= 1.0f && !economy.ValueRO.NexusComplete)
                {
                    economy.ValueRW.NexusComplete = true;
                    TriggerNexusCompletion(ref state);
                }
            }
        }

        private void SpawnMegastructurePart(ref SystemState state)
        {
            if (SystemAPI.TryGetSingletonRW<UpgradeData>(out var upgrade))
            {
                upgrade.ValueRW.NexusBuffSpeed += 0.05f;
                upgrade.ValueRW.NexusBuffBattery += 0.05f;

                // Signal drones to flash
                var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
                var eventEntity = ecb.CreateEntity();
                ecb.AddComponent(eventEntity, new GameEvent
                {
                    Type = Juice.GameEventType.Warning, // Global indicator
                    Position = float3.zero,
                    Value = 800f // Magic number for NEXUS BUFF
                });
            }
        }

        private void TriggerNexusCompletion(ref SystemState state)
        {
            var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
            var eventEntity = ecb.CreateEntity();
            ecb.AddComponent(eventEntity, new GameEvent
            {
                Type = Juice.GameEventType.Warning, // Reuse warning for global alerts
                Position = float3.zero,
                Value = 777f // Magic number for NEXUS COMPLETED
            });
        }
    }
}
