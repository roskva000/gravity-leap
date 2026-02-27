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

            foreach (var (transform, droneData) in SystemAPI.Query<RefRW<LocalTransform>, RefRW<DroneData>>().WithAll<DroneTag>())
            {
                if (!droneData.ValueRO.IsBusy) continue;

                float3 direction = droneData.ValueRO.TargetPosition - transform.ValueRO.Position;
                float distance = math.length(direction);

                if (distance > 0.1f)
                {
                    float3 moveDir = math.normalize(direction);
                    transform.ValueRW.Position += moveDir * droneData.ValueRO.Speed * deltaTime;
                    
                    // Hedefe bakış (Rotate towards target)
                    transform.ValueRW.Rotation = quaternion.LookRotationSafe(moveDir, math.up());
                }
                else
                {
                    // Hedefe ulaşıldı
                    if (SystemAPI.HasComponent<ShipData>(droneData.ValueRO.TargetEntity))
                    {
                        var ship = SystemAPI.GetComponentRW<ShipData>(droneData.ValueRO.TargetEntity);
                        
                        // Gemi docked ise servis başlasın
                        if (ship.ValueRO.CurrentState == ShipState.Docked)
                        {
                            ship.ValueRW.CurrentState = ShipState.Servicing;
                        }

                        // Servis ilerlemesi (Şimdilik drone hızıyla doğru orantılı)
                        ship.ValueRW.RepairProgress += deltaTime * 0.2f; 

                        if (ship.ValueRO.RepairProgress >= 1.0f)
                        {
                            droneData.ValueRW.IsBusy = false; // İşlem bitti
                        }
                    }
                    else
                    {
                        droneData.ValueRW.IsBusy = false; // Hedef yok olmuş
                    }
                }
            }
        }
    }
}
