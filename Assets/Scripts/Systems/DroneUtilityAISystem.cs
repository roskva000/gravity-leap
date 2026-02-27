using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using GalacticNexus.Scripts.Components;

namespace GalacticNexus.Scripts.Systems
{
    [BurstCompile]
    public partial struct DroneUtilityAISystem : ISystem
    {
        private EntityQuery _dockedShipsQuery;

        public void OnCreate(ref SystemState state)
        {
            _dockedShipsQuery = SystemAPI.QueryBuilder()
                .WithAll<ShipData, ShipTag, LocalTransform>()
                .Build();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Hizmet bekleyen gemileri NativeArray'e topla
            int shipCount = _dockedShipsQuery.CalculateEntityCount();
            if (shipCount == 0) return;

            var shipsData = new NativeArray<ShipData>(shipCount, Allocator.TempJob);
            var shipsPositions = new NativeArray<float3>(shipCount, Allocator.TempJob);
            var shipsEntities = new NativeArray<Entity>(shipCount, Allocator.TempJob);

            int index = 0;
            foreach (var (shipData, shipTransform, shipEntity) in SystemAPI.Query<RefRO<ShipData>, RefRO<LocalTransform>, Entity>().WithAll<ShipTag>())
            {
                if (shipData.ValueRO.CurrentState == ShipState.Docked)
                {
                    shipsData[index] = shipData.ValueRO;
                    shipsPositions[index] = shipTransform.ValueRO.Position;
                    shipsEntities[index] = shipEntity;
                    index++;
                }
            }

            // Sadece gerçekten Docked olanları içeren kırpılmış dizileri oluştur
            var activeShipsData = new NativeArray<ShipData>(index, Allocator.TempJob);
            var activeShipsPositions = new NativeArray<float3>(index, Allocator.TempJob);
            var activeShipsEntities = new NativeArray<Entity>(index, Allocator.TempJob);

            NativeArray<ShipData>.Copy(shipsData, activeShipsData, index);
            NativeArray<float3>.Copy(shipsPositions, activeShipsPositions, index);
            NativeArray<Entity>.Copy(shipsEntities, activeShipsEntities, index);

            shipsData.Dispose();
            shipsPositions.Dispose();
            shipsEntities.Dispose();

            if (index == 0)
            {
                activeShipsData.Dispose();
                activeShipsPositions.Dispose();
                activeShipsEntities.Dispose();
                return;
            }

            // Job'u oluştur ve çalıştır
            var droneJob = new DroneUtilityJob
            {
                ShipsData = activeShipsData,
                ShipsPositions = activeShipsPositions,
                ShipsEntities = activeShipsEntities
            };

            state.Dependency = droneJob.ScheduleParallel(state.Dependency);

            // Dizileri Job bittikten sonra dispose et
            state.Dependency = activeShipsData.Dispose(state.Dependency);
            state.Dependency = activeShipsPositions.Dispose(state.Dependency);
            state.Dependency = activeShipsEntities.Dispose(state.Dependency);
        }
    }

    [BurstCompile]
    public partial struct DroneUtilityJob : IJobEntity
    {
        [ReadOnly] public NativeArray<ShipData> ShipsData;
        [ReadOnly] public NativeArray<float3> ShipsPositions;
        [ReadOnly] public NativeArray<Entity> ShipsEntities;

        public void Execute(RefRW<DroneData> droneData, in LocalTransform droneTransform)
        {
            // Drone meşgulse veya şarjı azsa atla
            if (droneData.ValueRO.IsBusy || droneData.ValueRO.BatteryLevel < 0.2f || droneData.ValueRO.CurrentState == DroneState.Charging) return;

            Entity bestTarget = Entity.Null;
            float bestScore = -1f;
            float3 bestTargetPos = float3.zero;

            for (int i = 0; i < ShipsData.Length; i++)
            {
                float score = CalculateUtilityScore(droneTransform.Position, ShipsPositions[i], ShipsData[i]);
                
                if (score > bestScore)
                {
                    bestScore = score;
                    bestTarget = ShipsEntities[i];
                    bestTargetPos = ShipsPositions[i];
                }
            }

            if (bestTarget != Entity.Null && bestScore > 0.5f)
            {
                droneData.ValueRW.CurrentTargetEntity = bestTarget;
                droneData.ValueRW.IsBusy = true;
                droneData.ValueRW.CurrentState = DroneState.Working;
                droneData.ValueRW.TargetPosition = bestTargetPos;
            }
        }

        private float CalculateUtilityScore(float3 dronePos, float3 shipPos, ShipData ship)
        {
            float distance = math.distance(dronePos, shipPos);
            float distanceFactor = math.clamp(1.0f - (distance / 100f), 0, 1);
            
            float fractionPriority = ship.OwnerFraction == Fraction.VoidWalkers ? 1.2f : 1.0f;
            float urgency = (1.0f - ship.Fuel) + (1.0f - ship.RepairProgress);
            
            return (distanceFactor * 0.4f) + (urgency * 0.4f) * fractionPriority;
        }
    }
