using UnityEngine;
using Unity.Entities;
using System.IO;
using GalacticNexus.Scripts.Components;
using System;

namespace GalacticNexus.Scripts.Persistence
{
    public class SaveLoadManager : MonoBehaviour
    {
        private string SavePath => Path.Combine(Application.persistentDataPath, "nexus_save.json");

        private void Start()
        {
            // Basit bir gecikmeyle Load tetiklenebilir (Entity'lerin hazır olması için)
            Invoke("LoadGame", 0.1f);
        }

        private void OnApplicationQuit()
        {
            SaveGame();
        }

        public void SaveGame()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null) return;
            
            var em = world.EntityManager;

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
            File.WriteAllText(SavePath, json);
            Debug.Log($"Game Saved: {json}");
        }

        public void LoadGame()
        {
            if (!File.Exists(SavePath)) return;

            string json = File.ReadAllText(SavePath);
            var data = JsonUtility.FromJson<GameSaveData>(json);

            var em = World.DefaultGameObjectInjectionWorld.EntityManager;
            
            // Economy & Timestamp Güncelle
            if (em.TryGetSingletonRW<EconomyData>(out var economy))
            {
                economy.ValueRW.ScrapCurrency = data.ScrapCurrency;
                economy.ValueRW.TotalShipsServiced = data.TotalShipsServiced;
                economy.ValueRW.LastSaveTimestamp = data.LastSaveTimestamp;
            }

            // Upgrade Güncelle
            if (em.TryGetSingletonRW<UpgradeData>(out var upgrade))
            {
                upgrade.ValueRW.DockLevel = data.DockLevel;
                upgrade.ValueRW.DroneSpeedLevel = data.DroneSpeedLevel;
                upgrade.ValueRW.DroneBatteryLevel = data.DroneBatteryLevel;
            }

            // Monetization Güncelle
            if (em.TryGetSingletonRW<MonetizationData>(out var monData))
            {
                monData.ValueRW.IsNoAdsPurchased = data.IsNoAdsPurchased;
            }

            Debug.Log($"Game Loaded Successfully from {SavePath}");
        }
    }
}
