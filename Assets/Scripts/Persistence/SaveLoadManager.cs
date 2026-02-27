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
            // Senkron çıkışı bloklamadan son bir deneme (veya Main thread dükkanı kapatmadan hızlıca)
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

                // ECS verilerini Main Thread'de oku
                if (!em.TryGetSingleton<EconomyData>(out var economy)) return;
                if (!em.TryGetSingleton<UpgradeData>(out var upgrade)) return;
                
                bool isNoAds = false;
                if (em.TryGetSingleton<MonetizationData>(out var monData))
                {
                    isNoAds = monData.IsNoAdsPurchased;
                }

                var data = new GameSaveData
                {
                    ScrapCurrency = economy.ScrapCurrency,
                    TotalShipsServiced = economy.TotalShipsServiced,
                    DockLevel = upgrade.DockLevel,
                    DroneSpeedLevel = upgrade.DroneSpeedLevel,
                    DroneBatteryLevel = upgrade.DroneBatteryLevel,
                    IsNoAdsPurchased = isNoAds,
                    LastSaveTimestamp = DateTime.UtcNow.Ticks
                };

                string json = JsonUtility.ToJson(data);

                // Asenkron dosya yazma
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
