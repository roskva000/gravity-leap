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
                    TotalShipsServiced = 0
                });
            }
        }
    }
}
