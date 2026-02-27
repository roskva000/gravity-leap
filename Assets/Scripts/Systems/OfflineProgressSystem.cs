using Unity.Entities;
using GalacticNexus.Scripts.Components;
using GalacticNexus.Scripts.Persistence;
using Unity.Mathematics;
using System;
using UnityEngine;

namespace GalacticNexus.Scripts.Systems
{
    public partial struct OfflineProgressSystem : ISystem
    {
        private bool _isProcessed;

        public void OnCreate(ref SystemState state)
        {
            _isProcessed = false;
        }

        public void OnUpdate(ref SystemState state)
        {
            if (_isProcessed) return;

            // Singleton'ları kontrol et
            if (!SystemAPI.TryGetSingletonRW<EconomyData>(out var economy)) return;
            if (!SystemAPI.TryGetSingleton<UpgradeData>(out var upgrade)) return;

            long lastTicks = economy.ValueRO.LastSaveTimestamp;
            if (lastTicks <= 0) 
            {
                _isProcessed = true;
                return;
            }

            long currentTicks = DateTime.UtcNow.Ticks;
            long deltaTicks = currentTicks - lastTicks;

            if (deltaTicks > 0)
            {
                double totalSeconds = TimeSpan.FromTicks(deltaTicks).TotalSeconds;
                double totalMinutes = totalSeconds / 60.0;
                
                // Hard-cap: Maksimum 24 saat
                totalSeconds = Math.Min(totalSeconds, 86400.0);
                totalMinutes = totalSeconds / 60.0;

                // 1. Prestij Çarpanı
                double prestigeMultiplier = 1.0 + (economy.ValueRO.DarkMatter * 0.10);

                // 2. Reklam Boost Lojiği
                double adMultiplier = 1.0;
                if (SystemAPI.TryGetSingletonRW<MonetizationData>(out var monData))
                {
                    float boostRemaining = monData.ValueRO.AdBoostRemainingSeconds;
                    
                    if (boostRemaining > 0)
                    {
                        // Offline sürenin ne kadarı boostlu geçecek?
                        double boostedSeconds = Math.Min(totalSeconds, (double)boostRemaining);
                        double normalSeconds = totalSeconds - boostedSeconds;
                        
                        // Ağırlıklı çarpan hesapla (Örn: 1 saat offline, 30dk boost varsa -> 1.5x ortalama)
                        adMultiplier = ((boostedSeconds * 2.0) + (normalSeconds * 1.0)) / totalSeconds;
                        
                        // Kalan süreyi güncelle
                        monData.ValueRW.AdBoostRemainingSeconds = (float)Math.Max(0, boostRemaining - totalSeconds);
                        if (monData.ValueRW.AdBoostRemainingSeconds <= 0) monData.ValueRW.LastAdMultiplier = 1.0f;
                    }
                }

                // 3. Final Hesaplama
                double avgRewardPerMin = 10.0; 
                double efficiency = 1.0 + upgrade.DroneSpeedLevel * 0.1;
                double baseOfflineEarnings = totalMinutes * (upgrade.DockLevel * avgRewardPerMin * efficiency);
                
                double finalOfflineEarnings = baseOfflineEarnings * prestigeMultiplier * adMultiplier;

                economy.ValueRW.ScrapCurrency += finalOfflineEarnings;
                
                Debug.Log($"Offline Progress: Welcome back! You earned {finalOfflineEarnings:F0} Scrap ({totalMinutes:F1} min, Multi: {adMultiplier:F2}x).");
            }
            
            _isProcessed = true;
        }
    }
}
