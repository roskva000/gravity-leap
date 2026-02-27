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
            foreach (var ship in SystemAPI.Query<RefRO<ShipData>>().WithAll<ShipTag>())
            {
                if (ship.ValueRO.CurrentState == ShipState.Waiting) waitingCount++;
            }

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
                    // Zaman bazlı entropi ekleyerek her spawn'ın farklı olmasını sağla
                    uint seed = spawner.ValueRO.RandomSeed + (uint)(SystemAPI.Time.ElapsedTime * 1000) + (uint)state.WorldUnmanaged.EntityManager.GetEntityCount();
                    var rand = new Random(seed);
                    Fraction randomFraction = (Fraction)rand.NextInt(0, 3);

                    // Gemiyi başlangıç pozisyonuna yerleştir
                    ecb.SetComponent(newShip, LocalTransform.FromPosition(spawner.ValueRO.SpawnPosition));
                    
                    // Gemi verilerini güncelle
                    bool isCritical = rand.NextFloat(0, 1) < 0.2f;
                    
                    ecb.SetComponent(newShip, new ShipData
                    {
                        Health = 100f,
                        Fuel = rand.NextFloat(0.1f, 0.5f), // Yarım depo gelmiş
                        CargoCapacity = 1000f,
                        CurrentState = ShipState.Waiting,
                        OwnerFraction = randomFraction,
                        RepairProgress = 0f,
                        Condition = isCritical ? ShipCondition.Critical : ShipCondition.Normal,
                        HullIntegrity = isCritical ? 0.3f : 1.0f,
                        MoveSpeed = isCritical ? 2.5f : 5.0f
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
