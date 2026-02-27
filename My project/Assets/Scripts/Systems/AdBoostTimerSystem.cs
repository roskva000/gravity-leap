using Unity.Burst;
using Unity.Entities;
using GalacticNexus.Scripts.Components;
using Unity.Mathematics;

namespace GalacticNexus.Scripts.Systems
{
    [BurstCompile]
    public partial struct AdBoostTimerSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.TryGetSingletonRW<MonetizationData>(out var monData)) return;

            if (monData.ValueRO.AdBoostRemainingSeconds > 0)
            {
                monData.ValueRW.AdBoostRemainingSeconds = math.max(0, monData.ValueRO.AdBoostRemainingSeconds - SystemAPI.Time.DeltaTime);
                
                // Boost bittiyse çarpanı sıfırla
                if (monData.ValueRO.AdBoostRemainingSeconds <= 0)
                {
                    monData.ValueRW.LastAdMultiplier = 1.0f;
                }
            }
        }
    }
}
