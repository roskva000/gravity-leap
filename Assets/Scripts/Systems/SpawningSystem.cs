using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using GalacticNexus.Scripts.Components;

namespace GalacticNexus.Scripts.Systems
{
    [BurstCompile]
    public partial struct SpawningSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float currentTime = (float)SystemAPI.Time.ElapsedTime;

            foreach (var spawner in SystemAPI.Query<RefRW<SpawnerData>>())
            {
                if (currentTime >= spawner.ValueRO.NextSpawnTime)
                {
                    // Yeni gemi oluştur (Instantiate)
                    var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
                    var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
                    
                    Entity newShip = ecb.Instantiate(spawner.ValueRO.ShipPrefab);
                    
                    // Rastgele veriler ata
                    var rand = new Random(spawner.ValueRO.RandomSeed + (uint)state.WorldUnmanaged.EntityManager.GetEntityCount());
                    Fraction randomFraction = (Fraction)rand.NextInt(0, 3);

                    // Gemiyi başlangıç pozisyonuna yerleştir
                    ecb.SetComponent(newShip, LocalTransform.FromPosition(spawner.ValueRO.SpawnPosition));
                    
                    // Gemi verilerini güncelle
                    ecb.SetComponent(newShip, new ShipData
                    {
                        Health = 100f,
                        Fuel = rand.NextFloat(0.1f, 0.5f), // Yarım depo gelmiş
                        CargoCapacity = 1000f,
                        CurrentState = ShipState.Approaching,
                        OwnerFraction = randomFraction,
                        RepairProgress = 0f
                    });

                    // Ödül verisini ekle (Dinamik ekonomik veri)
                    ecb.AddComponent(newShip, new RewardData
                    {
                        BaseReward = 50f,
                        FractionMultiplier = (randomFraction == Fraction.VoidWalkers) ? 1.5f : 1.0f
                    });

                    // Bir sonraki spawn zamanını belirle
                    spawner.ValueRW.NextSpawnTime = currentTime + spawner.ValueRO.SpawnInterval;
                }
            }
        }
    }
}
