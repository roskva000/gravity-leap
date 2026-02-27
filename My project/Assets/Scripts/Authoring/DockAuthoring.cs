using Unity.Entities;
using GalacticNexus.Scripts.Components;
using UnityEngine;

namespace GalacticNexus.Scripts.Authoring
{
    public class DockAuthoring : MonoBehaviour
    {
        public float ServiceMultiplier = 1.0f;

        public class DockBaker : Baker<DockAuthoring>
        {
            public override void Bake(DockAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Renderable);
                AddComponent(entity, new DockTag());
                AddComponent(entity, new DockData
                {
                    IsOccupied = false,
                    ServiceMultiplier = authoring.ServiceMultiplier
                });
            }
        }
    }
}
