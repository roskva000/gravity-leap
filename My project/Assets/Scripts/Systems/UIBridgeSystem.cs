using Unity.Entities;
using GalacticNexus.Scripts.Components;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace GalacticNexus.Scripts.Systems
{
    public partial struct UIBridgeSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            // Ekonomi verisini al
            if (!SystemAPI.TryGetSingleton<EconomyData>(out var economy)) return;
            
            // UI referanslarını al
            foreach (var uiRefs in SystemAPI.Query<UIReferencesComponent>())
            {
                if (economy.NexusComplete)
                {
                    if (uiRefs.ScrapJuice != null)
                    {
                        uiRefs.ScrapJuice.SetGoldenMode(true);
                        uiRefs.ScrapJuice.SetVibeMode(true);
                    }
                    if (uiRefs.GemsText != null) uiRefs.GemsText.color = new UnityEngine.Color(1f, 0.84f, 0f);
                    if (uiRefs.ScrapText != null) uiRefs.ScrapText.color = new UnityEngine.Color(1f, 0.84f, 0f);
                }

                if (uiRefs.ScrapJuice != null)
                    uiRefs.ScrapJuice.SetTargetValue(economy.ScrapCurrency);
                else
                    uiRefs.TargetScrap = economy.ScrapCurrency; // Fallback
                
                if (uiRefs.GemsText != null)
                    uiRefs.GemsText.SetText($"GEMS: {economy.TotalShipsServiced}");

                // Task AA: Post-Processing Sync
                if (uiRefs.PostProcessVolume != null)
                {
                    if (uiRefs.PostProcessVolume.profile.TryGet<Bloom>(out var bloom))
                    {
                        bool isRaid = false;
                        if (SystemAPI.TryGetSingleton<GlobalMarketData>(out var market))
                            isRaid = market.IsRaidActive;

                        bool isVIP = false;
                        foreach (var ship in SystemAPI.Query<RefRO<ShipData>>())
                        {
                            if (ship.ValueRO.OwnerFraction == FractionType.VoidWalkers) 
                            {
                                isVIP = true;
                                break;
                            }
                        }

                        float targetIntensity = 1.0f; // Default
                        Color targetColor = Color.white;

                        if (isRaid)
                        {
                            targetIntensity = 5.0f;
                            targetColor = Color.red;
                        }
                        else if (isVIP)
                        {
                            targetIntensity = 3.0f;
                            targetColor = new Color(0.5f, 0f, 1f); // Purple
                        }

                        bloom.intensity.value = Mathf.Lerp(bloom.intensity.value, targetIntensity, SystemAPI.Time.DeltaTime * 2f);
                        if (uiRefs.PostProcessVolume.profile.TryGet<ColorAdjustments>(out var ca))
                        {
                            ca.colorFilter.value = Color.Lerp(ca.colorFilter.value, targetColor, SystemAPI.Time.DeltaTime * 2f);
                        }
                    }
                }
            }
        }
    }
}
