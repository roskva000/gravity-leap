using Unity.Entities;
using TMPro;

namespace GalacticNexus.Scripts.Components
{
    // Managed component - Unity UI elementlerini ECS dünyasına bağlar
    public class UIReferencesComponent : IComponentData
    {
        public TextMeshProUGUI ScrapText;
        public TextMeshProUGUI GemsText;
        public TextMeshProUGUI ActiveShipsText;
        public UIJuiceController ScrapJuice;
        public double TargetScrap;
    }
}
