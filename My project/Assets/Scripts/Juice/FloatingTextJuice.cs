using UnityEngine;
using TMPro;
using UnityEngine.Pool;

namespace GalacticNexus.Scripts.Juice
{
    public class FloatingTextJuice : MonoBehaviour
    {
        [Header("Physics Settings")]
        [SerializeField] private float initialVelocityMin = 3f;
        [SerializeField] private float initialVelocityMax = 5f;
        [SerializeField] private float xDriftRange = 2f;
        [SerializeField] private float gravity = 2f; // Downward deceleration
        
        [Header("Visual Settings")]
        [SerializeField] private float duration = 2.0f;
        [SerializeField] private float startScale = 1.5f;
        [SerializeField] private AnimationCurve scaleCurve = AnimationCurve.Linear(0, 1, 1, 0.5f);
        [SerializeField] private AnimationCurve alphaCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
        
        [Header("Color Transition")]
        [SerializeField, ColorUsage(true, true)] private Color neonOrange = new Color(1f, 0.55f, 0f, 1f); // #FF8C00
        [SerializeField] private Color rustBrown = new Color(0.55f, 0.27f, 0.07f, 1f); // #8B4513

        private TextMeshPro textMesh;
        private IObjectPool<GameObject> pool;
        private float timer;
        private Vector3 velocity;
        private Vector3 currentScale;

        private void Awake()
        {
            textMesh = GetComponentInChildren<TextMeshPro>();
            currentScale = transform.localScale;
        }

        public void Initialize(string text, IObjectPool<GameObject> parentPool)
        {
            if (textMesh != null)
            {
                textMesh.text = text;
                textMesh.color = neonOrange; // Start with neon orange
            }
            
            pool = parentPool;
            timer = 0f;
            
            // Random direction fling (Metal fragment style)
            float randomX = Random.Range(-xDriftRange, xDriftRange);
            float randomY = Random.Range(initialVelocityMin, initialVelocityMax);
            velocity = new Vector3(randomX, randomY, 0);

            // Start scale pop
            transform.localScale = currentScale * startScale;
            
            gameObject.SetActive(true);
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            timer += dt;
            float progress = timer / duration;

            if (progress >= 1.0f)
            {
                pool.Release(gameObject);
                return;
            }

            // Physics movement
            transform.position += velocity * dt;
            velocity.y -= gravity * dt; // Simple gravity/deceleration effect

            // Scale progression
            float scaleEffect = scaleCurve.Evaluate(progress);
            transform.localScale = currentScale * (1.0f + (startScale - 1.0f) * scaleEffect);

            // Color and Alpha transition
            if (textMesh != null)
            {
                Color c = Color.Lerp(neonOrange, rustBrown, progress);
                c.a *= alphaCurve.Evaluate(progress);
                textMesh.color = c;
            }
        }
        public void SetToWarning()
        {
            if (textMesh != null)
            {
                textMesh.color = Color.red;
            }
        }
    }
}
