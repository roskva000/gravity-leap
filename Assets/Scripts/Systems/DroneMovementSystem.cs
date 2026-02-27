using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using GalacticNexus.Scripts.Components;

namespace GalacticNexus.Scripts.Systems
{
    [BurstCompile]
    public partial struct DroneMovementSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float deltaTime = SystemAPI.Time.DeltaTime;
            if (!SystemAPI.TryGetSingleton<UpgradeData>(out var upgrade)) return;

            float batteryEfficiency = 1.0f - upgrade.GetBatteryEfficiency(); 
            float passiveRegen = upgrade.SolarCollectorLevel * 0.1f * deltaTime;
            bool blackMarketActive = false;
            float4 dirtyYellow = new float4(0.8f, 0.7f, 0.1f, 1.0f);
            float4 buffWhite = new float4(1.0f, 1.0f, 1.0f, 5.0f); // High intensity white

            // Task T: Detect Nexus Buff Event
            bool nexusFlashActive = false;
            foreach (var ev in SystemAPI.Query<RefRO<GameEvent>>())
            {
                if (ev.ValueRO.Value == 800f) nexusFlashActive = true;
            }

            var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);

            if (SystemAPI.TryGetSingletonRW<BlackMarketModule>(out var bmm))
            {
                if (bmm.ValueRO.IsActive)
                {
                    bmm.ValueRW.Timer -= deltaTime;
                    blackMarketActive = true;
                    if (bmm.ValueRO.Timer <= 0)
                    {
                        bmm.ValueRW.IsActive = false;
                        bmm.ValueRW.PenaltyActive = true;
                    }
                }
            }

            if (SystemAPI.TryGetSingletonRW<ShieldData>(out var shield))
            {
                shield.ValueRW.Integrity = math.min(shield.ValueRO.MaxIntegrity, shield.ValueRO.Integrity + passiveRegen);
                
                // Active Repair Logic
                foreach (var (droneTransform, droneData) in SystemAPI.Query<LocalTransform, RefRW<DroneData>>().WithAll<DroneTag>())
                {
                    if (droneData.ValueRO.CurrentState == DroneState.Working && 
                        math.distance(droneTransform.Position, float3.zero) < 1.0f)
                    {
                        float repairPower = droneData.ValueRO.IsOverclocked ? 5.0f : 2.0f;
                        shield.ValueRW.Integrity = math.min(shield.ValueRO.MaxIntegrity, shield.ValueRO.Integrity + repairPower * deltaTime);
                    }
                }
            }

            foreach (var (transform, droneData, entity) in SystemAPI.Query<RefRW<LocalTransform>, RefRW<DroneData>, Entity>().WithAll<DroneTag>())
            {
                if (droneData.ValueRO.IsMalfunctioning) continue;

                // Handle Penalty
                if (SystemAPI.TryGetSingletonRW<BlackMarketModule>(out var bmmCheck) && bmmCheck.ValueRO.PenaltyActive)
                {
                    var rand = new Unity.Mathematics.Random((uint)(SystemAPI.Time.ElapsedTime * 1000) + (uint)entity.Index);
                    if (rand.NextFloat() < 0.50f) droneData.ValueRW.BatteryLevel = 0;
                    // Reset penalty flag handled outside or after loop? 
                    // Let's do it after the loop for consistency if needed, but per entity roll is here.
                }

                float consumptionMultiplier = blackMarketActive ? 0 : 1.0f;
                consumptionMultiplier *= (1.0f - upgrade.NexusBuffBattery); // Apply permanent buff efficiency

                if (nexusFlashActive)
                    ecb.AddComponent(entity, new NeonColorOverride { Value = buffWhite });
                else if (blackMarketActive)
                    ecb.AddComponent(entity, new NeonColorOverride { Value = dirtyYellow });
                else
                    ecb.RemoveComponent<NeonColorOverride>(entity);

                if (droneData.ValueRO.IsOverclocked)
                {
                    consumptionMultiplier *= math.max(1.5f, 4.0f - upgrade.DroneBatteryLevel * 0.25f); 
                    
                    var rand = new Unity.Mathematics.Random((uint)(SystemAPI.Time.ElapsedTime * 1000) + 1);
                    if (rand.NextFloat() < 0.05f * deltaTime)
                    {
                        droneData.ValueRW.IsMalfunctioning = true;
                        var arızaEvent = ecb.CreateEntity();
                        ecb.AddComponent(arızaEvent, new GameEvent { Type = Juice.GameEventType.Warning, Position = transform.ValueRO.Position, Value = 2.0f });
                        continue;
                    }
                }

                if (droneData.ValueRO.CurrentState == DroneState.Charging)
                {
                    float chargeRate = 0.2f * math.max(1, upgrade.SolarCollectorLevel);
                    droneData.ValueRW.BatteryLevel = math.min(1.0f, droneData.ValueRO.BatteryLevel + deltaTime * chargeRate);
                    if (droneData.ValueRO.BatteryLevel >= 1.0f) droneData.ValueRW.CurrentState = DroneState.Idle;
                }
                else
                {
                    // Move to Target
                    float3 direction = math.normalize(droneData.ValueRO.TargetPosition - transform.ValueRO.Position);
                    float moveDist = (5.0f + upgrade.GetDroneSpeedBonus()) * (1.0f + upgrade.NexusBuffSpeed) * (droneData.ValueRO.IsOverclocked ? 2.0f : 1.0f) * deltaTime;
                    
                    if (math.distance(transform.ValueRO.Position, droneData.ValueRO.TargetPosition) > 0.1f)
                    {
                        transform.ValueRW.Position += direction * moveDist;
                        droneData.ValueRW.BatteryLevel -= deltaTime * 0.02f * consumptionMultiplier * batteryEfficiency;
                    }
                    else if (droneData.ValueRO.CurrentState == DroneState.Working)
                    {
                        // Working at destination
                        droneData.ValueRW.BatteryLevel -= deltaTime * 0.05f * consumptionMultiplier * batteryEfficiency;
                    }
                }

                if (droneData.ValueRO.BatteryLevel <= 0)
                {
                    droneData.ValueRW.BatteryLevel = 0;
                    droneData.ValueRW.CurrentState = DroneState.Charging;
                    droneData.ValueRW.IsBusy = false;
                    droneData.ValueRW.TargetPosition = float3.zero;
                }
            }

            // Global reset of penalty factor
            if (SystemAPI.TryGetSingletonRW<BlackMarketModule>(out var bmmFinal))
                bmmFinal.ValueRW.PenaltyActive = false;
        }
    }
}
