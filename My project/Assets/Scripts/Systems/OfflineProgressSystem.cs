using Unity.Burst;
using Unity.Entities;
using GalacticNexus.Scripts.Components;
using GalacticNexus.Scripts.Persistence;
using Unity.Mathematics;
using System;
using UnityEngine;

namespace GalacticNexus.Scripts.Systems
{
    [BurstCompile]
    public partial struct OfflineProgressSystem : ISystem
    {
        private bool _isProcessed;

        public void OnCreate(ref SystemState state)
        {
            _isProcessed = false;
        }

        // OnUpdate managed kısımları (DateTime) halleder, asıl hesaplama Burst ile yapılır.
        public void OnUpdate(ref SystemState state)
        {
            if (_isProcessed) return;

            long currentTicks = DateTime.UtcNow.Ticks;
            CalculateOfflineEarnings(ref state, currentTicks);
            
            _isProcessed = true;
        }

        // Not [BurstCompile] — DateTime is managed code, not Burst-compatible
        private void CalculateOfflineEarnings(ref SystemState state, long currentTicks)
        {
            // Singleton'ları kontrol et
            if (!SystemAPI.TryGetSingletonRW<EconomyData>(out var economy)) return;
            if (!SystemAPI.TryGetSingleton<UpgradeData>(out var upgrade)) return;

            long lastTicks = economy.ValueRO.LastSaveTimestamp;
            if (lastTicks <= 0) return;

            long deltaTicks = currentTicks - lastTicks;

            if (deltaTicks > 0)
            {
                // TimeSpan.FromTicks(deltaTicks).TotalSeconds yerine manuel bölme
                double totalSeconds = deltaTicks / 10000000.0;
                
                // Hard-cap: Maksimum 24 saat
                totalSeconds = math.min(totalSeconds, 86400.0);
                double totalMinutes = totalSeconds / 60.0;

                // 1. Prestij Çarpanı
                double prestigeMultiplier = 1.0 + (economy.ValueRO.DarkMatter * 0.10);

                // 2. Reklam Boost Lojiği
                double adMultiplier = 1.0;
                if (SystemAPI.TryGetSingletonRW<MonetizationData>(out var monData))
                {
                    float boostRemaining = monData.ValueRO.AdBoostRemainingSeconds;
                    
                    if (boostRemaining > 0)
                    {
                        double boostedSeconds = math.min(totalSeconds, (double)boostRemaining);
                        double normalSeconds = totalSeconds - boostedSeconds;
                        
                        adMultiplier = ((boostedSeconds * 2.0) + (normalSeconds * 1.0)) / totalSeconds;
                        
                        monData.ValueRW.AdBoostRemainingSeconds = (float)math.max(0, boostRemaining - (float)totalSeconds);
                        if (monData.ValueRW.AdBoostRemainingSeconds <= 0) monData.ValueRW.LastAdMultiplier = 1.0f;
                    }
                }

                // 3. Final Hesaplama
                double avgRewardPerMin = 10.0; 
                double efficiency = 1.0 + upgrade.DroneSpeedLevel * 0.1;
                double baseOfflineEarnings = totalMinutes * (upgrade.DockLevel * avgRewardPerMin * efficiency);
                
                double finalOfflineEarnings = baseOfflineEarnings * prestigeMultiplier * adMultiplier;

                economy.ValueRW.ScrapCurrency += finalOfflineEarnings;
                
                // Debug.Log silindi (Burst uyumu için)
            }
        }
    }
}
