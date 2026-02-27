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

            foreach (var (shipData, transform) in SystemAPI.Query<RefRW<ShipData>, RefRW<LocalTransform>>())
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
                        if (shipData.ValueRO.RepairProgress >= 1.0f)
                        {
                            shipData.ValueRW.CurrentState = ShipState.Taxes;
                        }
                        break;
                }
            }
        }
    }
}
