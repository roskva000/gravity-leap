using Unity.Entities;
using UnityEngine;
using GalacticNexus.Scripts.Components;

namespace GalacticNexus.Scripts.UI
{
    public class UIUpgradeController : MonoBehaviour
    {
        private EntityManager _entityManager;

        private void Start()
        {
            _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
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
            Entity requestEntity = _entityManager.CreateEntity();
            _entityManager.AddComponentData(requestEntity, new UpgradeRequest { Type = type });
        }
    }
}
