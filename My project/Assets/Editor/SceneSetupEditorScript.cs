using UnityEditor;
using UnityEngine;
using GalacticNexus.Scripts.Authoring;
using UnityEngine.Rendering.Universal;

namespace GalacticNexus.Scripts.Editor
{
    public class SceneSetupEditorScript : EditorWindow
    {
        [MenuItem("Tools/Pas ve Neon/Sahneyi Kur")]
        public static void SetupScene()
        {
            // 1. Kamera Ayarları (Uzay Boşluğu / Koyu Gri)
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                Undo.RecordObject(mainCam, "Update Camera for Pas ve Neon");
                mainCam.clearFlags = CameraClearFlags.SolidColor;
                if (ColorUtility.TryParseHtmlString("#0A0A0C", out Color deepSpace))
                {
                    mainCam.backgroundColor = deepSpace;
                }
                Debug.Log("[Pas ve Neon] Kamera 'Uzay Boşluğu' rengine (#0A0A0C) güncellendi.");
            }

            // 2. ECS Authoring Objeleri
            CreateOrUpdateObject<GlobalEconomyAuthoring>("GlobalEconomy");
            CreateOrUpdateObject<StationManagerAuthoring>("StationManager");
            CreateOrUpdateObject<UIBridgeAuthoring>("UIBridge");
            
            var spawner = CreateOrUpdateObject<SpawnerAuthoring>("ShipSpawner");
            if (spawner != null) spawner.transform.position = new Vector3(0, 10, 0);

            // 3. Rıhtım Grubu
            GameObject docksGroup = GameObject.Find("Docks_Group");
            if (docksGroup == null)
            {
                docksGroup = new GameObject("Docks_Group");
                Undo.RegisterCreatedObjectUndo(docksGroup, "Create Docks_Group");
            }

            CreateDock(docksGroup.transform, "Dock_Alpha", 1.2f, new Vector3(-5, 0, 0));
            CreateDock(docksGroup.transform, "Dock_Beta", 1.0f, new Vector3(5, 0, 0));

            // 4. URP Kontrol
            CheckURP();

            Debug.Log("[Pas ve Neon] Sahne restorasyonu tamamlandı. Lütfen Inspector üzerinden Prefab ve UI referanslarını (UIBridge) bağlamayı unutmayın!");
        }

        private static T CreateOrUpdateObject<T>(string name) where T : MonoBehaviour
        {
            GameObject obj = GameObject.Find(name);
            if (obj == null)
            {
                obj = new GameObject(name);
                Undo.RegisterCreatedObjectUndo(obj, "Create " + name);
            }
            
            T component = obj.GetComponent<T>();
            if (component == null)
            {
                component = obj.AddComponent<T>();
            }
            return component;
        }

        private static void CreateDock(Transform parent, string name, float multiplier, Vector3 pos)
        {
            GameObject dockObj = GameObject.Find(name);
            if (dockObj == null)
            {
                dockObj = new GameObject(name);
                dockObj.transform.SetParent(parent);
                Undo.RegisterCreatedObjectUndo(dockObj, "Create " + name);
            }
            dockObj.transform.position = pos;
            
            var authoring = dockObj.GetComponent<DockAuthoring>();
            if (authoring == null) authoring = dockObj.AddComponent<DockAuthoring>();
            authoring.ServiceMultiplier = multiplier;
        }

        private static void CheckURP()
        {
            var pipeline = UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline;
            if (pipeline == null)
            {
                Debug.LogWarning("[Pas ve Neon] URP Asset'i 'Project Settings > Graphics' kısmında eksik olabilir!");
            }
            
            if (Object.FindFirstObjectByType<Light2D>() == null)
            {
                Debug.LogWarning("[Pas ve Neon] Sahnede 'Global Light 2D' bulunamadı. Görseller karanlık kalabilir.");
            }
        }
    }
}
