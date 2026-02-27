using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using UnityEngine;
using GalacticNexus.Scripts.Components;

namespace GalacticNexus.Scripts.Systems
{
    public partial class SelectionSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            // Sol tık veya dokunma kontrolü
            if (!Input.GetMouseButtonDown(0)) return;

            // Kamera üzerinden Raycast (Hybrid yaklaşım)
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            
            // Tüm gemileri dolaş ve en yakını bul (Şimdilik basit mesafe kontrolü)
            // Not: Gerçek projede Physics World Raycast kullanılması önerilir.
            Entity closestEntity = Entity.Null;
            float minDistance = float.MaxValue;

            Entities.ForEach((Entity entity, in LocalTransform transform, in ShipData ship) =>
            {
                // Basit küresel çarpışma kontrolü (Radius: 2.0)
                float3 shipPos = transform.Position;
                float distanceToRay = math.length(math.cross(ray.direction, (float3)ray.origin - shipPos));

                if (distanceToRay < 2.0f)
                {
                    float distToCam = math.distance(ray.origin, shipPos);
                    if (distToCam < minDistance)
                    {
                        minDistance = distToCam;
                        closestEntity = entity;
                    }
                }
            }).Run();

            // Önceki seçimleri temizle
            EntityManager.RemoveComponent<SelectedTag>(SystemAPI.QueryBuilder().WithAll<SelectedTag>().Build());

            // Yeni seçimi işaretle
            if (closestEntity != Entity.Null)
            {
                EntityManager.AddComponent<SelectedTag>(closestEntity);
                Debug.Log($"Ship Selected: {closestEntity}");
            }
        }
    }
}
