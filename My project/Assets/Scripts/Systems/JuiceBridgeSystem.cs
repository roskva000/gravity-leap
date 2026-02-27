using Unity.Burst;
using Unity.Entities;
using GalacticNexus.Scripts.Components;
using GalacticNexus.Scripts.Juice;
using Unity.Transforms;

namespace GalacticNexus.Scripts.Systems
{
    [BurstCompile]
    public partial struct VFXBridgeSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            // Örnek: Gemi her Taxes durumuna geçtiğinde olay tetikle
            foreach (var (ship, transform, entity) in SystemAPI.Query<RefRO<ShipData>, RefRO<LocalTransform>>().WithEntityAccess())
            {
                if (ship.ValueRO.CurrentState == ShipState.Taxes)
                {
                    // Her update'de tetiklememek için şimdilik placeholder (Gerçekte state-change trigger gerekir)
                }
            }
        }
    }
}
