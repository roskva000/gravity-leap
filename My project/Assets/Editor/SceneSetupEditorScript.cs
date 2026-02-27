using UnityEditor;
using UnityEngine;
using GalacticNexus.Scripts.Authoring;
using GalacticNexus.Scripts.UI;
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
            CreateSubScene();
            
            GameObject subSceneObj = GameObject.Find("ECS_World_SubScene");
            Transform parent = subSceneObj != null ? subSceneObj.transform : null;
            
            SetupInitialDrones(parent);

            CreateOrUpdateObject<GlobalEconomyAuthoring>("GlobalEconomy", parent);
            CreateOrUpdateObject<StationManagerAuthoring>("StationManager", parent);
            CreateOrUpdateObject<UIBridgeAuthoring>("UIBridge", null); // UI Root'ta kalsın
            
            var spawner = CreateOrUpdateObject<SpawnerAuthoring>("ShipSpawner", parent);
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
 
            Debug.Log("[Pas ve Neon] Sahne restorasyonu tamamlandı! ECS SubScene oluşturuldu ve başlangıç droneları eklendi.");
        }

        private static void CreateSubScene()
        {
            #if UNITY_2022_1_OR_NEWER
            string subSceneName = "ECS_World_SubScene";
            GameObject subSceneObj = GameObject.Find(subSceneName);
            
            if (subSceneObj == null)
            {
                subSceneObj = new GameObject(subSceneName);
                var subScene = subSceneObj.AddComponent<Unity.Scenes.SubScene>();
                
                // Sahne için bir asset oluşturmak gerekiyor
                if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
                    AssetDatabase.CreateFolder("Assets", "Scenes");
                
                string scenePath = "Assets/Scenes/" + subSceneName + "_Data.unity";
                
                // Eğer sahne yoksa oluştur
                if (AssetDatabase.LoadAssetAtPath<Object>(scenePath) == null)
                {
                    var newScene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene, UnityEditor.SceneManagement.NewSceneMode.Additive);
                    UnityEditor.SceneManagement.EditorSceneManager.SaveScene(newScene, scenePath);
                    // Yeni sahneyi kapat (SubScene zaten açacak)
                    UnityEditor.SceneManagement.EditorSceneManager.CloseScene(newScene, true);
                }

                // SubScene bileşenine ata
                var sceneAsset = AssetDatabase.LoadAssetAtPath<Object>(scenePath);
                var field = typeof(Unity.Scenes.SubScene).GetField("m_SceneAsset", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field != null) field.SetValue(subScene, sceneAsset);
                
                Debug.Log("[Pas ve Neon] SubScene altyapısı kuruldu ve SceneAsset bağlandı.");
            }
            #endif
        }

        private static void SetupInitialDrones(Transform parent = null)
        {
            GameObject droneGroup = GameObject.Find("Drones_Group");
            if (droneGroup == null)
            {
                droneGroup = new GameObject("Drones_Group");
                if (parent != null) droneGroup.transform.SetParent(parent);
                Undo.RegisterCreatedObjectUndo(droneGroup, "Create Drones_Group");
            }
            else if (parent != null && droneGroup.transform.parent != parent)
            {
                droneGroup.transform.SetParent(parent);
            }

            // Drone prefabı bul veya oluştur
            GameObject dronePrefab = null;
            string[] guids = AssetDatabase.FindAssets("Drone_Template t:Prefab");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                dronePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            }

            if (dronePrefab == null)
            {
                // Geçici küp oluştur ve prefab yap
                GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.name = "Drone_Template";
                cube.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
                cube.AddComponent<DroneAuthoring>();
                
                // Yeşil Neon Materyal
                var renderer = cube.GetComponent<MeshRenderer>();
                var mat = new Material(Shader.Find("Sprites/Default"));
                mat.color = new Color(0, 1, 0.5f, 1);
                renderer.sharedMaterial = mat;
                
                if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
                    AssetDatabase.CreateFolder("Assets", "Prefabs");
                
                string prefabPath = "Assets/Prefabs/Drone_Template.prefab";
                PrefabUtility.SaveAsPrefabAsset(cube, prefabPath);
                DestroyImmediate(cube);
                dronePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                Debug.Log("[Pas ve Neon] Drone_Template (Yeşil Neon) prefabı otomatik oluşturuldu.");
            }

            // 3 Başlangıç Drone'u ekle
            if (droneGroup.transform.childCount < 3)
            {
                for (int i = 0; i < 3; i++)
                {
                    GameObject drone = (GameObject)PrefabUtility.InstantiatePrefab(dronePrefab, droneGroup.transform);
                    drone.transform.position = new Vector3(-10 + (i * 5), 0, 0);
                    drone.name = $"Drone_{i+1}";
                }
            }
        }

        private static T CreateOrUpdateObject<T>(string name, Transform parent = null) where T : MonoBehaviour
        {
            GameObject obj = GameObject.Find(name);
            if (obj == null)
            {
                obj = new GameObject(name);
                if (parent != null) obj.transform.SetParent(parent);
                Undo.RegisterCreatedObjectUndo(obj, "Create " + name);
            }
            else if (parent != null && obj.transform.parent != parent)
            {
                obj.transform.SetParent(parent);
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
                
                var scaler = canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
                scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                
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
            uiBridge.ScrapText = CreateTextElement(canvas.transform, "Scrap_Text", "SCRAP: 0", new Vector2(-20, -20));
            uiBridge.GemsText = CreateTextElement(canvas.transform, "Neon_Text", "NEON: 0", new Vector2(-20, -80));
            uiBridge.ActiveShipsText = CreateTextElement(canvas.transform, "Ships_Text", "SERVICED: 0", new Vector2(-20, -140));

            // Scrap için Juice Ekle
            if (uiBridge.ScrapText != null)
            {
                var scrapJuice = uiBridge.ScrapText.GetComponent<UIJuiceController>();
                if (scrapJuice == null) scrapJuice = uiBridge.ScrapText.gameObject.AddComponent<UIJuiceController>();
                uiBridge.ScrapJuice = scrapJuice;
            }

            // Neon için Juice Ekle
            if (uiBridge.GemsText != null)
            {
                var neonJuice = uiBridge.GemsText.GetComponent<UIJuiceController>();
                if (neonJuice == null) neonJuice = uiBridge.GemsText.gameObject.AddComponent<UIJuiceController>();
                
                // Reflection ile prefix ayarla
                var field = typeof(UIJuiceController).GetField("prefix", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field != null) field.SetValue(neonJuice, "NEON: ");
                
                uiBridge.NeonJuice = neonJuice;
            }

            // 4.5 Upgrade Paneli
            CreateUpgradeUI(canvas.transform);
            
            EditorUtility.SetDirty(uiBridge);
        }

        private static void CreateUpgradeUI(Transform canvasParent)
        {
            GameObject panel = GameObject.Find("Upgrade_Panel");
            if (panel == null)
            {
                panel = new GameObject("Upgrade_Panel");
                panel.transform.SetParent(canvasParent);
                var rect = panel.AddComponent<RectTransform>();
                rect.anchorMin = new Vector2(0, 0);
                rect.anchorMax = new Vector2(1, 0);
                rect.pivot = new Vector2(0.5f, 0);
                rect.anchoredPosition = new Vector2(0, 50);
                rect.sizeDelta = new Vector2(0, 150);
                Undo.RegisterCreatedObjectUndo(panel, "Create Upgrade Panel");
            }

            var controller = panel.GetComponent<GalacticNexus.Scripts.UI.UIUpgradeController>();
            if (controller == null) controller = panel.AddComponent<GalacticNexus.Scripts.UI.UIUpgradeController>();

            controller.DroneSpeedButton = CreateButton(panel.transform, "Btn_DroneSpeed", "DRONE SPEED (100)", new Vector2(-150, 0));
            controller.DockCapacityButton = CreateButton(panel.transform, "Btn_DockCapacity", "NEW DOCK (500)", new Vector2(150, 0));
            
            // OnClick eventlerini bağla
            UnityEditor.Events.UnityEventTools.AddPersistentListener(controller.DroneSpeedButton.onClick, controller.RequestDroneSpeedUpgrade);
            UnityEditor.Events.UnityEventTools.AddPersistentListener(controller.DockCapacityButton.onClick, controller.RequestDockCapacityUpgrade);

            EditorUtility.SetDirty(controller);
        }

        private static UnityEngine.UI.Button CreateButton(Transform parent, string name, string label, Vector2 pos)
        {
            GameObject obj = GameObject.Find(name);
            if (obj == null)
            {
                // Default Unity Button template'i yoksa kodla yapıyoruz
                obj = new GameObject(name);
                obj.transform.SetParent(parent);
                var img = obj.AddComponent<UnityEngine.UI.Image>();
                img.color = new Color(0.1f, 0.1f, 0.15f, 0.8f); // Koyu fütüristik arka plan
                var btn = obj.AddComponent<UnityEngine.UI.Button>();
                
                // Neon çerçeve hissi (isteğe bağlı, şimdilik sadece renk)
                var outline = obj.AddComponent<UnityEngine.UI.Outline>();
                outline.effectColor = new Color(0f, 1f, 1f, 0.5f); // Turkuaz neon
                outline.effectDistance = new Vector2(2, -2);
                
                var rect = obj.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(250, 80);
                rect.anchoredPosition = pos;

                GameObject textObj = new GameObject("Text");
                textObj.transform.SetParent(obj.transform);
                var tmp = textObj.AddComponent<TMPro.TextMeshProUGUI>();
                tmp.text = label;
                tmp.fontSize = 24;
                tmp.alignment = TMPro.TextAlignmentOptions.Center;
                tmp.color = Color.black;
                var tRect = textObj.GetComponent<RectTransform>();
                tRect.anchorMin = Vector2.zero;
                tRect.anchorMax = Vector2.one;
                tRect.sizeDelta = Vector2.zero;

                Undo.RegisterCreatedObjectUndo(obj, "Create " + name);
                return btn;
            }
            return obj.GetComponent<UnityEngine.UI.Button>();
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
