using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CombatSystem.Player;

namespace UI.Utilities
{
    public class PostureDebugUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Canvas debugCanvas;
        [SerializeField] private GameObject debugPanel;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private TextMeshProUGUI depthText;
        [SerializeField] private TextMeshProUGUI validationText;
        [SerializeField] private TextMeshProUGUI threatText;
        [SerializeField] private TextMeshProUGUI controllerText;
        [SerializeField] private TextMeshProUGUI bodyPoseText;
        [SerializeField] private TextMeshProUGUI headPositionText;  // NEW: Head position display
        [SerializeField] private TextMeshProUGUI headMovementText;  // NEW: Head movement display
        
        [Header("Visual Indicators")]
        [SerializeField] private Image depthBar;
        [SerializeField] private Image validationStatusIcon;
        [SerializeField] private Image threatIndicator;
        [SerializeField] private Image dodgeStatusIcon;
        [SerializeField] private Image headMovementIndicator;  // NEW: Head movement visual indicator
        
        [Header("Colors")]
        [SerializeField] private Color validColor = Color.green;
        [SerializeField] private Color invalidColor = Color.red;
        [SerializeField] private Color warningColor = Color.yellow;
        [SerializeField] private Color neutralColor = Color.white;
        
        [Header("Settings")]
        [SerializeField] private bool showInVR = true;
        [SerializeField] private bool showInEditor = true;
        [SerializeField] private float updateInterval = 0.1f;
        [SerializeField] private bool autoPosition = true;
        [SerializeField] private Vector3 vrOffset = new Vector3(0, 0, 12f);
        
        [Header("UI Positioning")]
        [SerializeField] private float distanceFromCamera = 12.0f;  // Distance from camera in meters
        
        private SquatDodge squatDodge;
        private Camera xrCamera;
        private float lastUpdateTime;
        private bool isInitialized = false;
        
        // UI State
        private bool isVisible = true;
        private Coroutine updateCoroutine;
        
        void Start()
        {
            InitializeUI();
            SetupVRPositioning();
            StartUpdateCoroutine();
        }
        
        void OnDestroy()
        {
            if (updateCoroutine != null)
            {
                StopCoroutine(updateCoroutine);
            }
        }
        
        private void InitializeUI()
        {
            // Find SquatDodge instance
            squatDodge = SquatDodge.Instance;
            if (squatDodge == null)
            {
                Debug.LogError("PostureDebugUI: SquatDodge instance not found!");
                return;
            }
            
            // Find XR Camera - improved detection
            xrCamera = FindXRCamera();
            if (xrCamera == null)
            {
                Debug.LogError("PostureDebugUI: No suitable camera found!");
                return;
            }
            
            // Create UI elements if not assigned
            if (debugCanvas == null)
            {
                CreateDebugCanvas();
            }
            
            if (debugPanel == null)
            {
                CreateDebugPanel();
            }
            
            // Setup initial visibility
            SetVisibility(ShouldShowUI());
            
            isInitialized = true;
            Debug.Log("PostureDebugUI initialized successfully");
        }
        
        private void CreateDebugCanvas()
        {
            GameObject canvasGO = new GameObject("PostureDebugCanvas");
            debugCanvas = canvasGO.AddComponent<Canvas>();
            debugCanvas.renderMode = RenderMode.WorldSpace;
            debugCanvas.sortingOrder = 100;
            
            // Assign the camera to the canvas
            debugCanvas.worldCamera = xrCamera;
            
            // Set scale to 0.01 for VR
            canvasGO.transform.localScale = new Vector3(-1,1,1) * 0.007f;
            
            // Add CanvasScaler for proper scaling
            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 10f;
            scaler.referencePixelsPerUnit = 100f;
            
            // Add GraphicRaycaster
            canvasGO.AddComponent<GraphicRaycaster>();
        }
        
        // NEW: Improved camera detection
        private Camera FindXRCamera()
        {
            // Try to find XR Origin camera first
            var xrOrigin = FindObjectOfType<Unity.XR.CoreUtils.XROrigin>();
            if (xrOrigin != null && xrOrigin.Camera != null)
            {
                Debug.Log("PostureDebugUI: Found XR Origin camera");
                return xrOrigin.Camera;
            }
            
            // Try to find camera with XR tag
            var xrCameras = FindObjectsOfType<Camera>();
            foreach (var cam in xrCameras)
            {
                if (cam.CompareTag("MainCamera") || cam.name.ToLower().Contains("xr") || cam.name.ToLower().Contains("vr"))
                {
                    Debug.Log($"PostureDebugUI: Found XR camera: {cam.name}");
                    return cam;
                }
            }
            
            // Fallback to main camera
            if (Camera.main != null)
            {
                Debug.Log("PostureDebugUI: Using main camera as fallback");
                return Camera.main;
            }
            
            // Last resort - any camera
            var anyCamera = FindObjectOfType<Camera>();
            if (anyCamera != null)
            {
                Debug.Log($"PostureDebugUI: Using any available camera: {anyCamera.name}");
                return anyCamera;
            }
            
            return null;
        }
        
