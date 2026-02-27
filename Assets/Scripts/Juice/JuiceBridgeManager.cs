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
            var go = Instantiate(FloatingTextPrefab, pos + Vector3.up * 2, Quaternion.identity);
            go.GetComponentInChildren<TextMeshPro>().text = text;
            Destroy(go, 2.0f);
        }
    }
}
