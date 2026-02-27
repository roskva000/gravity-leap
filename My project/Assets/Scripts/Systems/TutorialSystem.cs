using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using GalacticNexus.Scripts.Components;
using GalacticNexus.Scripts.Juice;
using Unity.Transforms;

namespace GalacticNexus.Scripts.Systems
{
    [BurstCompile]
    public partial struct TutorialSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.TryGetSingletonRW<EconomyData>(out var economy)) return;

            // Tutorial Step 0: Initial Spawning
            if (economy.ValueRO.TutorialStep == 0)
            {
                // Check if any ship exists
                bool anyShip = false;
                foreach (var _ in SystemAPI.Query<RefRO<ShipData>>().WithAll<ShipTag>())
                {
                    anyShip = true;
                    break;
                }

                if (!anyShip && SystemAPI.TryGetSingleton<SpawnerData>(out var spawner))
                {
                    var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
                    Entity ship = ecb.Instantiate(spawner.ShipPrefab);
                    
                    ecb.SetComponent(ship, LocalTransform.FromPosition(spawner.SpawnPosition));
                    ecb.SetComponent(ship, new ShipData
                    {
                        Health = 100f,
                        Fuel = 0.5f,
                        CargoCapacity = 1000f,
                        CurrentState = ShipState.Waiting,
                        OwnerFraction = Fraction.Sindicato,
                        RepairProgress = 0f,
                        Condition = ShipCondition.Normal,
                        HullIntegrity = 1.0f,
                        MoveSpeed = 5.0f,
                        RequiredDroneCount = 1
                    });

                    ecb.AddComponent(ship, new RewardData { BaseReward = 50f, FractionMultiplier = 1.0f });
                    
                    // Task X: Spawn Tutorial Arrow
                    if (spawner.ArrowPrefab != Entity.Null)
                    {
                        Entity arrow = ecb.Instantiate(spawner.ArrowPrefab);
                        ecb.AddComponent(arrow, new TutorialArrow { TargetEntity = ship });
                        ecb.SetComponent(arrow, LocalTransform.FromPosition(spawner.SpawnPosition + new float3(0, 5, 0)));
                    }

                    // Increment to wait for click
                    economy.ValueRW.TutorialStep = 1;
                }
            }

            // Tutorial Step 1: Detect click (SelectionSystem adds SelectedTag to ships)
            if (economy.ValueRO.TutorialStep == 1)
            {
                bool shipSelected = false;
                foreach (var ship in SystemAPI.Query<RefRO<ShipData>>().WithAll<SelectedTag>())
                {
                    shipSelected = true;
                    break;
                }

                if (shipSelected)
                {
                    economy.ValueRW.TutorialStep = 2;
                }
            }

            // Tutorial Step 2: On first Scrap gain, trigger narrative
            if (economy.ValueRO.TutorialStep == 2 && economy.ValueRO.TotalShipsServiced > 0)
            {
                var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
                
                // Task W: Trigger Narrative
                var storyEntity = ecb.CreateEntity();
                ecb.AddComponent(storyEntity, new GameEvent
                {
                    Type = GameEventType.StoryTrigger,
                    Value = 101f, // Code for "Sindicato Welcome"
                    Position = float3.zero
                });

                // Task X: Cleanup arrows
                foreach (var (arrow, entity) in SystemAPI.Query<TutorialArrow>().WithEntityAccess())
                {
                    ecb.DestroyEntity(entity);
                }
                
                economy.ValueRW.TutorialStep = 3; // Tutorial Complete
            }
        }
    }
}
