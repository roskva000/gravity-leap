using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using GalacticNexus.Scripts.Components;

namespace GalacticNexus.Scripts.Systems
{
    [BurstCompile]
    public partial struct ShipDespawnSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float deltaTime = SystemAPI.Time.DeltaTime;
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            foreach (var (transform, ship, entity) in SystemAPI.Query<RefRW<LocalTransform>, RefRO<ShipData>>().WithEntityAccess())
            {
                if (ship.ValueRO.CurrentState == ShipState.Departing)
                {
                    // İstasyonun uzağına (örn: yukarı ve ileri) doğru hareket et
                    float3 moveDir = new float3(0, 1, 1);
                    transform.ValueRW.Position += moveDir * deltaTime * 15f; // Ayrılma hızı: 15f
                    
                    // Belirli bir mesafeye ulaşınca (İstasyondan 60 birim uzaklaşınca) yok et
                    if (math.length(transform.ValueRO.Position) > 60f)
                    {
                        ecb.DestroyEntity(entity);
                    }
                }
            }
        }
    }
}
