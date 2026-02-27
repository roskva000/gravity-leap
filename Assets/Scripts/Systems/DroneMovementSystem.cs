using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using GalacticNexus.Scripts.Components;

namespace GalacticNexus.Scripts.Systems
{
    [BurstCompile]
    public partial struct DroneMovementSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float deltaTime = SystemAPI.Time.DeltaTime;
            if (!SystemAPI.TryGetSingleton<UpgradeData>(out var upgrade)) return;

            float batteryEfficiency = 1.0f - upgrade.GetBatteryEfficiency(); // Upgrade verimliliği

            var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);

            // Task O & N: Shield Maintenance
            if (SystemAPI.TryGetSingletonRW<ShieldData>(out var shield))
            {
                // Passive Regeneration from Solar Collectors
                float passiveRegen = upgrade.SolarCollectorLevel * 0.1f * deltaTime;
                shield.ValueRW.Integrity = math.min(shield.ValueRO.MaxIntegrity, shield.ValueRO.Integrity + passiveRegen);

                // Active Repair by Drones at center (0,0,0)
                foreach (var (droneTransform, droneData) in SystemAPI.Query<LocalTransform, RefRW<DroneData>>().WithAll<DroneTag>())
                {
                    if (droneData.ValueRO.CurrentState == DroneState.Working && 
                        math.distance(droneTransform.Position, float3.zero) < 1.0f)
                    {
                        float repairPower = droneData.ValueRO.IsOverclocked ? 5.0f : 2.0f;
                        shield.ValueRW.Integrity = math.min(shield.ValueRO.MaxIntegrity, shield.ValueRO.Integrity + repairPower * deltaTime);
                        
                        // Work consumes battery
                        droneData.ValueRW.BatteryLevel -= deltaTime * 0.05f * batteryEfficiency;
                    }
                }
            }

            foreach (var (transform, droneData) in SystemAPI.Query<RefRW<LocalTransform>, RefRW<DroneData>>().WithAll<DroneTag>())
            {
                if (droneData.ValueRO.IsMalfunctioning) continue;

                float consumptionMultiplier = 1.0f;
                if (droneData.ValueRO.IsOverclocked)
                {
                    // Task L: Overclock Efficiency Scaling
                    consumptionMultiplier = math.max(1.5f, 4.0f - upgrade.DroneBatteryLevel * 0.25f); 
                    
                    var rand = new Unity.Mathematics.Random((uint)(SystemAPI.Time.ElapsedTime * 1000) + 1);
                    if (rand.NextFloat() < 0.05f * deltaTime)
                    {
                        droneData.ValueRW.IsMalfunctioning = true;
                        
                        var arızaEvent = ecb.CreateEntity();
                        ecb.AddComponent(arızaEvent, new GameEvent
                        {
                            Type = GameEventType.Warning,
                            Position = transform.ValueRO.Position,
                            Value = 2.0f 
                        });
                        continue;
                    }
                }

                // Durum Yönetimi (FSM)
                if (droneData.ValueRO.CurrentState == DroneState.Charging)
                {
                    // Görev F: Solar Collector şarj olma hızı (0.2f * SolarCollectorLevel)
                    float chargeRate = 0.2f * math.max(1, upgrade.SolarCollectorLevel);
                    droneData.ValueRW.BatteryLevel = math.min(1.0f, droneData.ValueRO.BatteryLevel + deltaTime * chargeRate);
                    
                    // Şarj bittiğinde bayrağı sıfırla ve READY fırlat
                    if (droneData.ValueRO.BatteryLevel >= 1.0f)
                    {
                        droneData.ValueRW.CurrentState = DroneState.Idle;
                        droneData.ValueRW.WasBatteryWarningSent = false;

                        // READY Juice Event
                        var readyEvent = ecb.CreateEntity();
                        ecb.AddComponent(readyEvent, new GameEvent
                        {
                            Type = GameEventType.DroneBoost, // Use DroneBoost for "READY" visual
                            Position = transform.ValueRO.Position,
                            Value = 1.0f // Indicator for Ready
                        });
                    }
                    
                    // Şarj noktasına git (Merkez)
                    MoveToTarget(ref transform.ValueRW, new float3(0, 0, 0), droneData.ValueRO.Speed, deltaTime);
                    continue;
                }

                if (!droneData.ValueRO.IsBusy)
                {
                    // Boşta ama şarjı azsa şarja git
                    if (droneData.ValueRO.BatteryLevel < 0.2f)
                    {
                        droneData.ValueRW.CurrentState = DroneState.Charging;
                    }
                    continue;
                }

                // Working Durumu - Şarj Tüketimi
                droneData.ValueRW.BatteryLevel -= deltaTime * 0.1f * batteryEfficiency * consumptionMultiplier;

                // Kritik Şarj Kontrolü (Tek seferlik uyarı)
                if (droneData.ValueRO.BatteryLevel < 0.2f)
                {
                    if (!droneData.ValueRO.WasBatteryWarningSent)
                    {
                        droneData.ValueRW.WasBatteryWarningSent = true;
                        
                        var eventEntity = ecb.CreateEntity();
                        ecb.AddComponent(eventEntity, new GameEvent
                        {
                            Type = GameEventType.Warning,
                            Position = transform.ValueRO.Position,
                            Value = 0f // 0 for battery alert
                        });
                    }

                    droneData.ValueRW.IsBusy = false;
                    droneData.ValueRW.CurrentTargetEntity = Entity.Null;
                    droneData.ValueRW.CurrentState = DroneState.Charging;
                    continue;
                }

                float3 direction = droneData.ValueRO.TargetPosition - transform.ValueRO.Position;
                float distance = math.length(direction);

                if (distance > 0.1f)
                {
                    MoveToTarget(ref transform.ValueRW, droneData.ValueRO.TargetPosition, droneData.ValueRO.Speed, deltaTime);
                }
                else
                {
                    // Hedefe ulaşıldı - Repair logic removed here, now handled in ShipStateSystem
                    if (!SystemAPI.HasComponent<ShipData>(droneData.ValueRO.CurrentTargetEntity))
                    {
                        droneData.ValueRW.IsBusy = false;
                        droneData.ValueRW.CurrentState = DroneState.Idle;
                    }
                }
            }
        }

        private void MoveToTarget(ref LocalTransform transform, float3 target, float speed, float dt)
        {
            float3 direction = target - transform.Position;
            if (math.lengthsq(direction) < 0.01f) return;

            float3 moveDir = math.normalize(direction);
            transform.Position += moveDir * speed * dt;
            transform.Rotation = quaternion.LookRotationSafe(moveDir, math.up());
        }
    }
}
