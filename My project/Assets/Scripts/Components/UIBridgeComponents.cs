using Unity.Entities;
using TMPro;
using UnityEngine.Rendering;
using GalacticNexus.Scripts.UI;

namespace GalacticNexus.Scripts.Components
{
    // Managed component - Unity UI elementlerini ECS dünyasına bağlar
    public class UIReferencesComponent : IComponentData
    {
        public TextMeshProUGUI ScrapText;
        public TextMeshProUGUI GemsText;
        public TextMeshProUGUI ActiveShipsText;
        public UIJuiceController ScrapJuice;
        public Volume PostProcessVolume;
        public double TargetScrap;
    }
}
