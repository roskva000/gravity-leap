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
                    break;
            }
        }

        private void SpawnFloatingText(Vector3 pos, string text)
        {
            if (!FloatingTextPrefab) return;
            
            var go = floatingTextPool.Get();
            go.transform.position = pos + Vector3.up * 2;
            go.transform.rotation = Quaternion.identity;

            if (go.TryGetComponent<FloatingTextJuice>(out var juice))
            {
                juice.Initialize(text, floatingTextPool);
            }
            else
            {
                // Eğer script yoksa fallback (ama plana göre ekledik)
                go.GetComponentInChildren<TextMeshPro>().text = text;
                // Bu durumda Release yönetimi zor olacağı için script olması şart
                Debug.LogWarning("FloatingTextPrefab does not have FloatingTextJuice component!");
            }
        }
    }
}
