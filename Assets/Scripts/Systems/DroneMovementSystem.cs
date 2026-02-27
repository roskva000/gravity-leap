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

            foreach (var (transform, droneData) in SystemAPI.Query<RefRW<LocalTransform>, RefRW<DroneData>>().WithAll<DroneTag>())
            {
                // Durum Yönetimi (FSM)
                if (droneData.ValueRO.CurrentState == DroneState.Charging)
                {
                    // Şarj ol (0.2f per second)
                    droneData.ValueRW.BatteryLevel = math.min(1.0f, droneData.ValueRO.BatteryLevel + deltaTime * 0.2f);
                    
                    // Şarj bittiğinde bayrağı sıfırla
                    if (droneData.ValueRO.BatteryLevel >= 1.0f)
                    {
                        droneData.ValueRW.CurrentState = DroneState.Idle;
                        droneData.ValueRW.WasBatteryWarningSent = false;
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
                droneData.ValueRW.BatteryLevel -= deltaTime * 0.05f * batteryEfficiency;

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
