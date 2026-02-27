using UnityEngine;
using TMPro;

namespace GalacticNexus.Scripts.UI
{
    public class UIJuiceController : MonoBehaviour
    {
        [Header("Juice Settings")]
        [SerializeField] private float smoothTime = 0.15f;
        [SerializeField] private float popScale = 1.25f;
        [SerializeField] private float popDuration = 0.3f;
        
        [Header("Color Settings")]
        [SerializeField, ColorUsage(true, true)] private Color glowColor = new Color(1f, 0.37f, 0f, 1f); // #FF5E00
        [SerializeField] private Color baseColor = Color.white;

        private TextMeshProUGUI scrapText;
        private double displayedValue;
        private double targetValue;
        private float currentVelocity; // For SmoothDamp
        
        private Vector3 originalScale;
        private float popTimer;
        private bool isPopping;

        // Reference caching to avoid GC
        private static readonly string PREFIX = "SCRAP: ";

        private void Awake()
        {
            scrapText = GetComponent<TextMeshProUGUI>();
            originalScale = transform.localScale;
        }

        public void SetTargetValue(double newValue, float magnitudeMultiplier = 1f)
        {
            if (Mathf.Approximately((float)targetValue, (float)newValue)) return;
            
            if (newValue > targetValue)
            {
                StartPop(magnitudeMultiplier);
            }
            
            targetValue = newValue;
        }

        private bool isVibing;
        private Color goldenColor = new Color(1f, 0.84f, 0f, 1f); // #FFD700

        public void SetGoldenMode(bool enabled)
        {
            baseColor = enabled ? goldenColor : Color.white;
            if (scrapText != null) scrapText.color = baseColor;
        }

        public void SetVibeMode(bool enabled) => isVibing = enabled;

        private void Update()
        {
            // Vibe effect (Continuous subtle vibration)
            if (isVibing)
            {
                float vibe = Mathf.Sin(Time.time * 5f) * 0.05f;
                transform.localScale = originalScale * (1f + vibe);
            }

            // Spring-like smooth movement for the number
            double prevValue = displayedValue;
            displayedValue = Mathf.SmoothDamp((float)displayedValue, (float)targetValue, ref currentVelocity, smoothTime);

            // Update text only when the whole number changes to save performance
            if (Mathf.FloorToInt((float)prevValue) != Mathf.FloorToInt((float)displayedValue))
            {
                UpdateText();
            }

            HandlePopAnimation();
        }

        private void UpdateText()
        {
            if (scrapText != null)
            {
                scrapText.text = $"{PREFIX}{displayedValue:F0}";
            }
        }

        private float currentPopScale = 1.25f;

        private void StartPop(float magnitudeMultiplier = 1f)
        {
            isPopping = true;
            popTimer = popDuration;
            currentPopScale = 1f + (popScale - 1f) * magnitudeMultiplier;
        }

        private void HandlePopAnimation()
        {
            if (!isPopping) return;

            popTimer -= Time.deltaTime;
            float progress = 1f - (popTimer / popDuration);
            
            // Fast out, slow in curve for the "Pop"
            float curve = Mathf.Sin(progress * Mathf.PI);
            
            // Scale animation
            transform.localScale = originalScale * (1f + (currentPopScale - 1f) * curve);

            // Color / Glow animation
            if (scrapText != null)
            {
                scrapText.color = Color.Lerp(baseColor, glowColor, curve);
            }

            if (popTimer <= 0)
            {
                isPopping = false;
                transform.localScale = originalScale;
                if (scrapText != null) scrapText.color = baseColor;
            }
        }
    }
}
