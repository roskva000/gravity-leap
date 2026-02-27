using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using GalacticNexus.Scripts.Components;

namespace GalacticNexus.Scripts.Systems
{
    [BurstCompile]
    public partial struct TutorialArrowSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float currentTime = (float)SystemAPI.Time.ElapsedTime;
            var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);

            foreach (var (arrow, transform, entity) in SystemAPI.Query<RefRO<TutorialArrow>, RefRW<LocalTransform>>().WithEntityAccess())
            {
                if (SystemAPI.HasComponent<LocalTransform>(arrow.ValueRO.TargetEntity))
                {
                    var targetTransform = SystemAPI.GetComponent<LocalTransform>(arrow.ValueRO.TargetEntity);
                    
                    // Hover over target
                    float3 offset = new float3(0, 5f + math.sin(currentTime * 5f) * 1f, 0);
                    transform.ValueRW.Position = targetTransform.Position + offset;
                    
                    // Pulse Scale
                    transform.ValueRW.Scale = 1.0f + math.sin(currentTime * 10f) * 0.2f;

                    // Sync with NeonColor if we had a component for it on the arrow
                    if (SystemAPI.HasComponent<NeonColorOverride>(entity))
                    {
                        var color = SystemAPI.GetComponentRW<NeonColorOverride>(entity);
                        color.ValueRW.Value.w = 0.5f + math.sin(currentTime * 5f) * 0.5f; // Alpha pulse
                    }
                }
                else
                {
                    // If target destroyed (unlikely during tutorial but safe)
                    ecb.DestroyEntity(entity);
                }
            }
        }
    }
}