        private void CreateDebugPanel()
        {
            // Create main panel
            GameObject panelGO = new GameObject("DebugPanel");
            panelGO.transform.SetParent(debugCanvas.transform, false);
            debugPanel = panelGO;
            
            // Add background
            var bgImage = panelGO.AddComponent<Image>();
            bgImage.color = new Color(0, 0, 0, 0.8f);
            
            // Add RectTransform
            var rectTransform = panelGO.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(400, 700);  // Increased height for new elements
            rectTransform.anchoredPosition = Vector2.zero;
            
            // Create text elements
            CreateTextElement("StatusText", "STATUS", 0, 320, 20);
            CreateTextElement("DepthText", "DEPTH", 0, 280, 16);
            CreateTextElement("HeadPositionText", "HEAD POSITION", 0, 240, 14);  // NEW
            CreateTextElement("HeadMovementText", "HEAD MOVEMENT", 0, 200, 14);  // NEW
            CreateTextElement("ValidationText", "VALIDATION", 0, 160, 14);
            CreateTextElement("ThreatText", "THREATS", 0, 120, 14);
            CreateTextElement("ControllerText", "CONTROLLERS", 0, 80, 12);
            CreateTextElement("BodyPoseText", "BODY POSE", 0, 40, 12);
            
            // Create visual indicators
            CreateDepthBar();
            CreateStatusIcons();
            
            // Get references
            statusText = panelGO.transform.Find("StatusText").GetComponent<TextMeshProUGUI>();
            depthText = panelGO.transform.Find("DepthText").GetComponent<TextMeshProUGUI>();
            headPositionText = panelGO.transform.Find("HeadPositionText").GetComponent<TextMeshProUGUI>();  // NEW
            headMovementText = panelGO.transform.Find("HeadMovementText").GetComponent<TextMeshProUGUI>();  // NEW
            validationText = panelGO.transform.Find("ValidationText").GetComponent<TextMeshProUGUI>();
            threatText = panelGO.transform.Find("ThreatText").GetComponent<TextMeshProUGUI>();
            controllerText = panelGO.transform.Find("ControllerText").GetComponent<TextMeshProUGUI>();
            bodyPoseText = panelGO.transform.Find("BodyPoseText").GetComponent<TextMeshProUGUI>();
        }
        
        private void CreateTextElement(string name, string initialText, float x, float y, int fontSize)
        {
            GameObject textGO = new GameObject(name);
            textGO.transform.SetParent(debugPanel.transform, false);
            
            var text = textGO.AddComponent<TextMeshProUGUI>();
            text.text = initialText;
            text.fontSize = fontSize;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Center;
            
            var rectTransform = textGO.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(380, 30);
            rectTransform.anchoredPosition = new Vector2(x, y);
        }
        
        private void CreateDepthBar()
        {
            GameObject barGO = new GameObject("DepthBar");
            barGO.transform.SetParent(debugPanel.transform, false);
            
            depthBar = barGO.AddComponent<Image>();
            depthBar.color = Color.cyan;
            
            var rectTransform = barGO.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(300, 20);
            rectTransform.anchoredPosition = new Vector2(0, 260);
        }
        
