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

            foreach (var (transform, droneData) in SystemAPI.Query<RefRW<LocalTransform>, RefRW<DroneData>>().WithAll<DroneTag>())
            {
                // Durum Yönetimi (FSM)
                if (droneData.ValueRO.CurrentState == DroneState.Charging)
                {
                    // Şarj ol (0.2f per second)
                    droneData.ValueRW.BatteryLevel = math.min(1.0f, droneData.ValueRO.BatteryLevel + deltaTime * 0.2f);
                    
                    // Şarj noktasına git (Merkez)
                    MoveToTarget(ref transform.ValueRW, new float3(0, 0, 0), droneData.ValueRO.Speed, deltaTime);

                    if (droneData.ValueRO.BatteryLevel >= 1.0f)
                    {
                        droneData.ValueRW.CurrentState = DroneState.Idle;
                    }
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

                // Kritik Şarj Kontrolü
                if (droneData.ValueRO.BatteryLevel < 0.2f)
                {
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
                    // Hedefe ulaşıldı
                    if (SystemAPI.HasComponent<ShipData>(droneData.ValueRO.CurrentTargetEntity))
                    {
                        var ship = SystemAPI.GetComponentRW<ShipData>(droneData.ValueRO.CurrentTargetEntity);
                        
                        if (ship.ValueRO.CurrentState == ShipState.Docked)
                        {
                            ship.ValueRW.CurrentState = ShipState.Servicing;
                        }

                        ship.ValueRW.RepairProgress += deltaTime * 0.2f; 

                        if (ship.ValueRO.RepairProgress >= 1.0f)
                        {
                            droneData.ValueRW.IsBusy = false;
                            droneData.ValueRW.CurrentState = DroneState.Idle;
                        }
                    }
                    else
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
