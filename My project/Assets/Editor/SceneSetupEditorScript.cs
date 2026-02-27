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
            if (spawner != null)
            {
                spawner.transform.position = new Vector3(0, 10, 0);
                
                // Prefab ataması
                if (spawner.ShipPrefab == null)
                {
                    string[] guids = AssetDatabase.FindAssets("Ship_Template t:Prefab");
                    if (guids.Length > 0)
                    {
                        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                        spawner.ShipPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                        EditorUtility.SetDirty(spawner);
                        Debug.Log("[Pas ve Neon] ShipSpawner'a 'Ship_Template' prefab'ı atandı.");
                    }
                }
            }

            // 3. Rıhtım Grubu
            GameObject docksGroup = GameObject.Find("Docks_Group");
            if (docksGroup == null)
            {
                docksGroup = new GameObject("Docks_Group");
                Undo.RegisterCreatedObjectUndo(docksGroup, "Create Docks_Group");
            }

            CreateDock(docksGroup.transform, "Dock_Alpha", 1.2f, new Vector3(-5, 0, 0));
            CreateDock(docksGroup.transform, "Dock_Beta", 1.0f, new Vector3(5, 0, 0));

            // 4. UI ve Canvas Kurulumu
            CreateUIHierarchy();
 
            // 5. URP Kontrol
            CheckURP();
 
            Debug.Log("[Pas ve Neon] Sahne restorasyonu tamamlandı! UI hiyerarşisi oluşturuldu ve UIBridge referansları bağlandı.");
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
                // Prefab'dan oluşturmayı dene
                string[] guids = AssetDatabase.FindAssets("Dock_Template t:Prefab");
                if (guids.Length > 0)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    dockObj = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
                    dockObj.name = name;
                }
                else
                {
                    dockObj = new GameObject(name);
                    dockObj.transform.SetParent(parent);
                }
                Undo.RegisterCreatedObjectUndo(dockObj, "Create " + name);
            }
            dockObj.transform.position = pos;
            
            var authoring = dockObj.GetComponent<DockAuthoring>();
            if (authoring == null) authoring = dockObj.AddComponent<DockAuthoring>();
            authoring.ServiceMultiplier = multiplier;
        }

        private static void CreateUIHierarchy()
        {
            // Canvas Kontrol
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObj = new GameObject("GameHUD_Canvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
                canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
                Undo.RegisterCreatedObjectUndo(canvasObj, "Create Canvas");
            }

            // EventSystem Kontrol (Yeni Input System Uyumlu)
            var eventSystem = Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>();
            if (eventSystem == null)
            {
                GameObject esObj = new GameObject("EventSystem");
                eventSystem = esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
                Undo.RegisterCreatedObjectUndo(esObj, "Create EventSystem");
            }

            // StandaloneInputModule yerine InputSystemUIInputModule kontrolü
            var oldModule = eventSystem.GetComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            if (oldModule != null)
            {
                // Eğer proje yeni Input System kullanıyorsa StandaloneInputModule hata verir
                // Otomatik olarak InputSystemUIInputModule eklemeye çalışalım
                #if ENABLE_INPUT_SYSTEM
                DestroyImmediate(oldModule);
                if (eventSystem.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>() == null)
                {
                    eventSystem.gameObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
                }
                #endif
            }
            else if (eventSystem.GetComponent<UnityEngine.EventSystems.BaseInputModule>() == null)
            {
                #if ENABLE_INPUT_SYSTEM
                eventSystem.gameObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
                #else
                eventSystem.gameObject.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                #endif
            }

            // UIBridge referanslarını al
            var uiBridge = CreateOrUpdateObject<UIBridgeAuthoring>("UIBridge");

            // Text Elemanlarını Oluştur
            uiBridge.ScrapText = CreateTextElement(canvas.transform, "Scrap_Text", "SCRAP: 0", new Vector2(-200, -50));
            uiBridge.GemsText = CreateTextElement(canvas.transform, "Neon_Text", "NEON: 0", new Vector2(-200, -100));
            uiBridge.ActiveShipsText = CreateTextElement(canvas.transform, "Ships_Text", "SERVICED: 0", new Vector2(-200, -150));
            
            EditorUtility.SetDirty(uiBridge);
        }

        private static TMPro.TextMeshProUGUI CreateTextElement(Transform parent, string name, string initialText, Vector2 pos)
        {
            GameObject obj = GameObject.Find(name);
            if (obj == null)
            {
                obj = new GameObject(name);
                obj.transform.SetParent(parent);
                Undo.RegisterCreatedObjectUndo(obj, "Create " + name);
            }

            var tmp = obj.GetComponent<TMPro.TextMeshProUGUI>();
            if (tmp == null) tmp = obj.AddComponent<TMPro.TextMeshProUGUI>();

            tmp.text = initialText;
            tmp.fontSize = 36;
            tmp.alignment = TMPro.TextAlignmentOptions.Left;
            
            var rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(1, 1);
            rect.anchoredPosition = pos;
            rect.sizeDelta = new Vector2(400, 50);

            return tmp;
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
