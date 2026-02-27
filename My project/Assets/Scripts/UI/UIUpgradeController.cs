using Unity.Entities;
using UnityEngine;
using GalacticNexus.Scripts.Components;
using GalacticNexus.Scripts.Juice;

namespace GalacticNexus.Scripts.UI
{
    public class UIUpgradeController : MonoBehaviour
    {
        private EntityManager _entityManager;

        [Header("UI References")]
        public UnityEngine.UI.Button DroneSpeedButton;
        public UnityEngine.UI.Button DockCapacityButton;
        
        [Header("Style Settings")]
        public Color RustColor = new Color(0.4f, 0.2f, 0.1f);
        public Color NeonColor = new Color(0f, 1f, 0.8f);

        private void OnEnable()
        {
            Juice.JuiceBridgeManager.OnEconomyUpdated += HandleEconomyUpdated;
        }

        private void OnDisable()
        {
            Juice.JuiceBridgeManager.OnEconomyUpdated -= HandleEconomyUpdated;
        }

        private void Start()
        {
            _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        }

        private void HandleEconomyUpdated(double scrap, double neon)
        {
            // Drone Speed Cost (matches UpgradeSystem: 100 * 1.15^level)
            // For now use simplified detection or query the UpgradeData singleton
            UpdateSlot(DroneSpeedButton, scrap >= 100);
            UpdateSlot(DockCapacityButton, scrap >= 500);
        }

        private void UpdateSlot(UnityEngine.UI.Button btn, bool canAfford)
        {
            if (btn == null) return;
            btn.interactable = canAfford;
            var colors = btn.colors;
            colors.normalColor = canAfford ? NeonColor : RustColor;
            btn.colors = colors;
        }

        public void RequestDroneSpeedUpgrade()
        {
            CreateRequest(UpgradeType.DroneSpeed);
        }

        public void RequestDockCapacityUpgrade()
        {
            CreateRequest(UpgradeType.DockCapacity);
        }

        private void CreateRequest(UpgradeType type)
        {
            if (_entityManager == default) _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            Entity requestEntity = _entityManager.CreateEntity();
            _entityManager.AddComponentData(requestEntity, new UpgradeRequest { Type = type });
        }
    }
}
