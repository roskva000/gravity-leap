using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using UnityEngine;
using GalacticNexus.Scripts.Components;
using Unity.Physics;

namespace GalacticNexus.Scripts.Systems
{
    // BurstCompile eklenmedi çünkü UnityEngine.Camera kullanılıyor.
    public partial struct SelectionSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            // Sol tık veya dokunma kontrolü
            if (!Input.GetMouseButtonDown(0)) return;

            if (Camera.main == null) return;

            // Kamera üzerinden Raycast
            UnityEngine.Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            
            // PhysicsWorld üzerinden Raycast işlemi
            if (!SystemAPI.TryGetSingleton<PhysicsWorldSingleton>(out var physicsWorld)) return;

            RaycastInput rayInput = new RaycastInput
            {
                Start = ray.origin,
                End = (float3)ray.origin + (float3)ray.direction * 500f,
                Filter = CollisionFilter.Default
            };

            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            // Önceki seçimleri temizle (ECB kullanarak)
            foreach (var (tag, entity) in SystemAPI.Query<RefRO<SelectedTag>>().WithEntityAccess())
            {
                ecb.RemoveComponent<SelectedTag>(entity);
            }

            // Yeni seçimi physics world üzerinden yap
            if (physicsWorld.CastRay(rayInput, out var hit))
            {
                Entity hitEntity = hit.Entity;
                
                // Eğer çarpılan entity bir gemi ise seç
                if (state.EntityManager.HasComponent<ShipData>(hitEntity))
                {
                    ecb.AddComponent<SelectedTag>(hitEntity);
                    Debug.Log($"Ship Selected: {hitEntity}");
                }
                
                // Task I & L: Drone Interaction
                if (state.EntityManager.TryGetComponent<DroneData>(hitEntity, out var droneData))
                {
                    if (droneData.IsMalfunctioning)
                    {
                        if (SystemAPI.TryGetSingletonRW<EconomyData>(out var economy))
                        {
                            float repairCost = 50f;
                            if (economy.ValueRO.ScrapCurrency >= repairCost)
                            {
                                economy.ValueRW.ScrapCurrency -= repairCost;
                                droneData.IsMalfunctioning = false;
                                droneData.BatteryLevel = 0.5f; // Give some battery after repair
                                ecb.SetComponent(hitEntity, droneData);
                                
                                var repairEvent = ecb.CreateEntity();
                                ecb.AddComponent(repairEvent, new GameEvent
                                {
                                    Type = GameEventType.Warning, // Using Warning group for FloatingText in bridge
                                    Position = hit.Position,
                                    Value = 999f // Magic number for REPAIRED
                                });
                                Debug.Log("Drone Repaired in Field!");
                            }
                        }
                    }
                    else
                    {
                        droneData.IsOverclocked = !droneData.IsOverclocked;
                        ecb.SetComponent(hitEntity, droneData);
                        Debug.Log($"Drone Overclocked: {droneData.IsOverclocked}");
                    }
                }
            }
        }
    }
}