        private void CreateStatusIcons()
        {
            // Validation Status Icon
            GameObject validationGO = new GameObject("ValidationIcon");
            validationGO.transform.SetParent(debugPanel.transform, false);
            validationStatusIcon = validationGO.AddComponent<Image>();
            validationStatusIcon.color = neutralColor;
            
            var validationRect = validationGO.GetComponent<RectTransform>();
            validationRect.sizeDelta = new Vector2(30, 30);
            validationRect.anchoredPosition = new Vector2(-150, 160);
            
            // Threat Indicator
            GameObject threatGO = new GameObject("ThreatIcon");
            threatGO.transform.SetParent(debugPanel.transform, false);
            threatIndicator = threatGO.AddComponent<Image>();
            threatIndicator.color = neutralColor;
            
            var threatRect = threatGO.GetComponent<RectTransform>();
            threatRect.sizeDelta = new Vector2(30, 30);
            threatRect.anchoredPosition = new Vector2(-150, 120);
            
            // Dodge Status Icon
            GameObject dodgeGO = new GameObject("DodgeIcon");
            dodgeGO.transform.SetParent(debugPanel.transform, false);
            dodgeStatusIcon = dodgeGO.AddComponent<Image>();
            dodgeStatusIcon.color = neutralColor;
            
            var dodgeRect = dodgeGO.GetComponent<RectTransform>();
            dodgeRect.sizeDelta = new Vector2(30, 30);
            dodgeRect.anchoredPosition = new Vector2(-150, 80);
            
            // Head Movement Indicator
            GameObject headMovementGO = new GameObject("HeadMovementIcon");
            headMovementGO.transform.SetParent(debugPanel.transform, false);
            headMovementIndicator = headMovementGO.AddComponent<Image>();
            headMovementIndicator.color = neutralColor;
            
            var headMovementRect = headMovementGO.GetComponent<RectTransform>();
            headMovementRect.sizeDelta = new Vector2(30, 30);
            headMovementRect.anchoredPosition = new Vector2(-150, 40);
        }
        
