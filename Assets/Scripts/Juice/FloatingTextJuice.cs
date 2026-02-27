using UnityEngine;
using TMPro;
using UnityEngine.Pool;

namespace GalacticNexus.Scripts.Juice
{
    public class FloatingTextJuice : MonoBehaviour
    {
        [SerializeField] private float floatSpeed = 2f;
        [SerializeField] private float duration = 1.5f;
        [SerializeField] private AnimationCurve alphaCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);

        private TextMeshPro textMesh;
        private IObjectPool<GameObject> pool;
        private float timer;
        private Color initialColor;

        private void Awake()
        {
            textMesh = GetComponentInChildren<TextMeshPro>();
            if (textMesh != null) initialColor = textMesh.color;
        }

        public void Initialize(string text, IObjectPool<GameObject> parentPool)
        {
            if (textMesh != null)
            {
                textMesh.text = text;
                textMesh.color = initialColor;
            }
            
            pool = parentPool;
            timer = 0f;
            gameObject.SetActive(true);
        }

        private void Update()
        {
            timer += Time.deltaTime;
            float normalizedTime = timer / duration;

            // Yukarı doğru süzülme
            transform.position += Vector3.up * floatSpeed * Time.deltaTime;

            // Saydamlık (Alpha) değişimi
            if (textMesh != null)
            {
                Color c = textMesh.color;
                c.a = initialColor.a * alphaCurve.Evaluate(normalizedTime);
                textMesh.color = c;
            }

            if (timer >= duration)
            {
                pool.Release(gameObject);
            }
        }
    }
}
