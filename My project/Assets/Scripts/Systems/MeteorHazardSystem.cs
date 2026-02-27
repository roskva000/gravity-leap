using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;
using GalacticNexus.Scripts.Components;
using GalacticNexus.Scripts.Juice;

namespace GalacticNexus.Scripts.Systems
{
    [BurstCompile]
    public partial struct MeteorHazardSystem : ISystem
    {
        private float timer;

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.TryGetSingleton<EconomyData>(out var economy)) return;
            if (economy.TutorialStep < 3) return;

            float deltaTime = SystemAPI.Time.DeltaTime;
            timer += deltaTime;

            // Her 30 saniyede bir %5 ihtimalle meteor yağmuru
            if (timer >= 30f)
            {
                timer = 0f;
                
                var rand = new Unity.Mathematics.Random((uint)(SystemAPI.Time.ElapsedTime * 1000) + 1);
                if (rand.NextFloat(0, 1) < 0.05f)
                {
                    StartMeteorShower(ref state);
                }
            }
        }

        private void StartMeteorShower(ref SystemState state)
        {
            var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
            
            // Dock'taki gemileri bul (Docked veya Servicing durumundaki gemiler)
            foreach (var (ship, transform, entity) in SystemAPI.Query<RefRW<ShipData>, LocalTransform>().WithEntityAccess())
            {
                if (ship.ValueRO.CurrentState == ShipState.Docked || 
                    ship.ValueRO.CurrentState == ShipState.Servicing ||
                    ship.ValueRO.CurrentState == ShipState.Wreck)
                {
                    // Meteor çarpma ihtimali (her gemi için %20)
                    var rand = new Unity.Mathematics.Random((uint)(entity.Index + SystemAPI.Time.ElapsedTime * 100));
                    if (rand.NextFloat(0, 1) < 0.2f)
                    {
                        // Hasar ver
                        ship.ValueRW.HullIntegrity = math.max(0, ship.ValueRO.HullIntegrity - 0.2f);
                        
                        // Hazard Görseli (Warning Event)
                        var hitEvent = ecb.CreateEntity();
                        ecb.AddComponent(hitEvent, new GameEvent
                        {
                            Type = GameEventType.Warning,
                            Position = transform.Position,
                            Value = 1.0f // 1 for meteor hit
                        });

                        // %0'a düşerse WRECK
                        if (ship.ValueRO.HullIntegrity <= 0.01f && ship.ValueRO.CurrentState != ShipState.Wreck)
                        {
                            ship.ValueRW.CurrentState = ShipState.Wreck;
                            ship.ValueRW.RequiredDroneCount = 2; // Görev E: 2 drone gereksinimi
                            ship.ValueRW.RepairProgress = 0f; // Reset progress
                        }
                    }
                }
            }
        }
    }
}