        private void SetupVRPositioning()
        {
            if (xrCamera != null && autoPosition)
            {
                // Use ViewportToWorldPoint like BillboardUI for proper VR positioning
                Vector3 viewportPos = new Vector3(0.5f, 0.5f, distanceFromCamera);
                debugCanvas.transform.position = xrCamera.ViewportToWorldPoint(viewportPos);
                
                // Face the camera (like BillboardUI)
                Vector3 dir = (xrCamera.transform.position - debugCanvas.transform.position).normalized;
                dir.y = 0; // ignore vertical component for horizontal facing
                if (dir.sqrMagnitude > 0.001f)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(dir);
                    debugCanvas.transform.rotation = targetRotation;
                }
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
        
        private void StartUpdateCoroutine()
        {
            if (updateCoroutine != null)
            {
                StopCoroutine(updateCoroutine);
            }
            updateCoroutine = StartCoroutine(UpdateUI());
        }
        
        private IEnumerator UpdateUI()
        {
            while (true)
            {
                if (isInitialized && isVisible && squatDodge != null)
                {
                    UpdateAllUI();
                }
                yield return new WaitForSeconds(updateInterval);
            }
        }
        
        private void UpdateAllUI()
        {
            UpdateStatusText();
            UpdateDepthDisplay();
            UpdateHeadPositionDisplay();  // NEW
            UpdateHeadMovementDisplay();  // NEW
            UpdateValidationDisplay();
            UpdateThreatDisplay();
            UpdateControllerDisplay();
            UpdateControllerMovementDisplay(); // NEW: Controller movement display
            UpdateBodyPoseDisplay();
            UpdateVisualIndicators();
            
            // Update UI positioning if auto-positioning is enabled
            if (autoPosition && xrCamera != null)
            {
                UpdateUIPositioning();
            }
        }
        
        private void UpdateStatusText()
        {
            if (statusText == null) return;
            
            string status = "READY";
            Color statusColor = neutralColor;
            
            if (squatDodge.IsDodging)
            {
                status = "DODGING";
                statusColor = validColor;
            }
            else if (squatDodge.IsOnCooldown)
            {
                status = "COOLDOWN";
                statusColor = warningColor;
            }
            else if (squatDodge.IsSimulatingSquat())
            {
                status = "SIMULATING";
                statusColor = warningColor;
            }
            else if (squatDodge.CurrentDepthNorm >= 0.6f)
            {
                status = "SQUATTING";
                statusColor = validColor;
            }
            
            statusText.text = $"STATUS: {status}";
            statusText.color = statusColor;
        }
        
        private void UpdateDepthDisplay()
        {
            if (depthText == null || depthBar == null) return;
            
            float depthNorm = squatDodge.CurrentDepthNorm;
            float depth = squatDodge.CurrentSquatDepth;
            
            depthText.text = $"DEPTH: {depth:F3}m ({depthNorm:P0})";
            
            // Update depth bar
            depthBar.fillAmount = depthNorm;
            depthBar.color = Color.Lerp(Color.red, Color.green, depthNorm);
        }
        
        // NEW: Display detailed head position information
        private void UpdateHeadPositionDisplay()
        {
            if (headPositionText == null) return;
            
            Vector3 headPos = squatDodge.CameraPosition;
            float baselineHeight = squatDodge.BaselineHeight;
            float heightDifference = headPos.y - baselineHeight;
            
            string positionInfo = $"Y: {headPos.y:F3}m | Baseline: {baselineHeight:F3}m | Diff: {heightDifference:F3}m";
            headPositionText.text = positionInfo;
            
            // Color based on height difference
            if (heightDifference < -0.1f) // Significantly below baseline
            {
                headPositionText.color = validColor; // Green for squatting
            }
            else if (heightDifference > 0.1f) // Significantly above baseline
            {
                headPositionText.color = warningColor; // Yellow for standing tall
            }
            else
            {
                headPositionText.color = neutralColor; // White for normal
            }
        }
        
        // NEW: Display head movement and velocity information
        private void UpdateHeadMovementDisplay()
        {
            if (headMovementText == null) return;
            
            float velocity = squatDodge.CurrentVelocity;
            float absVelocity = Mathf.Abs(velocity);
            
            string movementInfo = $"Velocity: {velocity:F3}m/s | Speed: {absVelocity:F3}m/s";
            headMovementText.text = movementInfo;
            
            // Color based on movement speed
            if (absVelocity > 0.5f) // Moving fast
            {
                headMovementText.color = warningColor; // Yellow for fast movement
            }
            else if (absVelocity > 0.2f) // Moving moderately
            {
                headMovementText.color = Color.Lerp(neutralColor, warningColor, 0.5f); // Light yellow
            }
            else
            {
                headMovementText.color = validColor; // Green for slow/stable movement
            }
        }
        
        private void UpdateValidationDisplay()
        {
            if (validationText == null || validationStatusIcon == null) return;
            
            bool isValid = squatDodge.IsValidSquatForm;
            string validationStatus = isValid ? "VALID" : "INVALID";
            
            validationText.text = $"FORM: {validationStatus}";
            validationText.color = isValid ? validColor : invalidColor;
            validationStatusIcon.color = isValid ? validColor : invalidColor;
        }
        
        private void UpdateThreatDisplay()
        {
            if (threatText == null || threatIndicator == null) return;
            
            int threatCount = squatDodge.NearbyThreatCount;
            float closestDistance = squatDodge.ClosestThreatDistance;
            bool hasThreats = squatDodge.HasNearbyThreats;
            
            string threatInfo = hasThreats ? 
                $"{threatCount} threats (closest: {closestDistance:F1}m)" : 
                "No threats";
            
            threatText.text = $"THREATS: {threatInfo}";
            threatText.color = hasThreats ? warningColor : neutralColor;
            threatIndicator.color = hasThreats ? warningColor : neutralColor;
        }
        
        private void UpdateControllerDisplay()
        {
            if (controllerText == null) return;
            
            Vector3 leftPos = squatDodge.LeftControllerPosition;
            Vector3 rightPos = squatDodge.RightControllerPosition;
            Vector3 camPos = squatDodge.CameraPosition;
            
            // Calculate hand distances from head
            float leftDistance = Vector3.Distance(camPos, leftPos);
            float rightDistance = Vector3.Distance(camPos, rightPos);
            float handHeightDiff = Mathf.Abs(leftPos.y - rightPos.y);
            
            string controllerInfo = $"L:{leftDistance:F2}m R:{rightDistance:F2}m | Diff:{handHeightDiff:F2}m";
            controllerText.text = controllerInfo;
            
            // Color based on hand symmetry
            if (handHeightDiff > 0.5f)
            {
                controllerText.color = warningColor;
            }
            else if (handHeightDiff > 0.3f)
            {
                controllerText.color = Color.Lerp(neutralColor, warningColor, 0.5f);
            }
            else
            {
                controllerText.color = validColor;
            }
        }
        
        // NEW: Display controller movement information
        private void UpdateControllerMovementDisplay()
        {
            if (controllerText == null) return;
            
            float leftForward = squatDodge.LeftControllerForwardMovement;
            float rightForward = squatDodge.RightControllerForwardMovement;
            float combined = squatDodge.CombinedControllerMovement;
            bool detected = squatDodge.IsControllerMovementDetected;
            
            string movementInfo = $"L:{leftForward:F2}m R:{rightForward:F2}m | Combined:{combined:F2}m";
            if (detected)
            {
                movementInfo += " [DETECTED]";
            }
            
            controllerText.text = movementInfo;
            
            // Color based on detection
            if (detected)
            {
                controllerText.color = validColor; // Green when movement detected
            }
            else if (combined > 0.05f)
            {
                controllerText.color = warningColor; // Yellow for some movement
            }
            else
            {
                controllerText.color = neutralColor; // White for no movement
            }
        }
        
        // NEW: Update UI positioning continuously using ViewportToWorldPoint (like BillboardUI)
        private void UpdateUIPositioning()
        {
            if (debugCanvas == null || xrCamera == null) return;
            
            // Ensure camera is assigned to canvas
            if (debugCanvas.worldCamera != xrCamera)
            {
                debugCanvas.worldCamera = xrCamera;
            }
            
            // Use ViewportToWorldPoint for proper VR positioning (like BillboardUI)
            // Position UI in the center of the view at the assigned distance
            Vector3 viewportPos = new Vector3(0.5f, 0.5f, distanceFromCamera);
            debugCanvas.transform.position = xrCamera.ViewportToWorldPoint(viewportPos);
            
            // Face the camera (like BillboardUI)
            Vector3 dir = (xrCamera.transform.position - debugCanvas.transform.position).normalized;
            dir.y = 0; // ignore vertical component for horizontal facing
            if (dir.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(dir);
                debugCanvas.transform.rotation = targetRotation;
            }
        }
        
        private void UpdateBodyPoseDisplay()
        {
            if (bodyPoseText == null) return;
            
            float kneeAngle = squatDodge.EstimatedKneeAngle;
            Vector3 hipPos = squatDodge.EstimatedHipPosition;
            float velocity = squatDodge.CurrentVelocity;
            float dwellTime = squatDodge.DwellTime;
            bool isInBottom = squatDodge.IsInBottom;
            
            string poseInfo = $"Knee: {kneeAngle:F0}° | Hip: {hipPos.y:F2}m | V: {velocity:F2}";
            if (isInBottom)
            {
                poseInfo += $" | Dwell: {dwellTime:F1}s";
            }
            
            bodyPoseText.text = poseInfo;
            
            // Color based on movement state
            if (Mathf.Abs(velocity) > 0.5f)
            {
                bodyPoseText.color = warningColor; // Moving fast
            }
            else if (isInBottom)
            {
                bodyPoseText.color = validColor; // In squat position
            }
            else
            {
                bodyPoseText.color = neutralColor; // Normal
            }
        }
        
        private void UpdateVisualIndicators()
        {
            if (dodgeStatusIcon == null) return;
            
            // Update dodge status icon
            if (squatDodge.IsDodging)
            {
                dodgeStatusIcon.color = validColor;
            }
            else if (squatDodge.IsOnCooldown)
            {
                dodgeStatusIcon.color = warningColor;
            }
            else
            {
                dodgeStatusIcon.color = neutralColor;
            }
            
            // Update head movement indicator
            if (headMovementIndicator != null)
            {
                float absVelocity = Mathf.Abs(squatDodge.CurrentVelocity);
                
                if (absVelocity > 0.5f) // Moving fast
                {
                    headMovementIndicator.color = warningColor; // Yellow for fast movement
                }
                else if (absVelocity > 0.2f) // Moving moderately
                {
                    headMovementIndicator.color = Color.Lerp(neutralColor, warningColor, 0.5f); // Light yellow
                }
                else
                {
                    headMovementIndicator.color = validColor; // Green for slow/stable movement
                }
            }
        }
        
        public void SetVisibility(bool visible)
        {
            isVisible = visible;
            if (debugCanvas != null)
            {
                debugCanvas.gameObject.SetActive(visible);
            }
        }
        
        public void ToggleVisibility()
        {
            SetVisibility(!isVisible);
        }
        
        public void SetUpdateInterval(float interval)
        {
            updateInterval = Mathf.Max(0.01f, interval);
            if (updateCoroutine != null)
            {
                StartUpdateCoroutine();
            }
        }
        
        // Public API for external control
        public void ShowDebugInfo()
        {
            if (squatDodge != null)
            {
                squatDodge.DebugSquatDetection();
            }
        }
        
        public void ForceUpdate()
        {
            if (isInitialized && squatDodge != null)
            {
                UpdateAllUI();
            }
        }
        
        // Keyboard shortcuts for debugging
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.F1))
            {
                ToggleVisibility();
            }
            
            if (Input.GetKeyDown(KeyCode.F2))
            {
                ShowDebugInfo();
            }
            
            if (Input.GetKeyDown(KeyCode.F3))
            {
                ForceUpdate();
            }
        }
    }
}
