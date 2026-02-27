using UnityEngine;
using Unity.Entities;
using GalacticNexus.Scripts.Juice;
using TMPro;

namespace GalacticNexus.Scripts.Juice
{
    public class JuiceBridgeManager : MonoBehaviour
    {
        public ParticleSystem DockedVFX;
        public ParticleSystem ServiceFinishedVFX;
        public GameObject FloatingTextPrefab;
        public AudioSource GlobalAudioSource;
        public AudioClip SellSound;

        private UnityEngine.Pool.IObjectPool<GameObject> floatingTextPool;

        private void Awake()
        {
            floatingTextPool = new UnityEngine.Pool.ObjectPool<GameObject>(
                createFunc: () => Instantiate(FloatingTextPrefab),
                actionOnGet: (obj) => obj.SetActive(true),
                actionOnRelease: (obj) => obj.SetActive(false),
                actionOnDestroy: (obj) => Destroy(obj),
                collectionCheck: false,
                defaultCapacity: 10,
                maxSize: 20
            );
        }

        private void Update()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null) return;

            var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);

            // ECS'den gelen olayları işle
            foreach (var (eventData, entity) in world.EntityManager.Query<GameEvent>().WithEntityAccess())
            {
                ProcessEvent(eventData);
                ecb.DestroyEntity(entity); // Olayı tüket
            }

            ecb.Playback(world.EntityManager);
            ecb.Dispose();
        }

        private void ProcessEvent(GameEvent e)
        {
            switch (e.Type)
            {
                case GameEventType.ShipDocked:
                    if (DockedVFX) Instantiate(DockedVFX, e.Position, Quaternion.identity);
                    break;
                    
                case GameEventType.ScrapEarned:
                    SpawnFloatingText(e.Position, $"+{e.Value:F0} SCRAP");
                    if (GlobalAudioSource && SellSound) GlobalAudioSource.PlayOneShot(SellSound);
                    
                    // Task C: Scale UI Pop
                    var worldRef = World.DefaultGameObjectInjectionWorld;
                    if (worldRef != null)
                    {
                        foreach (var uiRefs in worldRef.EntityManager.Query<GalacticNexus.Scripts.Components.UIReferencesComponent>())
                        {
                            if (uiRefs.ScrapJuice != null)
                                uiRefs.ScrapJuice.SetTargetValue(e.Value, e.Scale); // Pass magnitude
                        }
                    }
                    break;
                    
                case GameEventType.Warning:
                    // Task B: Low Battery Warning
                    if (e.Value == 0f) // Battery alert indicator
                        SpawnFloatingText(e.Position, "CRITICAL POWER", true);
                    else
                        SpawnFloatingText(e.Position, "WARNING", true);
                    break;
            }
        }

        private void SpawnFloatingText(Vector3 pos, string text, bool isWarning = false)
        {
            if (!FloatingTextPrefab) return;
            
            var go = floatingTextPool.Get();
            go.transform.position = pos + Vector3.up * 2;
            go.transform.rotation = Quaternion.identity;

            if (go.TryGetComponent<FloatingTextJuice>(out var juice))
            {
                juice.Initialize(text, floatingTextPool);
                if (isWarning) juice.SetToWarning();
            }
            else
            {
                go.GetComponentInChildren<TextMeshPro>().text = text;
            }
        }
    }
}
