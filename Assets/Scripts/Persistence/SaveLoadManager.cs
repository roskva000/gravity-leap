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

        public void SaveGame()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (!world.EntityManager.TryGetSingleton<EconomyData>(out var economy)) return;
            if (!world.EntityManager.TryGetSingleton<UpgradeData>(out var upgrade)) return;

            var data = new GameSaveData
            {
                ScrapCurrency = economy.ScrapCurrency,
                TotalShipsServiced = economy.TotalShipsServiced,
                DockLevel = upgrade.DockLevel,
                DroneSpeedLevel = upgrade.DroneSpeedLevel,
                DroneBatteryLevel = upgrade.DroneBatteryLevel,
                LastSaveTimestamp = DateTime.UtcNow.Ticks
            };

            string json = JsonUtility.ToJson(data);
            File.WriteAllText(SavePath, json);
            Debug.Log($"Game Saved to {SavePath}");
        }

        public void LoadGame()
        {
            if (!File.Exists(SavePath)) return;

            string json = File.ReadAllText(SavePath);
            var data = JsonUtility.FromJson<GameSaveData>(json);

            var em = World.DefaultGameObjectInjectionWorld.EntityManager;
            
            // Economy Güncelle
            if (em.TryGetSingletonRW<EconomyData>(out var economy))
            {
                economy.ValueRW.ScrapCurrency = data.ScrapCurrency;
                economy.ValueRW.TotalShipsServiced = data.TotalShipsServiced;
            }

            // Upgrade Güncelle
            if (em.TryGetSingletonRW<UpgradeData>(out var upgrade))
            {
                upgrade.ValueRW.DockLevel = data.DockLevel;
                upgrade.ValueRW.DroneSpeedLevel = data.DroneSpeedLevel;
            }

            Debug.Log("Game Loaded Successfully");
        }
    }
}
