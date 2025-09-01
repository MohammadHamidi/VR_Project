using System.IO;
using UnityEngine;
using UnityEditor;

namespace VRRehab.EditorTools
{
    public class VRRehabMaterialCreator : EditorWindow
    {
        private const string MATERIALS_PATH = "Assets/Materials";

        // Material creation options
        private string materialName = "New_VRRehab_Material";
        private Color materialColor = Color.white;
        private float metallic = 0f;
        private float smoothness = 0.5f;
        private MaterialType selectedType = MaterialType.Standard;

        // Preset materials
        private string[] presetNames = {
            "Ball_Red", "Ball_Blue", "Ball_Green", "Ball_Yellow",
            "Ring_Gold", "Ring_Silver", "Ring_Bronze",
            "Ground_Grass", "Ground_Concrete", "Ground_Wood",
            "UI_Panel_SemiTransparent", "UI_Button_Normal"
        };

        private enum MaterialType
        {
            Standard,
            Transparent,
            UI_Default
        }

        [MenuItem("VR Rehab/Material Creator", false, 40)]
        public static void ShowWindow()
        {
            VRRehabMaterialCreator window = GetWindow<VRRehabMaterialCreator>();
            window.titleContent = new GUIContent("VR Rehab Material Creator");
            window.minSize = new Vector2(350, 400);
        }

        [MenuItem("VR Rehab/Create Preset Materials", false, 41)]
        public static void CreateAllPresetMaterials()
        {
            CreateBallMaterials();
            CreateRingMaterials();
            CreateGroundMaterials();
            CreateUIMaterials();

            AssetDatabase.Refresh();
            Debug.Log("All VR Rehab preset materials created!");
        }

        void OnGUI()
        {
            GUILayout.Label("VR Rehab Material Creator", EditorStyles.boldLabel);
            GUILayout.Space(10);

            // Custom Material Creation
            GUILayout.Label("Custom Material", EditorStyles.boldLabel);
            materialName = EditorGUILayout.TextField("Material Name", materialName);
            selectedType = (MaterialType)EditorGUILayout.EnumPopup("Material Type", selectedType);
            materialColor = EditorGUILayout.ColorField("Color", materialColor);

            if (selectedType == MaterialType.Standard)
            {
                metallic = EditorGUILayout.Slider("Metallic", metallic, 0f, 1f);
                smoothness = EditorGUILayout.Slider("Smoothness", smoothness, 0f, 1f);
            }

            if (GUILayout.Button("Create Custom Material"))
            {
                CreateCustomMaterial();
            }

            GUILayout.Space(20);

            // Preset Materials
            GUILayout.Label("Preset Materials", EditorStyles.boldLabel);
            if (GUILayout.Button("Create Ball Materials (Red, Blue, Green, Yellow)"))
            {
                CreateBallMaterials();
            }

            if (GUILayout.Button("Create Ring Materials (Gold, Silver, Bronze)"))
            {
                CreateRingMaterials();
            }

            if (GUILayout.Button("Create Ground Materials"))
            {
                CreateGroundMaterials();
            }

            if (GUILayout.Button("Create UI Materials"))
            {
                CreateUIMaterials();
            }

            if (GUILayout.Button("Create All Preset Materials"))
            {
                CreateAllPresetMaterials();
            }

            GUILayout.Space(20);

            // Texture Creation
            GUILayout.Label("Texture Creation", EditorStyles.boldLabel);
            if (GUILayout.Button("Create Procedural Textures"))
            {
                CreateProceduralTextures();
            }

            if (GUILayout.Button("Create Checkerboard Texture"))
            {
                CreateCheckerboardTexture();
            }

            GUILayout.Space(20);

            // Utilities
            GUILayout.Label("Utilities", EditorStyles.boldLabel);
            if (GUILayout.Button("Organize Materials by Type"))
            {
                OrganizeMaterials();
            }

            if (GUILayout.Button("Validate Material Assignments"))
            {
                ValidateMaterialAssignments();
            }
        }

