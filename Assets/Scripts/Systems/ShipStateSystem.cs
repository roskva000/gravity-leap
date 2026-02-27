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
            float deltaTime = SystemAPI.Time.DeltaTime;
            var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);

            // Step 1: Count drones servicing each ship
            // Using a simple native map for counting (shipEntity -> droneCount)
            var droneCounts = new Unity.Collections.NativeParallelHashMap<Entity, int>(100, Unity.Collections.Allocator.Temp);
            foreach (var drone in SystemAPI.Query<RefRO<DroneData>>())
            {
                if (drone.ValueRO.IsBusy && drone.ValueRO.CurrentState == DroneState.Working && drone.ValueRO.CurrentTargetEntity != Entity.Null)
                {
                    if (droneCounts.TryGetValue(drone.ValueRO.CurrentTargetEntity, out int count))
                        droneCounts[drone.ValueRO.CurrentTargetEntity] = count + 1;
                    else
                        droneCounts.TryAdd(drone.ValueRO.CurrentTargetEntity, 1);
                }
            }

            foreach (var (shipData, rust, neon, transform, entity) in SystemAPI.Query<RefRW<ShipData>, RefRW<RustAmountOverride>, RefRW<NeonPowerOverride>, RefRW<LocalTransform>>().WithEntityAccess())
            {
                switch (shipData.ValueRO.CurrentState)
                {
                    case ShipState.Approaching:
                        float3 target = shipData.ValueRO.TargetDockPosition;
                        float dist = math.distance(transform.ValueRO.Position, target);
                        
                        if (dist > 0.1f)
                        {
                            float3 dir = math.normalize(target - transform.ValueRO.Position);
                            // Task C: Use MoveSpeed (Critical ships are slower)
                            transform.ValueRW.Position += dir * deltaTime * shipData.ValueRO.MoveSpeed;
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
                    case ShipState.Wreck:
                        // Task E & C: Dynamic Repair Speed based on drone count and requirements
                        int activeDrones = 0;
                        droneCounts.TryGetValue(entity, out activeDrones);
                        
                        // Gereksinim kontrolü (Wreck için 2, Normal/Kritik için 1)
                        if (activeDrones >= shipData.ValueRO.RequiredDroneCount)
                        {
                            float baseRepairRate = 0.2f;
                            // Wreck daha zor tamir edilsin? (Hız / 2?) - Not explicitly asked but 2 drones makes it 0.4 anyway.
                            shipData.ValueRW.RepairProgress += deltaTime * baseRepairRate * activeDrones;
                        }

                        // Task A: driving _RustAmount
                        rust.ValueRW.Value = math.saturate(1.0f - shipData.ValueRO.RepairProgress);
                        
                        // Neon power stabilization
                        neon.ValueRW.Value = math.lerp(neon.ValueRO.Value, 1.0f, deltaTime * 2f);

                        if (shipData.ValueRO.RepairProgress >= 1.0f)
                        {
                            shipData.ValueRW.CurrentState = ShipState.Taxes;
                            neon.ValueRW.Value = 10.0f; // Neon Pulse

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

            droneCounts.Dispose();
        }
    }
}
