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
            if (_em.TryGetSingletonRW<MonetizationData>(out var monData))
            {
                monData.ValueRW.IsNoAdsPurchased = true;
            }
        }

        private void UpdateAdMultiplier(float multiplier, float duration)
        {
            if (_em.TryGetSingletonRW<MonetizationData>(out var monData))
            {
                monData.ValueRW.LastAdMultiplier = multiplier;
                // Süreyi ekle ve 24 saatle sınırla
                monData.ValueRW.AdBoostRemainingSeconds = math.min(86400f, monData.ValueRO.AdBoostRemainingSeconds + duration);
            }
        }
    }
}