        void CreateCustomMaterial()
        {
            EnsureDirectoryExists(MATERIALS_PATH);

            Material newMaterial;

            switch (selectedType)
            {
                case MaterialType.Standard:
                    newMaterial = new Material(Shader.Find("Standard"));
                    newMaterial.color = materialColor;
                    newMaterial.SetFloat("_Metallic", metallic);
                    newMaterial.SetFloat("_Glossiness", smoothness);
                    break;

                case MaterialType.Transparent:
                    newMaterial = new Material(Shader.Find("Standard"));
                    newMaterial.color = materialColor;
                    newMaterial.SetFloat("_Mode", 3); // Transparent
                    newMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    newMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    newMaterial.SetInt("_ZWrite", 0);
                    newMaterial.DisableKeyword("_ALPHATEST_ON");
                    newMaterial.EnableKeyword("_ALPHABLEND_ON");
                    newMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    newMaterial.renderQueue = 3000;
                    break;

                case MaterialType.UI_Default:
                    newMaterial = new Material(Shader.Find("UI/Default"));
                    newMaterial.color = materialColor;
                    break;

                default:
                    newMaterial = new Material(Shader.Find("Standard"));
                    newMaterial.color = materialColor;
                    break;
            }

            string materialPath = $"{MATERIALS_PATH}/{materialName}.mat";
            AssetDatabase.CreateAsset(newMaterial, materialPath);
            AssetDatabase.Refresh();

            Debug.Log($"Custom material created: {materialPath}");

            // Select the new material in the project window
            Selection.activeObject = newMaterial;
        }

        static void CreateBallMaterials()
        {
            EnsureDirectoryExists($"{MATERIALS_PATH}/Balls");

            Color[] ballColors = { Color.red, Color.blue, Color.green, Color.yellow };
            string[] ballNames = { "Ball_Red", "Ball_Blue", "Ball_Green", "Ball_Yellow" };

            for (int i = 0; i < ballColors.Length; i++)
            {
                Material ballMaterial = new Material(Shader.Find("Standard"));
                ballMaterial.color = ballColors[i];
                ballMaterial.SetFloat("_Metallic", 0f);
                ballMaterial.SetFloat("_Glossiness", 0.5f);

                string materialPath = $"{MATERIALS_PATH}/Balls/{ballNames[i]}.mat";
                AssetDatabase.CreateAsset(ballMaterial, materialPath);
            }

            Debug.Log("Ball materials created");
        }

        static void CreateRingMaterials()
        {
            EnsureDirectoryExists($"{MATERIALS_PATH}/Rings");

            // Gold
            Material goldMaterial = new Material(Shader.Find("Standard"));
            goldMaterial.color = new Color(1f, 0.8f, 0f);
            goldMaterial.SetFloat("_Metallic", 0.8f);
            goldMaterial.SetFloat("_Glossiness", 0.8f);
            AssetDatabase.CreateAsset(goldMaterial, $"{MATERIALS_PATH}/Rings/Ring_Gold.mat");

            // Silver
            Material silverMaterial = new Material(Shader.Find("Standard"));
            silverMaterial.color = new Color(0.8f, 0.8f, 0.8f);
            silverMaterial.SetFloat("_Metallic", 0.9f);
            silverMaterial.SetFloat("_Glossiness", 0.9f);
            AssetDatabase.CreateAsset(silverMaterial, $"{MATERIALS_PATH}/Rings/Ring_Silver.mat");

            // Bronze
            Material bronzeMaterial = new Material(Shader.Find("Standard"));
            bronzeMaterial.color = new Color(0.8f, 0.5f, 0.2f);
            bronzeMaterial.SetFloat("_Metallic", 0.6f);
            bronzeMaterial.SetFloat("_Glossiness", 0.4f);
            AssetDatabase.CreateAsset(bronzeMaterial, $"{MATERIALS_PATH}/Rings/Ring_Bronze.mat");

            Debug.Log("Ring materials created");
        }

        static void CreateGroundMaterials()
        {
            EnsureDirectoryExists($"{MATERIALS_PATH}/Ground");

            // Grass
            Material grassMaterial = new Material(Shader.Find("Standard"));
            grassMaterial.color = new Color(0.2f, 0.6f, 0.2f);
            grassMaterial.SetFloat("_Metallic", 0f);
            grassMaterial.SetFloat("_Glossiness", 0.1f);
            AssetDatabase.CreateAsset(grassMaterial, $"{MATERIALS_PATH}/Ground/Ground_Grass.mat");

            // Concrete
            Material concreteMaterial = new Material(Shader.Find("Standard"));
            concreteMaterial.color = new Color(0.7f, 0.7f, 0.7f);
            concreteMaterial.SetFloat("_Metallic", 0f);
            concreteMaterial.SetFloat("_Glossiness", 0.2f);
            AssetDatabase.CreateAsset(concreteMaterial, $"{MATERIALS_PATH}/Ground/Ground_Concrete.mat");

            // Wood
            Material woodMaterial = new Material(Shader.Find("Standard"));
            woodMaterial.color = new Color(0.6f, 0.4f, 0.2f);
            woodMaterial.SetFloat("_Metallic", 0f);
            woodMaterial.SetFloat("_Glossiness", 0.3f);
            AssetDatabase.CreateAsset(woodMaterial, $"{MATERIALS_PATH}/Ground/Ground_Wood.mat");

            Debug.Log("Ground materials created");
        }

