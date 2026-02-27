using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using GalacticNexus.Scripts.Components;
using Unity.Collections;

namespace GalacticNexus.Scripts.Systems
{
    [BurstCompile]
    public partial struct SpawningSystem : ISystem
    {
        private EntityQuery _waitingShipsQuery;

        public void OnCreate(ref SystemState state)
        {
            _waitingShipsQuery = state.GetEntityQuery(ComponentType.ReadOnly<ShipData>(), ComponentType.ReadOnly<ShipTag>());
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float currentTime = (float)SystemAPI.Time.ElapsedTime;
            if (!SystemAPI.TryGetSingleton<UpgradeData>(out var upgrade)) return;

            // Bekleyen gemi sayısını kontrol et
            int waitingCount = 0;
            var ships = _waitingShipsQuery.ToComponentDataArray<ShipData>(Unity.Collections.Allocator.Temp);
            foreach(var s in ships) if(s.CurrentState == ShipState.Waiting) waitingCount++;
            ships.Dispose();

            // Havuz limiti: 5 gemiden fazla bekleyen varsa spawning'i durdur
            if (waitingCount >= 5) return;

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
                        CurrentState = ShipState.Waiting,
                        OwnerFraction = randomFraction,
                        RepairProgress = 0f
                        // TargetDockPosition DockManagementSystem tarafından atanacak
                    });

                    // Ödül verisini ekle (Dinamik ekonomik veri)
                    ecb.AddComponent(newShip, new RewardData
                    {
                        BaseReward = 50f,
                        FractionMultiplier = (randomFraction == Fraction.VoidWalkers) ? 1.5f : 1.0f
                    });

                    // Bir sonraki spawn zamanını belirle (Dinamik Trafik)
                    float dynamicInterval = spawner.ValueRO.SpawnInterval / (1.0f + (upgrade.DockLevel * 0.25f));
                    dynamicInterval = math.max(1.5f, dynamicInterval); // Hard-cap limit

                    spawner.ValueRW.NextSpawnTime = currentTime + dynamicInterval;
                }
            }
        }
    }
}
