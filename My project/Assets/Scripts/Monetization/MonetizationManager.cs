using UnityEngine;
using Unity.Entities;
using GalacticNexus.Scripts.Components;
using Unity.Mathematics;

namespace GalacticNexus.Scripts.Monetization
{
    public class MonetizationManager : MonoBehaviour
    {
        private EntityManager _em;

        private void Start()
        {
            _em = World.DefaultGameObjectInjectionWorld.EntityManager;
        }

        // Reklam izleme butonu tetikler
        public void ShowRewardedAd()
        {
            Debug.Log("Playing Rewarded Ad...");
            // Reklam bittiğinde: 4 Saatlik Boost Ver
            UpdateAdMultiplier(2.0f, 14400f);
        }

        public void PurchaseNoAds()
        {
            Debug.Log("Purchasing No Ads...");
            // Satın alma başarılıysa:
            var qMon = _em.CreateEntityQuery(typeof(MonetizationData));
            if (!qMon.IsEmptyIgnoreFilter)
            {
                var monData = qMon.GetSingleton<MonetizationData>();
                monData.IsNoAdsPurchased = true;
                qMon.SetSingleton(monData);
            }
        }

        private void UpdateAdMultiplier(float multiplier, float duration)
        {
            var qMon = _em.CreateEntityQuery(typeof(MonetizationData));
            if (!qMon.IsEmptyIgnoreFilter)
            {
                var monData = qMon.GetSingleton<MonetizationData>();
                monData.LastAdMultiplier = multiplier;
                monData.AdBoostRemainingSeconds = math.min(86400f, monData.AdBoostRemainingSeconds + duration);
                qMon.SetSingleton(monData);
            }
        }
    }
}
