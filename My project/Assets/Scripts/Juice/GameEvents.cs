using Unity.Entities;
using Unity.Mathematics;

namespace GalacticNexus.Scripts.Juice
{
    public enum GameEventType
    {
        ShipDocked,
        ServiceFinished,
        ScrapEarned,
        DroneBoost,
        Warning,
        StoryTrigger,
        DroneMalfunction
    }

    public struct GameEvent : IComponentData
    {
        public GameEventType Type;
        public float3 Position;
        public float Value;
        public float Scale;
    }
}
