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
            foreach (var (shipData, transform) in SystemAPI.Query<RefRW<ShipData>, RefRW<LocalTransform>>())
            {
                switch (shipData.ValueRO.CurrentState)
                {
                    case ShipState.Approaching:
                        // İniş mantığı buraya gelecek
                        break;
                    case ShipState.Servicing:
                        // Tamir ve yakıt mantığı buraya gelecek
                        break;
                }
            }
        }
    }
}
