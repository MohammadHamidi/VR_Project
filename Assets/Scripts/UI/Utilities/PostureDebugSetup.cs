using UnityEngine;
using UI.Utilities;

namespace UI.Utilities
{
    /// <summary>
    /// Simple setup script to automatically create and configure the PostureDebugUI
    /// Attach this to any GameObject in your scene to automatically set up the debug UI
    /// </summary>
    public class PostureDebugSetup : MonoBehaviour
    {
        [Header("Auto Setup")]
        [SerializeField] private bool autoCreateOnStart = true;
        [SerializeField] private bool destroyAfterSetup = true;
        
        [Header("UI Settings")]
        [SerializeField] private bool showInVR = true;
        [SerializeField] private bool showInEditor = true;
        [SerializeField] private float updateInterval = 0.1f;
        [SerializeField] private Vector3 vrOffset = new Vector3(0, 0, 12f);
        
        [Header("Debug")]
        [SerializeField] private bool enableKeyboardShortcuts = true;
        
        private PostureDebugUI debugUI;
        
        void Start()
        {
            if (autoCreateOnStart)
            {
                CreateDebugUI();
            }
        }
        
        [ContextMenu("Create Debug UI")]
        public void CreateDebugUI()
        {
            if (debugUI != null)
            {
                Debug.LogWarning("PostureDebugUI already exists!");
                return;
            }
            
            // Create the debug UI GameObject
            GameObject debugUIGO = new GameObject("PostureDebugUI");
            debugUI = debugUIGO.AddComponent<PostureDebugUI>();
            
            // Configure settings
            debugUI.SetUpdateInterval(updateInterval);
            debugUI.SetVisibility(ShouldShowUI());
            
            Debug.Log("PostureDebugUI created and configured successfully!");
            
            if (destroyAfterSetup)
            {
                // Destroy this setup script after creating the UI
                Destroy(this);
            }
        }
        
        private bool ShouldShowUI()
        {
            #if UNITY_EDITOR
            return showInEditor;
            #else
            return showInVR;
            #endif
        }
        
        void Update()
        {
            if (!enableKeyboardShortcuts) return;
            
            // Keyboard shortcuts for quick testing
            if (Input.GetKeyDown(KeyCode.F1))
            {
                if (debugUI != null)
                {
                    debugUI.ToggleVisibility();
                }
            }
            
            if (Input.GetKeyDown(KeyCode.F2))
            {
                if (debugUI != null)
                {
                    debugUI.ShowDebugInfo();
                }
            }
            
            if (Input.GetKeyDown(KeyCode.F3))
            {
                if (debugUI != null)
                {
                    debugUI.ForceUpdate();
                }
            }
        }
        
        // Public API
        public PostureDebugUI GetDebugUI()
        {
            return debugUI;
        }
        
        public void DestroyDebugUI()
        {
            if (debugUI != null)
            {
                Destroy(debugUI.gameObject);
                debugUI = null;
            }
        }
    }
}
