using Unity.Entities;
using GalacticNexus.Scripts.Components;

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
            }
        }
    }
}
