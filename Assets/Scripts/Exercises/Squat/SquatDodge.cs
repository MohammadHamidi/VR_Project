using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using CombatSystem.Events;

namespace CombatSystem.Player
{
    public class SquatDodge : MonoBehaviour
    {
        public static SquatDodge Instance { get; private set; }

        [Header("Tracking References")]
        [SerializeField] private Transform xrCamera;
        [SerializeField] private Transform leftController;
        [SerializeField] private Transform rightController;

        [Header("Squat Detection - OPTIMIZED")]
        [SerializeField] private float targetDepth = 0.25f;        // Slightly deeper for better detection
        [SerializeField] private float validThreshold = 0.5f;      // 50% depth needed
        [SerializeField] private float dwellMin = 0.15f;           // Shorter minimum hold
        [SerializeField] private float dwellMax = 1.0f;            // Longer maximum hold
        [SerializeField] private float dodgeDuration = 0.5f;
        [SerializeField] private float cooldownDuration = 0.1f;    // Very short cooldown
        [SerializeField] private float squatEventCooldown = 0.2f;  // Reduced event cooldown

        [Header("Context Awareness - FIXED")]
        [SerializeField] private float threatDetectionRange = 8f;
        [SerializeField] private float noThreatPowerMultiplier = 0.7f;  // Better practice rewards
        [SerializeField] private float practiceRewardThreshold = 1.5f;  // More frequent practice
        [SerializeField] private bool enablePracticeRewards = true;
        [SerializeField] private bool requireThreatsForDodge = false;

        [Header("IK Body Proportions")]
        [SerializeField] private float shoulderWidth = 0.4f;
        [SerializeField] private float armLength = 0.6f;
        [SerializeField] private float torsoLength = 0.5f;
        [SerializeField] private float thighLength = 0.4f;
        [SerializeField] private float neckLength = 0.15f;

        [Header("Enhanced Validation - RELAXED")]
        [SerializeField] private float maxHandAsymmetry = 0.6f;        // More hand asymmetry allowed
        [SerializeField] private float maxNaturalReach = 1.4f;         // Longer reach allowed
        [SerializeField] private float coordinationThreshold = 0.4f;   // Much less strict coordination
        [SerializeField] private float minKneeAngle = 30f;             // Deeper squats allowed
        [SerializeField] private float maxKneeAngle = 160f;            // Shallower squats allowed
        [SerializeField] private float maxSpineAngle = 45f;            // More forward lean allowed

        [Header("Quality Assessment - RELAXED")]
        [SerializeField] private float perfectQualityThreshold = 70f;  // Easier perfect squats
        [SerializeField] private float smoothingFactor = 0.3f;         // More responsive
        [SerializeField] private float velocitySmoothing = 0.3f;

        [Header("Dodge Feedback")]
        [SerializeField] private AudioClip dodgeSound;
        [SerializeField] private AudioClip perfectSquatSound;
        [SerializeField] private AudioClip practiceSound;
        [SerializeField] private ParticleSystem dodgeEffect;
        [SerializeField] private ParticleSystem perfectSquatEffect;

        [Header("Testing & Simulation")]
        [SerializeField] private bool enableKeyboardSimulation = true;
        [SerializeField] private KeyCode simulateSquatKey = KeyCode.L;
        [SerializeField] private float simulationDuration = 1.0f;

        [Header("Debug Settings")]
        [SerializeField] private bool enableDebugLogs = true;
        [SerializeField] private bool logMovementDetails = false;
        
        [Header("Validation Options")]
        [SerializeField] private bool enableIKValidation = false;  // NEW: Toggle IK validation
        [SerializeField] private bool headOnlyMode = true;         // NEW: Head-only tracking mode
        
        [Header("Auto-Calibration & Compensation")]
        [SerializeField] private bool enableAutoCalibration = true;    // NEW: Automatic calibration
        [SerializeField] private float depthMultiplier = 3.0f;         // NEW: Depth compensation multiplier
        [SerializeField] private float autoCalibrationSensitivity = 0.05f; // NEW: How sensitive auto-calibration is
        [SerializeField] private float maxAutoAdjustment = 0.1f;       // NEW: Max automatic adjustment per frame
        
        [Header("Controller Movement Detection")]
        [SerializeField] private bool enableControllerMovementDetection = true; // NEW: Enable controller movement detection
        [SerializeField] private float controllerForwardThreshold = 0.1f;       // NEW: How much forward movement needed
        [SerializeField] private float controllerMovementWeight = 0.3f;         // NEW: Weight of controller movement in detection

        // Public Properties
        public bool IsDodging { get; private set; }
        public float CurrentSquatDepth { get; private set; }
        public float CurrentDepthNorm { get; private set; }
        public bool IsOnCooldown { get; private set; }
        public float BaselineHeight { get; private set; }
        public Vector3 EstimatedHipPosition { get; private set; }
        public float EstimatedKneeAngle { get; private set; }
        public bool IsValidSquatForm { get; private set; }

        // Context awareness properties
        public bool HasNearbyThreats { get; private set; }
        public int NearbyThreatCount { get; private set; }
        public float ClosestThreatDistance { get; private set; }

        private AudioSource _audioSource;
        private Coroutine _dodgeCoroutine;
        private Coroutine _cooldownCoroutine;
        private Coroutine _simulationCoroutine;
        
        // SIMPLIFIED tracking data
        private float smoothedY;
        private float velocity;
        private float lastSquatEventTime = -1f;
        private float lastValidSquatTime = -1f;
        private float lastPracticeRewardTime = -1f;
        
        // SIMPLIFIED squat state tracking
        private bool isInSquatPosition = false;
        private float squatEnterTime = 0f;
        private bool hasProcessedCurrentSquat = false;
        
