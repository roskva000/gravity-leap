using Unity.Entities;
using GalacticNexus.Scripts.Components;
using UnityEngine;

namespace GalacticNexus.Scripts.Authoring
{
    public class GlobalEconomyAuthoring : MonoBehaviour
    {
        public double InitialScrap = 1000;

        public class GlobalEconomyBaker : Baker<GlobalEconomyAuthoring>
        {
            public override void Bake(GlobalEconomyAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new EconomyData
                {
                    ScrapCurrency = authoring.InitialScrap,
                    NeonCurrency = 0,
                    TotalShipsServiced = 0,
                    NexusComplete = true, // Hemen akışı görmeliyiz
                    TutorialStep = 10,  // Spawner blocker'ı bypass et (Step < 3 engeli var)
                    NexusProgress = 0.1f
                });
            }
        }
    }
}
