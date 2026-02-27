using Unity.Entities;
using GalacticNexus.Scripts.Components;
using UnityEngine;

namespace GalacticNexus.Scripts.Authoring
{
    public class SpawnerAuthoring : MonoBehaviour
    {
        public GameObject ShipPrefab;
        public float SpawnInterval = 5f;
        public Vector3 SpawnPosition;

        public class SpawnerBaker : Baker<SpawnerAuthoring>
        {
            public override void Bake(SpawnerAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new SpawnerData
                {
                    ShipPrefab = GetEntity(authoring.ShipPrefab, TransformUsageFlags.Dynamic),
                    SpawnInterval = authoring.SpawnInterval,
                    NextSpawnTime = 0.1f,
                    SpawnPosition = authoring.SpawnPosition,
                    RandomSeed = 12345
                });
            }
        }
    }
}
