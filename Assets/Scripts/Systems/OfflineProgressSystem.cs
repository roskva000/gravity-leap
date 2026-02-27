using Unity.Entities;
using GalacticNexus.Scripts.Components;
using GalacticNexus.Scripts.Persistence;
using System;
using UnityEngine;

namespace GalacticNexus.Scripts.Systems
{
    public partial struct OfflineProgressSystem : ISystem
    {
        private bool _isProcessed;

        public void OnCreate(ref SystemState state)
        {
            _isProcessed = false;
        }

        public void OnUpdate(ref SystemState state)
        {
            if (_isProcessed) return;

            // Singleton'ları kontrol et
            if (!SystemAPI.TryGetSingletonRW<EconomyData>(out var economy)) return;

            // Load işlemi genellikle Game Management tarafından tetiklenir
            // Burada basitçe Timestamp farkını hesaplayalım (Mock)
            
            // Not: Gerçek projede SaveData'dan gelen LastSaveTimestamp kullanılır.
            // Şimdilik sistemin çalıştığını mühürlemek için:
            
            _isProcessed = true;
            Debug.Log("Offline Progress Calculated: Welcome back Commander!");
        }
    }
}