        // IK baseline positions
        private Vector3 baselineShoulderCenter;
        private Vector3 baselineHipPosition;
        private float baselineHandHeadDistance;
        
        // Controller tracking
        private Vector3 leftControllerVelocity;
        private Vector3 rightControllerVelocity;
        private Vector3 lastLeftPos;
        private Vector3 lastRightPos;

        // Simulation state
        private bool isSimulating = false;
        private float simulationStartTime;

        // Auto-calibration state
        private float maxObservedHeight = float.MinValue;
        private float minObservedHeight = float.MaxValue;
        private float autoCalibrationTimer = 0f;
        private float lastAutoCalibrationTime = 0f;
        private bool hasObservedFullRange = false;
        
        // Controller movement detection state
        private Vector3 baselineLeftControllerPos;
        private Vector3 baselineRightControllerPos;
        private float leftControllerForwardMovement = 0f;
        private float rightControllerForwardMovement = 0f;
        private float combinedControllerMovement = 0f;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null)
                _audioSource = gameObject.AddComponent<AudioSource>();

            InitializeXRReferences();
        }

        private void InitializeXRReferences()
        {
            if (xrCamera == null)
            {
                var xrOrigin = FindObjectOfType<Unity.XR.CoreUtils.XROrigin>();
                if (xrOrigin != null)
                {
                    xrCamera = xrOrigin.Camera.transform;
                }
                else
                {
                    xrCamera = Camera.main?.transform;
                }
            }

            // Auto-find controllers if not assigned
            if (leftController == null || rightController == null)
            {
                var inputDevices = new List<UnityEngine.XR.InputDevice>();
                UnityEngine.XR.InputDevices.GetDevices(inputDevices);
                
                foreach (var device in inputDevices)
                {
                    if (device.characteristics.HasFlag(InputDeviceCharacteristics.Left | InputDeviceCharacteristics.Controller))
                    {
                        var controllers = FindObjectsOfType<Transform>();
                        foreach (var controller in controllers)
                        {
                            if (controller.name.ToLower().Contains("left") && controller.name.ToLower().Contains("controller"))
                            {
                                leftController = controller;
                                break;
                            }
                        }
                    }
                    else if (device.characteristics.HasFlag(InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller))
                    {
                        var controllers = FindObjectsOfType<Transform>();
                        foreach (var controller in controllers)
                        {
                            if (controller.name.ToLower().Contains("right") && controller.name.ToLower().Contains("controller"))
                            {
                                rightController = controller;
                                break;
                            }
                        }
                    }
                }
            }
        }

        void Start()
        {
            CalibrateStandingHeight();
            CalibrateBodyProportions();
        }

        void Update()
        {
            HandleKeyboardInput();
            UpdateControllerVelocities();
            UpdateControllerMovement(); // NEW: Update controller movement detection
            UpdateThreatDetection();
            CheckSquatMovement(); // RENAMED and SIMPLIFIED
        }

        private void UpdateThreatDetection()
        {
            if (xrCamera == null) return;

            var drones = FindObjectsOfType<CombatSystem.Drones.DroneController>();
            var activeDrones = new List<CombatSystem.Drones.DroneController>();
            
            ClosestThreatDistance = float.MaxValue;
            
            foreach (var drone in drones)
            {
                if (drone.IsDestroyed) continue;
                
                float distance = Vector3.Distance(xrCamera.position, drone.transform.position);
                if (distance <= threatDetectionRange)
                {
                    activeDrones.Add(drone);
                    if (distance < ClosestThreatDistance)
                    {
                        ClosestThreatDistance = distance;
                    }
                }
            }
            
            NearbyThreatCount = activeDrones.Count;
            HasNearbyThreats = NearbyThreatCount > 0;
        }

        private void HandleKeyboardInput()
        {
            if (enableKeyboardSimulation && Input.GetKeyDown(simulateSquatKey))
            {
                if (!isSimulating && !IsDodging && !IsOnCooldown)
                {
                    if (enableDebugLogs)
                    Debug.Log($"[{simulateSquatKey}] Key pressed - Simulating squat action");
                    SimulateSquat();
                }
            }
            
            // NEW: Calibration adjustment shortcuts
            if (Input.GetKeyDown(KeyCode.C))
            {
                CalibrateWithRangeDetection();
            }
            
            if (Input.GetKeyDown(KeyCode.R))
            {
                CalibrateStandingHeight();
            }
            
            // Fine-tune baseline height
            if (Input.GetKey(KeyCode.LeftShift))
            {
                if (Input.GetKeyDown(KeyCode.UpArrow))
                {
                    AdjustBaselineHeight(0.01f); // Increase baseline
                }
                if (Input.GetKeyDown(KeyCode.DownArrow))
                {
                    AdjustBaselineHeight(-0.01f); // Decrease baseline
                }
            }
            
            // Fine-tune target depth
            if (Input.GetKey(KeyCode.LeftControl))
            {
                if (Input.GetKeyDown(KeyCode.UpArrow))
                {
                    AdjustTargetDepth(0.01f); // Increase target depth
                }
                if (Input.GetKeyDown(KeyCode.DownArrow))
                {
                    AdjustTargetDepth(-0.01f); // Decrease target depth
                }
            }
            
            // NEW: Auto-calibration shortcuts
            if (Input.GetKeyDown(KeyCode.A))
            {
                SetAutoCalibration(!enableAutoCalibration);
            }
            
            if (Input.GetKeyDown(KeyCode.Z))
            {
                ResetAutoCalibration();
            }
            
            if (Input.GetKeyDown(KeyCode.X))
            {
                ForceAutoCalibration();
            }
            
            // Fine-tune depth multiplier
            if (Input.GetKey(KeyCode.LeftAlt))
            {
                if (Input.GetKeyDown(KeyCode.UpArrow))
                {
                    SetDepthMultiplier(depthMultiplier + 0.5f); // Increase multiplier
                }
                if (Input.GetKeyDown(KeyCode.DownArrow))
                {
                    SetDepthMultiplier(depthMultiplier - 0.5f); // Decrease multiplier
                }
            }
            
            // NEW: Controller movement detection shortcuts
            if (Input.GetKeyDown(KeyCode.M))
            {
                SetControllerMovementDetection(!enableControllerMovementDetection);
            }
        }

        private void SimulateSquat()
        {
            if (_simulationCoroutine != null)
                StopCoroutine(_simulationCoroutine);
            
            _simulationCoroutine = StartCoroutine(SimulateSquatRoutine());
        }

        private IEnumerator SimulateSquatRoutine()
        {
            isSimulating = true;
            simulationStartTime = Time.time;
            
            if (enableDebugLogs)
            Debug.Log("Starting squat simulation...");
            
            float originalDepth = CurrentSquatDepth;
            float originalDepthNorm = CurrentDepthNorm;
            bool originalValidForm = IsValidSquatForm;
            
            CurrentSquatDepth = targetDepth;
            CurrentDepthNorm = 1.0f;
            IsValidSquatForm = true;
            EstimatedKneeAngle = 70f;
            
            CombatEvents.OnPlayerSquatDepthChanged?.Invoke(CurrentSquatDepth);
            
            yield return new WaitForSeconds(dwellMin + 0.1f);
            
            if (Time.time - lastSquatEventTime >= squatEventCooldown)
            {
                float simulatedQuality = CalculateSimulatedSquatQuality();
                ProcessSquatWithContext(CurrentDepthNorm, simulatedQuality, true);
                lastSquatEventTime = Time.time;
                
                if (enableDebugLogs)
                Debug.Log($"Simulated squat completed - Quality: {simulatedQuality:F1}");
                
                if ((!requireThreatsForDodge || HasNearbyThreats) && !IsDodging && !IsOnCooldown)
                {
                    TriggerDodge();
                }
            }
            
            yield return new WaitForSeconds(simulationDuration - (dwellMin + 0.1f));
            
            CurrentSquatDepth = originalDepth;
            CurrentDepthNorm = originalDepthNorm;
            IsValidSquatForm = originalValidForm;
            
            isSimulating = false;
            if (enableDebugLogs)
            Debug.Log("Squat simulation ended");
        }

        private float CalculateSimulatedSquatQuality()
        {
            return 95f;
        }

        private void UpdateControllerVelocities()
        {
            if (leftController != null)
            {
                leftControllerVelocity = (leftController.position - lastLeftPos) / Time.deltaTime;
                lastLeftPos = leftController.position;
            }

            if (rightController != null)
            {
                rightControllerVelocity = (rightController.position - lastRightPos) / Time.deltaTime;
                lastRightPos = rightController.position;
            }
        }

        // NEW: Calculate controller forward movement for squat detection
        private void UpdateControllerMovement()
        {
            if (!enableControllerMovementDetection || leftController == null || rightController == null)
            {
                combinedControllerMovement = 0f;
                return;
            }
            
            // Calculate forward movement relative to camera
            Vector3 cameraForward = xrCamera.forward;
            
            // Left controller forward movement
            Vector3 leftMovement = leftController.position - baselineLeftControllerPos;
            leftControllerForwardMovement = Vector3.Dot(leftMovement, cameraForward);
            
            // Right controller forward movement
            Vector3 rightMovement = rightController.position - baselineRightControllerPos;
            rightControllerForwardMovement = Vector3.Dot(rightMovement, cameraForward);
            
            // Combine both controller movements
            combinedControllerMovement = (leftControllerForwardMovement + rightControllerForwardMovement) / 2f;
            
            // Only consider positive forward movement (hands moving forward)
            combinedControllerMovement = Mathf.Max(0f, combinedControllerMovement);
        }

        // NEW: Automatic calibration that continuously learns from user movement
        private void UpdateAutoCalibration(float currentY)
        {
            // Track min/max heights observed
            if (currentY > maxObservedHeight)
            {
                maxObservedHeight = currentY;
            }
            if (currentY < minObservedHeight)
            {
                minObservedHeight = currentY;
            }
            
            // Check if we've observed a significant range
            float observedRange = maxObservedHeight - minObservedHeight;
            if (observedRange > autoCalibrationSensitivity && !hasObservedFullRange)
            {
                hasObservedFullRange = true;
                if (enableDebugLogs)
                    Debug.Log($"Auto-calibration: Observed movement range of {observedRange:F3}m");
            }
            
            // Periodically adjust baseline and target depth based on observations
            autoCalibrationTimer += Time.deltaTime;
            if (autoCalibrationTimer >= 5f) // Every 5 seconds
            {
                autoCalibrationTimer = 0f;
                PerformAutoCalibrationAdjustment();
            }
        }
        
        // NEW: Perform automatic calibration adjustments
        private void PerformAutoCalibrationAdjustment()
        {
            if (!hasObservedFullRange) return;
            
            float observedRange = maxObservedHeight - minObservedHeight;
            float currentBaseline = BaselineHeight;
            float currentTarget = targetDepth;
            
            // Adjust baseline to be closer to the observed maximum (standing height)
            float newBaseline = Mathf.Lerp(currentBaseline, maxObservedHeight, 0.1f);
            float baselineAdjustment = newBaseline - currentBaseline;
            
            // Limit the adjustment to prevent sudden changes
            baselineAdjustment = Mathf.Clamp(baselineAdjustment, -maxAutoAdjustment, maxAutoAdjustment);
            BaselineHeight += baselineAdjustment;
            
            // Adjust target depth based on observed range
            float idealTargetDepth = observedRange * 0.7f; // Use 70% of observed range
            float targetAdjustment = (idealTargetDepth - currentTarget) * 0.1f;
            targetAdjustment = Mathf.Clamp(targetAdjustment, -maxAutoAdjustment, maxAutoAdjustment);
            targetDepth = Mathf.Max(0.1f, targetDepth + targetAdjustment);
            
            // Reset observed range periodically to allow for new learning
            if (Time.time - lastAutoCalibrationTime > 30f) // Every 30 seconds
            {
                maxObservedHeight = float.MinValue;
                minObservedHeight = float.MaxValue;
                hasObservedFullRange = false;
                lastAutoCalibrationTime = Time.time;
                
                if (enableDebugLogs)
                    Debug.Log("Auto-calibration: Reset observation range for new learning cycle");
            }
            
            if (enableDebugLogs && (Mathf.Abs(baselineAdjustment) > 0.001f || Mathf.Abs(targetAdjustment) > 0.001f))
            {
                Debug.Log($"Auto-calibration adjustment: Baseline {baselineAdjustment:+0.000;-0.000}m, Target {targetAdjustment:+0.000;-0.000}m");
            }
        }

        // COMPLETELY REWRITTEN squat detection logic with auto-calibration
        private void CheckSquatMovement()
        {
            if (xrCamera == null || isSimulating) return;

            // Update basic measurements
            float currentY = xrCamera.position.y;
            smoothedY = Mathf.Lerp(smoothedY, currentY, smoothingFactor);
            
            // Auto-calibration: Track min/max heights
            if (enableAutoCalibration)
            {
                UpdateAutoCalibration(currentY);
            }
            
            // Calculate depth with multiplier compensation
            float rawDepth = Mathf.Max(0f, BaselineHeight - smoothedY);
            float compensatedDepth = rawDepth * depthMultiplier; // Apply depth multiplier
            
            // NEW: Add controller movement to depth calculation
            float controllerContribution = 0f;
            if (enableControllerMovementDetection && combinedControllerMovement > controllerForwardThreshold)
            {
                controllerContribution = combinedControllerMovement * controllerMovementWeight;
            }
            
            float totalDepth = compensatedDepth + controllerContribution;
            float depthNorm = Mathf.Clamp01(totalDepth / targetDepth);
            
            velocity = Mathf.Lerp(velocity, (currentY - smoothedY) / Time.deltaTime, velocitySmoothing);

            // Update public properties (use total depth including controller movement)
            CurrentSquatDepth = totalDepth;
            CurrentDepthNorm = depthNorm;

            // Estimate body pose and validate form (only if IK validation is enabled)
            if (enableIKValidation && !headOnlyMode)
            {
            EstimateBodyPose();
            }
            IsValidSquatForm = ValidateSquatForm(); // SIMPLIFIED validation

            // Fire depth change event
            CombatEvents.OnPlayerSquatDepthChanged?.Invoke(CurrentSquatDepth);

            // SIMPLIFIED state logic
            bool isCurrentlyInValidSquat = depthNorm >= validThreshold && IsValidSquatForm;

            // if (logMovementDetails)
            // {
            //     Debug.Log($"Movement: Y={currentY:F3}, Depth=, DepthNorm={depthNorm:F2}, ValidForm={IsValidSquatForm}, InSquat={isCurrentlyInValidSquat}");
            // }

            // Handle squat state transitions
            if (isCurrentlyInValidSquat && !isInSquatPosition)
            {
                // Entering squat position
                isInSquatPosition = true;
                squatEnterTime = Time.time;
                hasProcessedCurrentSquat = false;
                
                if (enableDebugLogs)
                    Debug.Log($"ENTERED SQUAT POSITION - Depth: {depthNorm:F2}, Threats: {HasNearbyThreats}");
                
                // Immediate dodge trigger if allowed
                if (CanTriggerDodge())
                {
                    TriggerDodge();
                }
            }
            else if (!isCurrentlyInValidSquat && isInSquatPosition)
            {
                // Exiting squat position
                float dwellTime = Time.time - squatEnterTime;
                
                if (enableDebugLogs)
                    Debug.Log($"EXITED SQUAT POSITION - Dwell: {dwellTime:F2}s, Processed: {hasProcessedCurrentSquat}");
                
                // Process squat if it meets criteria and hasn't been processed yet
                if (!hasProcessedCurrentSquat && dwellTime >= dwellMin && dwellTime <= dwellMax)
                {
                    ProcessCompletedSquat(depthNorm, dwellTime);
                }
                
                isInSquatPosition = false;
                hasProcessedCurrentSquat = false;
            }
            else if (isInSquatPosition && !hasProcessedCurrentSquat)
            {
                // Still in squat position - check if we should process it
                float currentDwellTime = Time.time - squatEnterTime;
                
                // Process if minimum dwell time is met and enough time has passed since last event
                if (currentDwellTime >= dwellMin && Time.time - lastSquatEventTime >= squatEventCooldown)
                {
                    ProcessCompletedSquat(depthNorm, currentDwellTime);
                }
            }
        }

        // SIMPLIFIED validation - IK validation is now optional
        private bool ValidateSquatForm()
        {
            // Head-only mode: always return true (no IK validation)
            if (headOnlyMode)
            {
                return true;
            }
            
            // If IK validation is disabled, return true
            if (!enableIKValidation)
            {
                return true;
            }
            
            // If no controllers, just return true (head-only tracking)
            if (leftController == null || rightController == null) 
            {
                return true;
            }

            // Check only the most important validations
            bool reasonableHandPosition = ValidateNaturalHandPosition();
            bool notExtremeAsymmetry = ValidateHandSymmetry();
            bool validKneeRange = ValidateKneeAngle();

            // Only require 2 out of 3 checks to pass
            int passedChecks = 0;
            if (reasonableHandPosition) passedChecks++;
            if (notExtremeAsymmetry) passedChecks++;
            if (validKneeRange) passedChecks++;

            bool isValid = passedChecks >= 2;
            
            if (!isValid && enableDebugLogs)
            {
                Debug.Log($"Form validation failed: HandPos={reasonableHandPosition}, Symmetry={notExtremeAsymmetry}, Knee={validKneeRange} ({passedChecks}/3)");
            }

            return isValid;
        }

        private bool CanTriggerDodge()
        {
            return (!requireThreatsForDodge || HasNearbyThreats) && 
                   !IsDodging && 
                   !IsOnCooldown && 
                   Time.time - lastValidSquatTime >= cooldownDuration;
        }

        private void ProcessCompletedSquat(float depthNorm, float dwellTime)
        {
            hasProcessedCurrentSquat = true;
            lastSquatEventTime = Time.time;
            
            float quality = CalculateSquatQuality(depthNorm, dwellTime);
            ProcessSquatWithContext(depthNorm, quality, false);
            
            if (enableDebugLogs)
                Debug.Log($"PROCESSED SQUAT - Depth: {depthNorm:F2}, Dwell: {dwellTime:F2}s, Quality: {quality:F1}");
        }

        private float CalculateSquatQuality(float depthNorm, float dwellTime)
        {
            // Simplified quality calculation
            float depthScore = depthNorm * 40f;
            float dwellScore = Mathf.Clamp01((dwellTime - dwellMin) / (dwellMax - dwellMin)) * 30f;
            float stabilityScore = Mathf.Clamp01(1f - Mathf.Abs(velocity) * 0.1f) * 20f;
            float formScore = IsValidSquatForm ? 10f : 5f;
            
            return depthScore + dwellScore + stabilityScore + formScore;
        }

        private void EstimateBodyPose()
        {
            if (leftController == null || rightController == null) return;

            Vector3 shoulderCenter = (leftController.position + rightController.position) / 2f;
            EstimatedHipPosition = shoulderCenter + Vector3.down * torsoLength;
            float hipDrop = baselineHipPosition.y - EstimatedHipPosition.y;
            EstimatedKneeAngle = CalculateKneeAngleFromHipDrop(hipDrop);
        }

        private float CalculateKneeAngleFromHipDrop(float hipDrop)
        {
            float normalizedDrop = Mathf.Clamp01(hipDrop / targetDepth);
            return Mathf.Lerp(180f, 60f, normalizedDrop);
        }

        private bool ValidateHandSymmetry()
        {
            float handHeightDifference = Mathf.Abs(leftController.position.y - rightController.position.y);
            return handHeightDifference <= maxHandAsymmetry;
        }

        private bool ValidateNaturalHandPosition()
        {
            float leftHandDistance = Vector3.Distance(xrCamera.position, leftController.position);
            float rightHandDistance = Vector3.Distance(xrCamera.position, rightController.position);
            float avgDistance = (leftHandDistance + rightHandDistance) / 2f;
            
            return avgDistance <= maxNaturalReach;
        }

        private bool ValidateKneeAngle()
        {
            return EstimatedKneeAngle >= minKneeAngle && EstimatedKneeAngle <= maxKneeAngle;
        }

        private void ProcessSquatWithContext(float depthNorm, float quality, bool isSimulation)
        {
            bool isPerfect = quality >= perfectQualityThreshold;
            
            if (HasNearbyThreats)
            {
                // Full rewards when threats are present
                CombatEvents.OnValidSquat?.Invoke(depthNorm, quality);
                if (isPerfect)
                {
                    CombatEvents.OnPerfectSquat?.Invoke(quality);
                }
                
                PlaySquatFeedback(isPerfect, false);
                
                if (enableDebugLogs)
                Debug.Log($"Threat-based squat: Depth={depthNorm:F2}, Quality={quality:F1}, Threats={NearbyThreatCount}");
            }
            else if (enablePracticeRewards)
            {
                // Reduced rewards for practice (no threats)
                if (Time.time - lastPracticeRewardTime >= practiceRewardThreshold)
                {
                    float practiceQuality = quality * noThreatPowerMultiplier;
                    CombatEvents.OnValidSquat?.Invoke(depthNorm, practiceQuality);
                    PlaySquatFeedback(false, true);
                    lastPracticeRewardTime = Time.time;
                    
                    if (enableDebugLogs)
                    Debug.Log($"Practice squat: Depth={depthNorm:F2}, Quality={practiceQuality:F1} (Reduced)");
                }
                else
                {
                    if (enableDebugLogs)
                    Debug.Log("Practice squat on cooldown, no rewards");
                }
            }
            else
            {
                if (enableDebugLogs)
                Debug.Log("No threats detected, squat ignored (practice rewards disabled)");
            }
        }

        public void CalibrateStandingHeight()
        {
            if (xrCamera != null)
            {
                BaselineHeight = xrCamera.position.y;
                smoothedY = BaselineHeight;
                if (enableDebugLogs)
                Debug.Log($"Standing height calibrated to: {BaselineHeight:F2}m");
            }
        }
        
        // NEW: Enhanced calibration with range detection
        public void CalibrateWithRangeDetection()
        {
            StartCoroutine(CalibrateWithRangeRoutine());
        }
        
        private IEnumerator CalibrateWithRangeRoutine()
        {
            if (xrCamera == null) yield break;
            
            if (enableDebugLogs)
                Debug.Log("Starting enhanced calibration - please stand normally for 2 seconds...");
            
            // First, get standing height
            float standingSum = 0f;
            int standingCount = 0;
            float endTime = Time.time + 2f;
            
            while (Time.time < endTime)
            {
                standingSum += xrCamera.position.y;
                standingCount++;
                yield return null;
            }
            
            if (standingCount > 0)
            {
                BaselineHeight = standingSum / standingCount;
                smoothedY = BaselineHeight;
                
                if (enableDebugLogs)
                    Debug.Log($"Standing height calibrated: {BaselineHeight:F3}m");
            }
            
            // Now detect the full range by asking user to squat
            if (enableDebugLogs)
                Debug.Log("Now please do a full squat and hold for 3 seconds to detect range...");
            
            float minHeight = BaselineHeight;
            float maxHeight = BaselineHeight;
            float rangeSum = 0f;
            int rangeCount = 0;
            endTime = Time.time + 3f;
            
            while (Time.time < endTime)
            {
                float currentY = xrCamera.position.y;
                rangeSum += currentY;
                rangeCount++;
                
                if (currentY < minHeight) minHeight = currentY;
                if (currentY > maxHeight) maxHeight = currentY;
                
                yield return null;
            }
            
            if (rangeCount > 0)
            {
                float detectedRange = maxHeight - minHeight;
                float averageHeight = rangeSum / rangeCount;
                
                // Adjust target depth based on detected range
                if (detectedRange > 0.1f) // If we detected significant movement
                {
                    targetDepth = Mathf.Max(0.15f, detectedRange * 0.8f); // Use 80% of detected range
                    if (enableDebugLogs)
                        Debug.Log($"Detected movement range: {detectedRange:F3}m, adjusted target depth to: {targetDepth:F3}m");
                }
                else
                {
                    if (enableDebugLogs)
                        Debug.Log($"Limited movement detected ({detectedRange:F3}m), using default target depth: {targetDepth:F3}m");
                }
                
                if (enableDebugLogs)
                {
                    Debug.Log($"Calibration complete:");
                    Debug.Log($"  Standing Height: {BaselineHeight:F3}m");
                    Debug.Log($"  Min Height: {minHeight:F3}m");
                    Debug.Log($"  Max Height: {maxHeight:F3}m");
                    Debug.Log($"  Detected Range: {detectedRange:F3}m");
                    Debug.Log($"  Target Depth: {targetDepth:F3}m");
                }
            }
        }

        private void CalibrateBodyProportions()
        {
            if (leftController != null && rightController != null)
            {
                baselineShoulderCenter = (leftController.position + rightController.position) / 2f;
                baselineHipPosition = baselineShoulderCenter + Vector3.down * torsoLength;
                
                float leftDistance = Vector3.Distance(xrCamera.position, leftController.position);
                float rightDistance = Vector3.Distance(xrCamera.position, rightController.position);
                baselineHandHeadDistance = (leftDistance + rightDistance) / 2f;
                
                // NEW: Calibrate controller baseline positions for movement detection
                baselineLeftControllerPos = leftController.position;
                baselineRightControllerPos = rightController.position;
                
                if (enableDebugLogs)
                    Debug.Log($"Controller baseline positions calibrated - Left: {baselineLeftControllerPos:F3}, Right: {baselineRightControllerPos:F3}");
            }
        }

        public void RecalibrateStanding(float sampleSeconds = 1f)
        {
            StartCoroutine(CoRecalibrate(sampleSeconds));
        }

        private IEnumerator CoRecalibrate(float sampleTime)
        {
            if (xrCamera == null) yield break;

            float headSum = 0f;
            Vector3 leftSum = Vector3.zero;
            Vector3 rightSum = Vector3.zero;
            int count = 0;
            float endTime = Time.time + sampleTime;

            while (Time.time < endTime)
            {
                headSum += xrCamera.position.y;
                if (leftController != null) leftSum += leftController.position;
                if (rightController != null) rightSum += rightController.position;
                count++;
                yield return null;
            }

            if (count > 0)
            {
                BaselineHeight = headSum / count;
                smoothedY = BaselineHeight;
                
                if (leftController != null && rightController != null)
                {
                    baselineShoulderCenter = (leftSum + rightSum) / (2f * count);
                    baselineHipPosition = baselineShoulderCenter + Vector3.down * torsoLength;
                }
                
                if (enableDebugLogs)
                Debug.Log($"Enhanced calibration complete - Height: {BaselineHeight:F2}m");
            }
        }

        private void TriggerDodge()
        {
            if (_dodgeCoroutine != null)
                StopCoroutine(_dodgeCoroutine);
            
            _dodgeCoroutine = StartCoroutine(DodgeRoutine());
        }

        private IEnumerator DodgeRoutine()
        {
            IsDodging = true;
            lastValidSquatTime = Time.time;
            CombatEvents.OnPlayerDodge?.Invoke();
            PlayDodgeFeedback();

            if (enableDebugLogs)
                Debug.Log("DODGE TRIGGERED!");

            yield return new WaitForSeconds(dodgeDuration);
            IsDodging = false;

            if (_cooldownCoroutine != null)
                StopCoroutine(_cooldownCoroutine);
            
            _cooldownCoroutine = StartCoroutine(CooldownRoutine());
        }

        private IEnumerator CooldownRoutine()
        {
            IsOnCooldown = true;
            yield return new WaitForSeconds(cooldownDuration);
            IsOnCooldown = false;
        }

        private void PlayDodgeFeedback()
        {
            if (_audioSource != null && dodgeSound != null)
                _audioSource.PlayOneShot(dodgeSound);

            if (dodgeEffect != null)
                dodgeEffect.Play();
        }

        private void PlaySquatFeedback(bool isPerfect, bool isPractice)
        {
            if (_audioSource != null)
            {
                AudioClip soundToPlay;
                if (isPerfect && perfectSquatSound != null)
                {
                    soundToPlay = perfectSquatSound;
                }
                else if (isPractice && practiceSound != null)
                {
                    soundToPlay = practiceSound;
                }
                else
                {
                    soundToPlay = dodgeSound;
                }
                
                if (soundToPlay != null)
                {
                    _audioSource.pitch = isPerfect ? 1.2f : (isPractice ? 0.8f : 1f);
                    _audioSource.PlayOneShot(soundToPlay);
                }
            }

            if (isPerfect && perfectSquatEffect != null)
            {
                perfectSquatEffect.Play();
            }
            else if (dodgeEffect != null)
            {
                dodgeEffect.Play();
            }
        }

        // Public API
        public void SimulateSquatAction()
        {
            if (!isSimulating && !IsDodging && !IsOnCooldown)
            {
                SimulateSquat();
            }
        }

        public void SetTestingMode(bool enableTesting)
        {
            if (enableTesting)
            {
                targetDepth = 0.15f;      // Very shallow for testing
                validThreshold = 0.3f;    // Very low threshold
                requireThreatsForDodge = false;
                enablePracticeRewards = true;
                enableDebugLogs = true;
                logMovementDetails = true;
                headOnlyMode = true;      // Enable head-only mode for testing
                enableIKValidation = false; // Disable IK validation for testing
        
                if (enableDebugLogs)
                    Debug.Log("TESTING MODE: Squat detection made very lenient for debugging (Head-only mode)");
            }
            else
            {
                // Reset to normal values
                targetDepth = 0.25f;
                validThreshold = 0.5f;
                requireThreatsForDodge = false;
                enableDebugLogs = true;
                logMovementDetails = false;
                headOnlyMode = true;      // Keep head-only mode as default
                enableIKValidation = false; // Keep IK validation disabled as default
        
                if (enableDebugLogs)
                    Debug.Log("Normal mode restored (Head-only mode)");
            }
        }

        public bool IsSimulatingSquat()
        {
            return isSimulating;
        }
        
        // NEW: Public methods to control validation modes
        public void SetHeadOnlyMode(bool enabled)
        {
            headOnlyMode = enabled;
            if (enableDebugLogs)
                Debug.Log($"Head-only mode: {(enabled ? "ENABLED" : "DISABLED")}");
        }
        
        public void SetIKValidation(bool enabled)
        {
            enableIKValidation = enabled;
            if (enableDebugLogs)
                Debug.Log($"IK validation: {(enabled ? "ENABLED" : "DISABLED")}");
        }
        
        public bool IsHeadOnlyMode()
        {
            return headOnlyMode;
        }
        
        public bool IsIKValidationEnabled()
        {
            return enableIKValidation;
        }
        
        // NEW: Manual calibration adjustment methods
        public void AdjustBaselineHeight(float adjustment)
        {
            BaselineHeight += adjustment;
            smoothedY = BaselineHeight;
            if (enableDebugLogs)
                Debug.Log($"Baseline height adjusted by {adjustment:F3}m to {BaselineHeight:F3}m");
        }
        
        public void SetBaselineHeight(float newHeight)
        {
            BaselineHeight = newHeight;
            smoothedY = BaselineHeight;
            if (enableDebugLogs)
                Debug.Log($"Baseline height set to {BaselineHeight:F3}m");
        }
        
        public void AdjustTargetDepth(float adjustment)
        {
            targetDepth = Mathf.Max(0.1f, targetDepth + adjustment);
            if (enableDebugLogs)
                Debug.Log($"Target depth adjusted by {adjustment:F3}m to {targetDepth:F3}m");
        }
        
        public void SetTargetDepth(float newDepth)
        {
            targetDepth = Mathf.Max(0.1f, newDepth);
            if (enableDebugLogs)
                Debug.Log($"Target depth set to {targetDepth:F3}m");
        }
        
        // NEW: Auto-calibration control methods
        public void SetAutoCalibration(bool enabled)
        {
            enableAutoCalibration = enabled;
            if (enableDebugLogs)
                Debug.Log($"Auto-calibration: {(enabled ? "ENABLED" : "DISABLED")}");
        }
        
        public void SetDepthMultiplier(float multiplier)
        {
            depthMultiplier = Mathf.Max(0.1f, multiplier);
            if (enableDebugLogs)
                Debug.Log($"Depth multiplier set to {depthMultiplier:F1}x");
        }
        
        public void ResetAutoCalibration()
        {
            maxObservedHeight = float.MinValue;
            minObservedHeight = float.MaxValue;
            hasObservedFullRange = false;
            autoCalibrationTimer = 0f;
            lastAutoCalibrationTime = Time.time;
            
            if (enableDebugLogs)
                Debug.Log("Auto-calibration data reset");
        }
        
        public void ForceAutoCalibration()
        {
            PerformAutoCalibrationAdjustment();
            if (enableDebugLogs)
                Debug.Log("Forced auto-calibration adjustment");
        }
        
        // NEW: Controller movement detection control methods
        public void SetControllerMovementDetection(bool enabled)
        {
            enableControllerMovementDetection = enabled;
            if (enableDebugLogs)
                Debug.Log($"Controller movement detection: {(enabled ? "ENABLED" : "DISABLED")}");
        }
        
        public void SetControllerForwardThreshold(float threshold)
        {
            controllerForwardThreshold = Mathf.Max(0f, threshold);
            if (enableDebugLogs)
                Debug.Log($"Controller forward threshold set to {controllerForwardThreshold:F3}m");
        }
        
        public void SetControllerMovementWeight(float weight)
        {
            controllerMovementWeight = Mathf.Clamp01(weight);
            if (enableDebugLogs)
                Debug.Log($"Controller movement weight set to {controllerMovementWeight:F2}");
        }

        public void ResetCooldowns()
        {
            lastSquatEventTime = -1f;
            lastValidSquatTime = -1f;
            lastPracticeRewardTime = -1f;
            isInSquatPosition = false;
            hasProcessedCurrentSquat = false;
            
            if (enableDebugLogs)
                Debug.Log("All cooldowns and state reset");
        }

        // Additional properties for UI debugging
        public Vector3 LeftControllerPosition => leftController != null ? leftController.position : Vector3.zero;
        public Vector3 RightControllerPosition => rightController != null ? rightController.position : Vector3.zero;
        public Vector3 CameraPosition => xrCamera != null ? xrCamera.position : Vector3.zero;
        public float CurrentVelocity => velocity;
        public bool IsInValidSquat => CurrentDepthNorm >= validThreshold && IsValidSquatForm;
        public float DwellTime => isInSquatPosition ? Time.time - squatEnterTime : 0f;
        public bool IsInBottom => isInSquatPosition;
        
        // NEW: Controller movement properties
        public float LeftControllerForwardMovement => leftControllerForwardMovement;
        public float RightControllerForwardMovement => rightControllerForwardMovement;
        public float CombinedControllerMovement => combinedControllerMovement;
        public bool IsControllerMovementDetected => combinedControllerMovement > controllerForwardThreshold;

        // DEBUG METHOD: Enhanced debugging
        public void DebugSquatDetection()
        {
            if (xrCamera == null) return;
            
            float y = xrCamera.position.y;
            float depth = Mathf.Max(0f, BaselineHeight - y);
            float depthNorm = Mathf.Clamp01(depth / targetDepth);
            
            Debug.Log($"=== ENHANCED SQUAT DEBUG ===");
            Debug.Log($"Validation Mode: Head-Only={headOnlyMode}, IK Validation={enableIKValidation}");
            Debug.Log($"CALIBRATION INFO:");
            Debug.Log($"  Baseline Height: {BaselineHeight:F3}m");
            Debug.Log($"  Current Height: {y:F3}m");
            Debug.Log($"  Height Difference: {y - BaselineHeight:F3}m");
            Debug.Log($"  Target Depth: {targetDepth:F3}m");
            Debug.Log($"  Valid Threshold: {validThreshold:F2} ({validThreshold * 100:F0}%)");
            Debug.Log($"SQUAT DETECTION:");
            Debug.Log($"  Squat Depth: {depth:F3}m");
            Debug.Log($"  Depth Normalized: {depthNorm:F2} ({depthNorm * 100:F0}%)");
            Debug.Log($"  Valid Squat Form: {IsValidSquatForm}");
            Debug.Log($"  Is In Squat Position: {isInSquatPosition}");
            Debug.Log($"  Has Processed Current: {hasProcessedCurrentSquat}");
            Debug.Log($"  Current Dwell Time: {DwellTime:F2}s");
            Debug.Log($"STATE:");
            Debug.Log($"  Is Dodging: {IsDodging}");
            Debug.Log($"  Is On Cooldown: {IsOnCooldown}");
            Debug.Log($"  Has Nearby Threats: {HasNearbyThreats}");
            Debug.Log($"  Require Threats: {requireThreatsForDodge}");
            Debug.Log($"  Can Trigger Dodge: {CanTriggerDodge()}");
            Debug.Log($"AUTO-CALIBRATION:");
            Debug.Log($"  Enabled: {enableAutoCalibration}");
            Debug.Log($"  Depth Multiplier: {depthMultiplier:F1}x");
            Debug.Log($"  Observed Range: {maxObservedHeight - minObservedHeight:F3}m");
            Debug.Log($"  Max Height: {maxObservedHeight:F3}m");
            Debug.Log($"  Min Height: {minObservedHeight:F3}m");
            Debug.Log($"  Has Full Range: {hasObservedFullRange}");
            Debug.Log($"CONTROLLER MOVEMENT:");
            Debug.Log($"  Enabled: {enableControllerMovementDetection}");
            Debug.Log($"  Left Forward: {leftControllerForwardMovement:F3}m");
            Debug.Log($"  Right Forward: {rightControllerForwardMovement:F3}m");
            Debug.Log($"  Combined: {combinedControllerMovement:F3}m");
            Debug.Log($"  Threshold: {controllerForwardThreshold:F3}m");
            Debug.Log($"  Weight: {controllerMovementWeight:F2}");
            Debug.Log($"  Detected: {IsControllerMovementDetected}");
            Debug.Log($"CALIBRATION SHORTCUTS:");
            Debug.Log($"  C = Full calibration with range detection");
            Debug.Log($"  R = Quick recalibrate standing height");
            Debug.Log($"  A = Toggle auto-calibration");
            Debug.Log($"  Z = Reset auto-calibration data");
            Debug.Log($"  X = Force auto-calibration adjustment");
            Debug.Log($"  Shift+Up/Down = Adjust baseline height");
            Debug.Log($"  Ctrl+Up/Down = Adjust target depth");
            Debug.Log($"  Alt+Up/Down = Adjust depth multiplier");
            Debug.Log($"  M = Toggle controller movement detection");
            
            if (leftController != null && rightController != null)
            {
                Debug.Log($"Left Controller: {leftController.position:F3}");
                Debug.Log($"Right Controller: {rightController.position:F3}");
                Debug.Log($"Estimated Knee Angle: {EstimatedKneeAngle:F1}°");
                
                bool handPos = ValidateNaturalHandPosition();
                bool symmetry = ValidateHandSymmetry();
                bool kneeAngle = ValidateKneeAngle();
                Debug.Log($"Validation: HandPos={handPos}, Symmetry={symmetry}, Knee={kneeAngle}");
            }
            
            Debug.Log("==================");
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public void DebugLogThreatStatus()
        {
            Debug.Log($"Threats: {NearbyThreatCount}, Closest: {ClosestThreatDistance:F1}m, Has Threats: {HasNearbyThreats}");
        }
    }
}