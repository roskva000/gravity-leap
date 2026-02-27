using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using GalacticNexus.Scripts.Components;

namespace GalacticNexus.Scripts.Systems
{
    [BurstCompile]
    public partial struct ShipStateSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Bu sistem gemilerin durum geçişlerini ve temel mantığını yönetecek.
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
