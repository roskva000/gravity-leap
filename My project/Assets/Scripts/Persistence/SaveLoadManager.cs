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

                var qEco = em.CreateEntityQuery(typeof(EconomyData));
                var qUpg = em.CreateEntityQuery(typeof(UpgradeData));
                var qMon = em.CreateEntityQuery(typeof(MonetizationData));
                var qShield = em.CreateEntityQuery(typeof(ShieldData));
                var qMarket = em.CreateEntityQuery(typeof(GlobalMarketData));

                if (qEco.IsEmptyIgnoreFilter || qUpg.IsEmptyIgnoreFilter) return;

                var economy = qEco.GetSingleton<EconomyData>();
                var upgrade = qUpg.GetSingleton<UpgradeData>();

                bool isNoAds = false;
                float adBoost = 0f;
                if (!qMon.IsEmptyIgnoreFilter)
                {
                    var monData = qMon.GetSingleton<MonetizationData>();
                    isNoAds = monData.IsNoAdsPurchased;
                    adBoost = monData.AdBoostRemainingSeconds;
                }

                var shield = qShield.IsEmptyIgnoreFilter ? new ShieldData() : qShield.GetSingleton<ShieldData>();
                var market = qMarket.IsEmptyIgnoreFilter ? new GlobalMarketData() : qMarket.GetSingleton<GlobalMarketData>();

                var data = new GameSaveData
                {
                    ScrapCurrency = economy.ScrapCurrency,
                    TotalShipsServiced = economy.TotalShipsServiced,
                    DarkMatter = economy.DarkMatter,
                    PrestigeCount = economy.PrestigeCount,
                    TutorialStep = economy.TutorialStep,
                    NexusProgress = economy.NexusProgress,
                    NexusComplete = economy.NexusComplete,
                    DockLevel = upgrade.DockLevel,
                    DroneSpeedLevel = upgrade.DroneSpeedLevel,
                    DroneBatteryLevel = upgrade.DroneBatteryLevel,
                    ShieldIntegrity = shield.Integrity,
                    SindicatoMultiplier = market.SindicatoMultiplier,
                    TheCoreMultiplier = market.TheCoreMultiplier,
                    VoidWalkersMultiplier = market.VoidWalkersMultiplier,
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
                
                var query = em.CreateEntityQuery(typeof(EconomyData));
                if (!query.IsEmptyIgnoreFilter)
                {
                    var economy = query.GetSingleton<EconomyData>();
                    economy.ScrapCurrency = data.ScrapCurrency;
                    economy.TotalShipsServiced = data.TotalShipsServiced;
                    economy.LastSaveTimestamp = data.LastSaveTimestamp;
                    economy.DarkMatter = data.DarkMatter;
                    economy.PrestigeCount = data.PrestigeCount;
                    economy.TutorialStep = data.TutorialStep;
                    economy.NexusProgress = data.NexusProgress;
                    economy.NexusComplete = data.NexusComplete;
                    query.SetSingleton(economy);
                }

                var qUpgrade = em.CreateEntityQuery(typeof(UpgradeData));
                if (!qUpgrade.IsEmptyIgnoreFilter)
                {
                    var upgrade = qUpgrade.GetSingleton<UpgradeData>();
                    upgrade.DockLevel = data.DockLevel;
                    upgrade.DroneSpeedLevel = data.DroneSpeedLevel;
                    upgrade.DroneBatteryLevel = data.DroneBatteryLevel;
                    qUpgrade.SetSingleton(upgrade);
                }

                var qShield = em.CreateEntityQuery(typeof(ShieldData));
                if (!qShield.IsEmptyIgnoreFilter)
                {
                    var shield = qShield.GetSingleton<ShieldData>();
                    shield.Integrity = data.ShieldIntegrity;
                    shield.MaxIntegrity = 100f; 
                    shield.IsActive = data.ShieldIntegrity > 0;
                    qShield.SetSingleton(shield);
                }

                var qMarket = em.CreateEntityQuery(typeof(GlobalMarketData));
                if (!qMarket.IsEmptyIgnoreFilter)
                {
                    var market = qMarket.GetSingleton<GlobalMarketData>();
                    market.SindicatoMultiplier = data.SindicatoMultiplier;
                    market.TheCoreMultiplier = data.TheCoreMultiplier;
                    market.VoidWalkersMultiplier = data.VoidWalkersMultiplier;
                    if (market.SindicatoMultiplier == 0) market.SindicatoMultiplier = 1.0f;
                    qMarket.SetSingleton(market);
                }

                var qMon = em.CreateEntityQuery(typeof(MonetizationData));
                if (!qMon.IsEmptyIgnoreFilter)
                {
                    var monData = qMon.GetSingleton<MonetizationData>();
                    monData.IsNoAdsPurchased = data.IsNoAdsPurchased;
                    monData.AdBoostRemainingSeconds = data.AdBoostRemainingSeconds;
                    // Reset multiplier if loaded with 0 boost
                    if (data.AdBoostRemainingSeconds <= 0) monData.LastAdMultiplier = 1.0f;
                    qMon.SetSingleton(monData);
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
