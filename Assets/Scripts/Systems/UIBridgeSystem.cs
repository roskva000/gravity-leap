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
            
            // UI referanslarını al (Managed component olduğu için SystemAPI.Query kullanıyoruz)
            foreach (var uiRefs in SystemAPI.Query<UIReferencesComponent>())
            {
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
