using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using GalacticNexus.Scripts.Components;
using GalacticNexus.Scripts.Juice;
using Unity.Transforms;

namespace GalacticNexus.Scripts.Systems
{
    [BurstCompile]
    public partial struct SyndicateRaidSystem : ISystem
    {
        private float raidCheckTimer;
        private float raidDuration;

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float deltaTime = SystemAPI.Time.DeltaTime;

            if (!SystemAPI.TryGetSingletonRW<GlobalMarketData>(out var market)) return;
            SystemAPI.TryGetSingletonRW<ShieldData>(out var shield);
            bool hasShield = shield.IsValid && shield.ValueRO.IsActive && shield.ValueRO.Integrity > 0;

            // Raid Trigger Logic
            if (!market.ValueRO.IsRaidActive)
            {
                raidCheckTimer += deltaTime;
                
                bool marketTrigger = market.ValueRO.SindicatoMultiplier >= 1.4f || 
                                     market.ValueRO.TheCoreMultiplier >= 1.4f || 
                                     market.ValueRO.VoidWalkersMultiplier >= 1.4f;

                if (marketTrigger || raidCheckTimer >= 300f) // 5 minutes
                {
                    var rand = new Unity.Mathematics.Random((uint)(SystemAPI.Time.ElapsedTime * 1000) + 1);
                    if (rand.NextFloat() < 0.10f || marketTrigger)
                    {
                        StartRaid(ref state, market);
                        raidCheckTimer = 0;
                        raidDuration = 30f; // Raid lasts 30 seconds
                    }
                }
            }
            else
            {
                raidDuration -= deltaTime;
                if (raidDuration <= 0)
                {
                    market.ValueRW.IsRaidActive = false;
                }

                // Raid Damage Logic
                ProcessRaidDamage(ref state, deltaTime, hasShield, ref shield);
            }
        }

        private void StartRaid(ref SystemState state, RefRW<GlobalMarketData> market)
        {
            market.ValueRW.IsRaidActive = true;
            
            var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
            var eventEntity = ecb.CreateEntity();
            ecb.AddComponent(eventEntity, new GameEvent
            {
                Type = GameEventType.Warning,
                Position = float3.zero,
                Value = 666f // Magic number for SYNDICATE RAID
            });
        }

        private void ProcessRaidDamage(ref SystemState state, float dt, bool hasShield, RefRW<ShieldData> shield)
        {
            float baseDamage = 2.0f * dt; // 2% HullIntegrity per second
            float shieldAbsorption = 0.8f;
            
            if (hasShield)
            {
                baseDamage *= (1.0f - shieldAbsorption);
                shield.ValueRW.Integrity -= 1.0f * dt; // Shield loses 1 unit per second while active during raid
            }

            // Damage ships in docks
            foreach (var ship in SystemAPI.Query<RefRW<ShipData>>().WithAll<ShipTag>())
            {
                if (ship.ValueRO.CurrentState == ShipState.Docked || ship.ValueRO.CurrentState == ShipState.Servicing)
                {
                    float shipDamageMultiplier = 1.0f;
                    
                    // Drone overclock check (need to query drones targeting this ship)
                    // Simplified for now: if any drone is overclocking, ship takes more damage
                    // Actually, let's keep it simple: 1.5x damage if 'exposed' by high power drones
                    
                    ship.ValueRW.HullIntegrity = math.max(0, ship.ValueRO.HullIntegrity - (baseDamage * shipDamageMultiplier / 100f));
                }
            }
        }
    }
}