        static void CreateUIMaterials()
        {
            EnsureDirectoryExists($"{MATERIALS_PATH}/UI");

            // Semi-transparent panel
            Material panelMaterial = new Material(Shader.Find("UI/Default"));
            panelMaterial.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);
            AssetDatabase.CreateAsset(panelMaterial, $"{MATERIALS_PATH}/UI/UI_Panel_SemiTransparent.mat");

            // Button normal
            Material buttonMaterial = new Material(Shader.Find("UI/Default"));
            buttonMaterial.color = new Color(0.3f, 0.3f, 0.3f, 1f);
            AssetDatabase.CreateAsset(buttonMaterial, $"{MATERIALS_PATH}/UI/UI_Button_Normal.mat");

            Debug.Log("UI materials created");
        }

        static void CreateProceduralTextures()
        {
            EnsureDirectoryExists("Assets/Textures/Procedural");

            // Create a simple gradient texture
            Texture2D gradientTexture = new Texture2D(256, 256);
            for (int x = 0; x < 256; x++)
            {
                for (int y = 0; y < 256; y++)
                {
                    float gradient = (float)y / 256f;
                    Color color = new Color(gradient, gradient, gradient);
                    gradientTexture.SetPixel(x, y, color);
                }
            }
            gradientTexture.Apply();

            string texturePath = "Assets/Textures/Procedural/Gradient_256x256.png";
            File.WriteAllBytes(texturePath, gradientTexture.EncodeToPNG());
            AssetDatabase.Refresh();

            Debug.Log("Procedural gradient texture created");
        }

        static void CreateCheckerboardTexture()
        {
            EnsureDirectoryExists("Assets/Textures/Procedural");

            Texture2D checkerTexture = new Texture2D(64, 64);
            for (int x = 0; x < 64; x++)
            {
                for (int y = 0; y < 64; y++)
                {
                    bool isBlack = ((x / 8) + (y / 8)) % 2 == 0;
                    Color color = isBlack ? Color.black : Color.white;
                    checkerTexture.SetPixel(x, y, color);
                }
            }
            checkerTexture.Apply();

            string texturePath = "Assets/Textures/Procedural/Checkerboard_64x64.png";
            File.WriteAllBytes(texturePath, checkerTexture.EncodeToPNG());
            AssetDatabase.Refresh();

            Debug.Log("Checkerboard texture created");
        }

        public static void OrganizeMaterials()
        {
            EnsureDirectoryExists($"{MATERIALS_PATH}/Balls");
            EnsureDirectoryExists($"{MATERIALS_PATH}/Rings");
            EnsureDirectoryExists($"{MATERIALS_PATH}/Ground");
            EnsureDirectoryExists($"{MATERIALS_PATH}/UI");

            // Move existing materials to appropriate folders
            string[] allMaterials = AssetDatabase.FindAssets("t:Material", new[] { MATERIALS_PATH });
            foreach (string guid in allMaterials)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string fileName = Path.GetFileNameWithoutExtension(path);

                string newPath = path;
                if (fileName.Contains("Ball_"))
                {
                    newPath = $"{MATERIALS_PATH}/Balls/{Path.GetFileName(path)}";
                }
                else if (fileName.Contains("Ring_"))
                {
                    newPath = $"{MATERIALS_PATH}/Rings/{Path.GetFileName(path)}";
                }
                else if (fileName.Contains("Ground_"))
                {
                    newPath = $"{MATERIALS_PATH}/Ground/{Path.GetFileName(path)}";
                }
                else if (fileName.Contains("UI_"))
                {
                    newPath = $"{MATERIALS_PATH}/UI/{Path.GetFileName(path)}";
                }

                if (newPath != path)
                {
                    AssetDatabase.MoveAsset(path, newPath);
                }
            }

            AssetDatabase.Refresh();
            Debug.Log("Materials organized by type");
        }

        static void ValidateMaterialAssignments()
        {
            // Find all renderers in the scene
            Renderer[] renderers = FindObjectsOfType<Renderer>();

            int missingMaterials = 0;
            int totalRenderers = 0;

            foreach (Renderer renderer in renderers)
            {
                totalRenderers++;
                if (renderer.sharedMaterial == null)
                {
                    missingMaterials++;
                    Debug.LogWarning($"Missing material on: {renderer.gameObject.name}");
                }
            }

            if (missingMaterials == 0)
            {
                Debug.Log($"✅ All {totalRenderers} renderers have materials assigned");
            }
            else
            {
                Debug.LogWarning($"⚠️ {missingMaterials} out of {totalRenderers} renderers are missing materials");
            }
        }

        static void EnsureDirectoryExists(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }
    }
}
