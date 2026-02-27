using Unity.Entities;

namespace GalacticNexus.Scripts.Components
{
    public struct SelectedTag : IComponentData, IEnableableComponent {}

    public struct SelectionData : IComponentData
    {
        public bool IsSelected;
    }
}
