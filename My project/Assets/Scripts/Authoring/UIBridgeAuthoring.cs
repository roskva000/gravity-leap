using Unity.Entities;
using UnityEngine;
using TMPro;
using GalacticNexus.Scripts.Components;

namespace GalacticNexus.Scripts.Authoring
{
    public class UIBridgeAuthoring : MonoBehaviour
    {
        public TextMeshProUGUI ScrapText;
        public TextMeshProUGUI GemsText;
        public TextMeshProUGUI ActiveShipsText;

        public class UIBridgeBaker : Baker<UIBridgeAuthoring>
        {
            public override void Bake(UIBridgeAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                
                // Managed component ekle
                AddComponentObject(entity, new UIReferencesComponent
                {
                    ScrapText = authoring.ScrapText,
                    GemsText = authoring.GemsText,
                    ActiveShipsText = authoring.ActiveShipsText
                });
            }
        }
    }
}
