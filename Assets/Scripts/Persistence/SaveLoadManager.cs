using UnityEngine;
using Unity.Entities;
using System.IO;
using GalacticNexus.Scripts.Components;
using System;
using System.Threading.Tasks;

namespace GalacticNexus.Scripts.Persistence
{
    public class SaveLoadManager : MonoBehaviour
    {
        private string SavePath => Path.Combine(Application.persistentDataPath, "nexus_save.json");
        private bool _isSaving = false;

        private void Start()
        {
            Invoke("LoadGame", 0.1f);
        }

        private async void OnApplicationQuit()
        {
            await SaveGameAsync();
        }

        public async void RequestSave()
        {
            await SaveGameAsync();
        }

        public async Task SaveGameAsync()
        {
            if (_isSaving) return;
            _isSaving = true;

            try
            {
                var world = World.DefaultGameObjectInjectionWorld;
                if (world == null) return;
                
                var em = world.EntityManager;

                if (!em.TryGetSingleton<EconomyData>(out var economy)) return;
                if (!em.TryGetSingleton<UpgradeData>(out var upgrade)) return;
                
                bool isNoAds = false;
                float adBoost = 0f;
                if (em.TryGetSingleton<MonetizationData>(out var monData))
                {
                    isNoAds = monData.IsNoAdsPurchased;
                    adBoost = monData.AdBoostRemainingSeconds;
                }

                var data = new GameSaveData
                {
                    ScrapCurrency = economy.ScrapCurrency,
                    TotalShipsServiced = economy.TotalShipsServiced,
                    DarkMatter = economy.DarkMatter,
                    PrestigeCount = economy.PrestigeCount,
                    DockLevel = upgrade.DockLevel,
                    DroneSpeedLevel = upgrade.DroneSpeedLevel,
                    DroneBatteryLevel = upgrade.DroneBatteryLevel,
                    IsNoAdsPurchased = isNoAds,
                    AdBoostRemainingSeconds = adBoost,
                    LastSaveTimestamp = DateTime.UtcNow.Ticks
                };

                string json = JsonUtility.ToJson(data);

                await Task.Run(async () =>
                {
                    using (FileStream sourceStream = new FileStream(SavePath,
                        FileMode.Create, FileAccess.Write, FileShare.None,
                        bufferSize: 4096, useAsync: true))
                    {
                        using (StreamWriter writer = new StreamWriter(sourceStream))
                        {
                            await writer.WriteAsync(json);
                        }
                    }
                });

                Debug.Log("Game Saved Asynchronously");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Save Error: {ex.Message}");
            }
            finally
            {
                _isSaving = false;
            }
        }

        public void LoadGame()
        {
            if (!File.Exists(SavePath)) return;

            try
            {
                string json = File.ReadAllText(SavePath);
                var data = JsonUtility.FromJson<GameSaveData>(json);

                var em = World.DefaultGameObjectInjectionWorld.EntityManager;
                
                if (em.TryGetSingletonRW<EconomyData>(out var economy))
                {
                    economy.ValueRW.ScrapCurrency = data.ScrapCurrency;
                    economy.ValueRW.TotalShipsServiced = data.TotalShipsServiced;
                    economy.ValueRW.LastSaveTimestamp = data.LastSaveTimestamp;
                    economy.ValueRW.DarkMatter = data.DarkMatter;
                    economy.ValueRW.PrestigeCount = data.PrestigeCount;
                }

                if (em.TryGetSingletonRW<UpgradeData>(out var upgrade))
                {
                    upgrade.ValueRW.DockLevel = data.DockLevel;
                    upgrade.ValueRW.DroneSpeedLevel = data.DroneSpeedLevel;
                    upgrade.ValueRW.DroneBatteryLevel = data.DroneBatteryLevel;
                }

                if (em.TryGetSingletonRW<MonetizationData>(out var monData))
                {
                    monData.ValueRW.IsNoAdsPurchased = data.IsNoAdsPurchased;
                    monData.ValueRW.AdBoostRemainingSeconds = data.AdBoostRemainingSeconds;
                    // Reset multiplier if loaded with 0 boost
                    if (data.AdBoostRemainingSeconds <= 0) monData.ValueRW.LastAdMultiplier = 1.0f;
                }

                Debug.Log($"Game Loaded Successfully from {SavePath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Load Error: {ex.Message}");
            }
        }
    }
}
