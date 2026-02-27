using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using GalacticNexus.Scripts.Components;
using GalacticNexus.Scripts.Juice;

namespace GalacticNexus.Scripts.Systems
{
    [BurstCompile]
    public partial struct MarketSystem : ISystem
    {
        private float timer;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EconomyData>();
            
            // Initialize Market Data
            var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);
            var marketEntity = state.EntityManager.CreateEntity();
            state.EntityManager.AddComponentData(marketEntity, new GlobalMarketData
            {
                SindicatoMultiplier = 1.0f,
                TheCoreMultiplier = 1.0f,
                VoidWalkersMultiplier = 1.0f
            });
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float deltaTime = SystemAPI.Time.DeltaTime;
            timer += deltaTime;

            if (timer >= 60f)
            {
                timer = 0f;
                UpdateMarket(ref state);
            }
        }

        private void UpdateMarket(ref SystemState state)
        {
            if (!SystemAPI.TryGetSingletonRW<GlobalMarketData>(out var market)) return;

            var rand = new Unity.Mathematics.Random((uint)(SystemAPI.Time.ElapsedTime * 1000) + 1);
            
            market.ValueRW.SindicatoMultiplier = rand.NextFloat(0.8f, 1.5f);
            market.ValueRW.TheCoreMultiplier = rand.NextFloat(0.8f, 1.5f);
            market.ValueRW.VoidWalkersMultiplier = rand.NextFloat(0.8f, 1.5f);

            // Notify UI Juice
            var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
            var eventEntity = ecb.CreateEntity();
            ecb.AddComponent(eventEntity, new GameEvent
            {
                Type = GameEventType.Warning, 
                Position = float3.zero,
                Value = 777f // Magic number for MARKET UPDATED in juice bridge
            });
        }
    }
}
