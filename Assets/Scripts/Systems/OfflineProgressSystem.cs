using Unity.Entities;
using GalacticNexus.Scripts.Components;
using GalacticNexus.Scripts.Persistence;
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
                double totalMinutes = TimeSpan.FromTicks(deltaTicks).TotalMinutes;
                
                // Hard-cap: Maksimum 24 saat (1440 dakika)
                totalMinutes = Math.Min(totalMinutes, 1440.0);

                // Formül: Dakika * (DockLevel * BazÖdül * DroneVerimi)
                double avgRewardPerMin = 10.0; // Baz değer
                double efficiency = 1.0 + upgrade.DroneSpeedLevel * 0.1;
                double offlineEarnings = totalMinutes * (upgrade.DockLevel * avgRewardPerMin * efficiency);

                economy.ValueRW.ScrapCurrency += offlineEarnings;
                
                Debug.Log($"Offline Progress: Welcome back Commander! You earned {offlineEarnings:F0} Scrap in {totalMinutes:F1} minutes.");
            }
            
            _isProcessed = true;
        }
    }
}
