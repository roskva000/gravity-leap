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
            if (!SystemAPI.TryGetSingleton<EconomyData>(out var economy)) return;

            float nexusFactor = economy.NexusProgress; // 0.0 to 1.0

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
                    float spawnRoll = rand.NextFloat(0, 1);
                    bool isLegendary = spawnRoll < 0.01f;
                    bool isCritical = !isLegendary && spawnRoll < 0.2f;
                    
                    if (isLegendary)
                    {
                        ecb.SetComponent(newShip, LocalTransform.FromPosition(spawner.ValueRO.SpawnPosition).WithScale(5.0f));
                    }

                    ecb.SetComponent(newShip, new ShipData
                    {
                        Health = 100f * (1.0f - nexusFactor * 0.5f),
                        Fuel = rand.NextFloat(0.1f, 0.5f) * (1.0f - nexusFactor * 0.5f), 
                        CargoCapacity = isLegendary ? 5000f : 1000f,
                        CurrentState = ShipState.Waiting,
                        OwnerFraction = randomFraction,
                        RepairProgress = 0f,
                        Condition = isLegendary ? ShipCondition.Legendary : (isCritical ? ShipCondition.Critical : ShipCondition.Normal),
                        HullIntegrity = isLegendary ? 0.5f : (isCritical ? 0.3f : 1.0f),
                        MoveSpeed = isLegendary ? 1.5f : (isCritical ? 2.5f : 5.0f),
                        RequiredDroneCount = isLegendary ? 10 : 1
                    });

                    // Ödül verisini ekle
                    float baseRewardScale = 1.0f + nexusFactor; // Up to 2x base reward
                    ecb.AddComponent(newShip, new RewardData
                    {
                        BaseReward = (isLegendary ? 500f : 50f) * baseRewardScale,
                        FractionMultiplier = (randomFraction == Fraction.VoidWalkers) ? 1.5f : 1.0f
                    });

                    if (isLegendary)
                    {
                        var eventEntity = ecb.CreateEntity();
                        ecb.AddComponent(eventEntity, new GameEvent
                        {
                            Type = Juice.GameEventType.Warning,
                            Position = spawner.ValueRO.SpawnPosition,
                            Value = 888f // Magic number for LEGENDARY SPAWN
                        });
                    }

                    // Bir sonraki spawn zamanını belirle (Dinamik Trafik)
                    float dynamicInterval = spawner.ValueRO.SpawnInterval / (1.0f + (upgrade.DockLevel * 0.25f) + (nexusFactor * 2.0f));
                    dynamicInterval = math.max(1.5f, dynamicInterval); // Hard-cap limit

                    spawner.ValueRW.NextSpawnTime = currentTime + dynamicInterval;
                }
            }
        }
    }
}
