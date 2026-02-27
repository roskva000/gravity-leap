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
            }
        }
    }
}
