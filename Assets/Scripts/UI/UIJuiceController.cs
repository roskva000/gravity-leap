using UnityEngine;
using TMPro;
using Unity.Entities;
using GalacticNexus.Scripts.Components;

namespace GalacticNexus.Scripts.UI
{
    public class UIJuiceController : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float countSpeed = 5f;
        [SerializeField] private float popScale = 1.2f;
        [SerializeField] private float popDuration = 0.2f;
        [SerializeField] private Color activeColor = new Color(1f, 0.5f, 0f); // Orange
        [SerializeField] private Color normalColor = Color.white;

        private TextMeshProUGUI scrapText;
        private double displayedValue;
        private double targetValue;
        
        private Vector3 originalScale;
        private float popTimer;
        private bool isPopping;

        private void Start()
        {
            scrapText = GetComponent<TextMeshProUGUI>();
            originalScale = transform.localScale;
            
            // Başlangıç değerini alabilmek için ECS World'e bak
            UpdateValuesFromECS();
            displayedValue = targetValue;
            UpdateText();
        }

        private void Update()
        {
            UpdateValuesFromECS();

            if (displayedValue < targetValue)
            {
                double prevValue = displayedValue;
                displayedValue = Mathf.MoveTowards((float)displayedValue, (float)targetValue, (float)(targetValue - displayedValue) * countSpeed * Time.deltaTime + 1f);
                
                // Sadece tam sayı değiştiğinde metni güncelle
                if (Mathf.FloorToInt((float)prevValue) != Mathf.FloorToInt((float)displayedValue))
                {
                    UpdateText();
                }

                if (!isPopping)
                {
                    StartPop();
                }
            }
            else if (displayedValue > targetValue)
            {
                // Değer azaldığında (harcama) aniden düşebilir veya yavaşça azalabilir
                displayedValue = targetValue;
                UpdateText();
            }

            HandlePopAnimation();
        }

        private void UpdateValuesFromECS()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null) return;

            var entityManager = world.EntityManager;
            var query = entityManager.CreateEntityQuery(typeof(UIReferencesComponent));
            
            if (query.HasSingleton<UIReferencesComponent>())
            {
                var uiRefs = query.GetSingleton<UIReferencesComponent>();
                targetValue = uiRefs.TargetScrap;
            }
        }

        private void UpdateText()
        {
            if (scrapText != null)
            {
                scrapText.text = $"SCRAP: {displayedValue:F0}";
            }
        }

        private void StartPop()
        {
            isPopping = true;
            popTimer = popDuration;
            if (scrapText != null) scrapText.color = activeColor;
        }

        private void HandlePopAnimation()
        {
            if (isPopping)
            {
                popTimer -= Time.deltaTime;
                float progress = 1f - (popTimer / popDuration);
                
                // Basit bir sinüs dalgası ile pop etkisi
                float scaleEffect = Mathf.Sin(progress * Mathf.PI);
                transform.localScale = originalScale * (1f + (popScale - 1f) * scaleEffect);

                if (popTimer <= 0)
                {
                    isPopping = false;
                    transform.localScale = originalScale;
                    if (scrapText != null) scrapText.color = normalColor;
                }
            }
        }
    }
}
