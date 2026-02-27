using Unity.Entities;
using Unity.Mathematics;

namespace GalacticNexus.Scripts.Components
{
    public struct SpawnerData : IComponentData
    {
        public Entity ShipPrefab;
        public float SpawnInterval;
        public float NextSpawnTime;
        public float3 SpawnPosition;
        public uint RandomSeed;
    }
}
