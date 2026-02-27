using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using Unity.Collections;
using GalacticNexus.Scripts.Components;

namespace GalacticNexus.Scripts.Systems
{
    [BurstCompile]
    public partial struct DockManagementSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // 1. Boş Dock'ları topla
            var docksQuery = SystemAPI.Query<RefRW<DockData>, LocalTransform>();
            var availableDocks = new NativeList<Entity>(Allocator.Temp);
            var dockTransforms = new NativeList<LocalTransform>(Allocator.Temp);

            foreach (var (dock, transform, entity) in docksQuery.WithEntityAccess())
            {
                if (!dock.ValueRO.IsOccupied)
                {
                    availableDocks.Add(entity);
                    dockTransforms.Add(transform);
                }
            }

            if (availableDocks.Length == 0) return;

            // 2. Bekleyen gemileri bul ve dock ata
            int dockIndex = 0;
            foreach (var (ship, shipEntity) in SystemAPI.Query<RefRW<ShipData>>().WithEntityAccess())
            {
                if (ship.ValueRO.CurrentState == ShipState.Waiting)
                {
                    if (dockIndex >= availableDocks.Length) break;

                    // Dock ata
                    Entity dockEntity = availableDocks[dockIndex];
                    var dockRef = SystemAPI.GetComponentRW<DockData>(dockEntity);
                    
                    ship.ValueRW.TargetDockPosition = dockTransforms[dockIndex].Position;
                    ship.ValueRW.AssignedDockEntity = dockEntity; // Referansı kaydet
                    ship.ValueRW.CurrentState = ShipState.Approaching;
                    dockRef.ValueRW.IsOccupied = true;
                    
                    dockIndex++;
                }
            }

            // Temizlik
            availableDocks.Dispose();
            dockTransforms.Dispose();
        }
    }
}
