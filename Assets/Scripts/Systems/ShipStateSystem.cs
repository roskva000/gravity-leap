using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using GalacticNexus.Scripts.Components;
using GalacticNexus.Scripts.Juice;
using Unity.Mathematics;

namespace GalacticNexus.Scripts.Systems
{
    [BurstCompile]
    public partial struct ShipStateSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
            float deltaTime = SystemAPI.Time.DeltaTime;

            foreach (var (shipData, rust, neon, transform) in SystemAPI.Query<RefRW<ShipData>, RefRW<RustAmountOverride>, RefRW<NeonPowerOverride>, RefRW<LocalTransform>>())
            {
                switch (shipData.ValueRO.CurrentState)
                {
                    case ShipState.Approaching:
                        float3 target = shipData.ValueRO.TargetDockPosition;
                        float dist = math.distance(transform.ValueRO.Position, target);
                        
                        if (dist > 0.1f)
                        {
                            float3 dir = math.normalize(target - transform.ValueRO.Position);
                            transform.ValueRW.Position += dir * deltaTime * 5f;
                            transform.ValueRW.Rotation = math.slerp(transform.ValueRO.Rotation, 
                                quaternion.LookRotationSafe(dir, math.up()), deltaTime * 2f);
                        }
                        else
                        {
                            shipData.ValueRW.CurrentState = ShipState.Docked;
                            transform.ValueRW.Position = target;

                            // Juicing: Docked Event
                            var eventEntity = ecb.CreateEntity();
                            ecb.AddComponent(eventEntity, new GameEvent
                            {
                                Type = GameEventType.ShipDocked,
                                Position = target,
                                Value = 1.0f
                            });
                        }
                        break;

                    case ShipState.Servicing:
                        // Task A: Update RustAmount (0.8 -> 0.1) based on RepairProgress (0 -> 1)
                        rust.ValueRW.Value = math.lerp(0.8f, 0.1f, shipData.ValueRO.RepairProgress);
                        
                        // Neon power base (pulse back to 1.0 if it was boosted)
                        neon.ValueRW.Value = math.lerp(neon.ValueRO.Value, 1.0f, deltaTime * 2f);

                        if (shipData.ValueRO.RepairProgress >= 1.0f)
                        {
                            shipData.ValueRW.CurrentState = ShipState.Taxes;
                            
                            // Task A: Neon Pulse on completion
                            neon.ValueRW.Value = 10.0f; 
                            
                            // Juicing: Service Finished Event (Optional highlight)
                            var serviceDoneEntity = ecb.CreateEntity();
                            ecb.AddComponent(serviceDoneEntity, new GameEvent
                            {
                                Type = GameEventType.ServiceFinished,
                                Position = transform.ValueRO.Position,
                                Value = 1.0f
                            });
                        }
                        break;
                }
            }
        }
    }
}
