using UnityEngine;
using Unity.Entities;
using GalacticNexus.Scripts.Components;

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
            // Reklam bittiğinde:
            UpdateAdMultiplier(2.0f);
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

        private void UpdateAdMultiplier(float multiplier)
        {
            if (_em.TryGetSingletonRW<MonetizationData>(out var monData))
            {
                monData.ValueRW.LastAdMultiplier = multiplier;
            }
        }
    }
}
