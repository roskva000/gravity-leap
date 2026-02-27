using UnityEngine;
using Unity.Entities;
using GalacticNexus.Scripts.Components;
using Unity.Mathematics;

namespace GalacticNexus.Scripts.Persistence
{
    public class PrestigeManager : MonoBehaviour
    {
        private EntityManager _em;

        private void Start()
        {
            _em = World.DefaultGameObjectInjectionWorld.EntityManager;
        }

        public void TryPerformPrestige()
        {
            var qEco = _em.CreateEntityQuery(typeof(EconomyData));
            if (qEco.IsEmptyIgnoreFilter) return;
            var economy = qEco.GetSingleton<EconomyData>();

            // Formül: floor(sqrt(TotalShips / 500))
            double earnedDarkMatter = math.floor(math.sqrt(economy.TotalShipsServiced / 500.0));

            if (earnedDarkMatter > 0)
            {
                PerformPrestige(earnedDarkMatter);
            }
            else
            {
                Debug.Log("Not enough ships serviced for prestige (Need 500+).");
            }
        }

        private void PerformPrestige(double darkMatterGain)
        {
            var qEco = _em.CreateEntityQuery(typeof(EconomyData));
            var qUpg = _em.CreateEntityQuery(typeof(UpgradeData));
            if (qEco.IsEmptyIgnoreFilter || qUpg.IsEmptyIgnoreFilter) return;

            var economy = qEco.GetSingleton<EconomyData>();
            var upgrade = qUpg.GetSingleton<UpgradeData>();

            // 1. Kalıcı verileri güncelle
            economy.DarkMatter += darkMatterGain;
            economy.PrestigeCount++;

            // 2. Sıradan verileri sıfırla
            economy.ScrapCurrency = 0;
            economy.TotalShipsServiced = 0;

            // 3. Yükseltmeleri sıfırla
            upgrade.DockLevel = 1;
            upgrade.DroneSpeedLevel = 1;
            upgrade.DroneBatteryLevel = 1;

            qEco.SetSingleton(economy);
            qUpg.SetSingleton(upgrade);

            // 4. ECS Dünyasını Resetle (Gemi ve Drone'ları temizle)
            ClearAllShipsAndDrones();

            Debug.Log($"PRESTIGE COMPLETE! Gained {darkMatterGain} Dark Matter. Total DM: {economy.DarkMatter}");
            
            // Kaydet
            GetComponent<SaveLoadManager>()?.RequestSave();
        }

        private void ClearAllShipsAndDrones()
        {
            // Basit temizlik: Tüm Gemi ve Drone tag'li entity'leri yok et
            var shipQuery = _em.CreateEntityQuery(typeof(ShipTag));
            _em.DestroyEntity(shipQuery);

            var droneQuery = _em.CreateEntityQuery(typeof(DroneTag));
            _em.DestroyEntity(droneQuery);

            // Dock durumlarını temizle
            var dockQuery = _em.CreateEntityQuery(typeof(DockData));
            var docks = dockQuery.ToComponentDataArray<DockData>(Unity.Collections.Allocator.Temp);
            var entities = dockQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
            
            for (int i = 0; i < entities.Length; i++)
            {
                var d = docks[i];
                d.IsOccupied = false;
                _em.SetComponentData(entities[i], d);
            }
            
            docks.Dispose();
            entities.Dispose();
        }
    }
}
