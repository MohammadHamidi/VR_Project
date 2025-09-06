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

        [Header("Squat Detection")]
        [SerializeField] private float targetDepth = 0.30f; // meters
        [SerializeField] private float validThreshold = 0.8f; // depthNorm
        [SerializeField] private float dwellMin = 0.3f;
        [SerializeField] private float dwellMax = 1.2f;
        [SerializeField] private float dodgeDuration = 0.5f;
        [SerializeField] private float cooldownDuration = 0.2f;

        [Header("IK Body Proportions")]
        [SerializeField] private float shoulderWidth = 0.4f;
        [SerializeField] private float armLength = 0.6f;
        [SerializeField] private float torsoLength = 0.5f;
        [SerializeField] private float thighLength = 0.4f;
        [SerializeField] private float neckLength = 0.15f;

        [Header("Enhanced Validation")]
        [SerializeField] private float maxHandAsymmetry = 0.3f;
        [SerializeField] private float maxNaturalReach = 1.0f;
        [SerializeField] private float coordinationThreshold = 0.15f;
        [SerializeField] private float minKneeAngle = 60f;
        [SerializeField] private float maxKneeAngle = 120f;
        [SerializeField] private float maxSpineAngle = 25f;

        [Header("Quality Assessment")]
        [SerializeField] private float perfectQualityThreshold = 85f;
        [SerializeField] private float smoothingFactor = 0.15f;
        [SerializeField] private float velocitySmoothing = 0.2f;

        [Header("Dodge Feedback")]
        [SerializeField] private AudioClip dodgeSound;
        [SerializeField] private AudioClip perfectSquatSound;
        [SerializeField] private ParticleSystem dodgeEffect;
        [SerializeField] private ParticleSystem perfectSquatEffect;

        // Public Properties
        public bool IsDodging { get; private set; }
        public float CurrentSquatDepth { get; private set; }
        public float CurrentDepthNorm { get; private set; }
        public bool IsOnCooldown { get; private set; }
        public float BaselineHeight { get; private set; }
        public Vector3 EstimatedHipPosition { get; private set; }
        public float EstimatedKneeAngle { get; private set; }
        public bool IsValidSquatForm { get; private set; }

        private AudioSource _audioSource;
        private Coroutine _dodgeCoroutine;
        private Coroutine _cooldownCoroutine;
        
        // Enhanced tracking data
        private float smoothedY;
        private float velocity;
        private bool inBottom;
        private float bottomEnterTime;
        
        // IK baseline positions
        private Vector3 baselineShoulderCenter;
        private Vector3 baselineHipPosition;
        private float baselineHandHeadDistance;
        
        // Controller tracking
        private Vector3 leftControllerVelocity;
        private Vector3 rightControllerVelocity;
        private Vector3 lastLeftPos;
        private Vector3 lastRightPos;

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
                        // Find left controller transform in scene
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
                        // Find right controller transform in scene
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
            UpdateControllerVelocities();
            CheckEnhancedSquatPosition();
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

        private void CheckEnhancedSquatPosition()
        {
            if (xrCamera == null) return;

            // Original head tracking
            float y = xrCamera.position.y;
            smoothedY = Mathf.Lerp(smoothedY, y, smoothingFactor);
            float depth = Mathf.Max(0f, BaselineHeight - smoothedY);
            float depthNorm = Mathf.Clamp01(depth / targetDepth);
            
            velocity = Mathf.Lerp(velocity, (y - smoothedY) / Time.deltaTime, velocitySmoothing);

            CurrentSquatDepth = depth;
            CurrentDepthNorm = depthNorm;

            // IK-based pose estimation
            EstimateBodyPose();
            
            // Enhanced validation with IK
            bool enhancedValidation = ValidateSquatWithIK();
            IsValidSquatForm = enhancedValidation;

            CombatEvents.OnPlayerSquatDepthChanged?.Invoke(CurrentSquatDepth);

            // Enhanced bottom detection
            if (!inBottom && depthNorm >= validThreshold && enhancedValidation)
            {
                inBottom = true;
                bottomEnterTime = Time.time;
            }
            else if (inBottom && (depthNorm < validThreshold - 0.1f || !enhancedValidation))
            {
                float dwell = Time.time - bottomEnterTime;
                if (dwell >= dwellMin && dwell <= dwellMax)
                {
                    // Enhanced quality calculation
                    float quality = CalculateEnhancedSquatQuality(depthNorm, dwell);
                    
                    CombatEvents.OnValidSquat?.Invoke(depthNorm, quality);
                    PlaySquatFeedback(quality >= perfectQualityThreshold);
                    
                    Debug.Log($"Valid enhanced squat: Depth={depthNorm:F2}, Quality={quality:F1}, " +
                             $"Dwell={dwell:F2}s, KneeAngle={EstimatedKneeAngle:F1}°");
                }
                inBottom = false;
            }

            // Trigger dodge with enhanced validation
            if (CurrentSquatDepth >= targetDepth * validThreshold && enhancedValidation && 
                !IsDodging && !IsOnCooldown)
            {
                TriggerDodge();
            }
        }

        private void EstimateBodyPose()
        {
            if (leftController == null || rightController == null) return;

            // Estimate shoulder center
            Vector3 shoulderCenter = (leftController.position + rightController.position) / 2f;
            
            // Estimate hip position (torso length below shoulder center)
            EstimatedHipPosition = shoulderCenter + Vector3.down * torsoLength;
            
            // Estimate knee angle based on hip drop
            float hipDrop = baselineHipPosition.y - EstimatedHipPosition.y;
            EstimatedKneeAngle = CalculateKneeAngleFromHipDrop(hipDrop);
        }

        private float CalculateKneeAngleFromHipDrop(float hipDrop)
        {
            // Simple geometric calculation for knee angle based on hip displacement
            float normalizedDrop = Mathf.Clamp01(hipDrop / targetDepth);
            
            // Knee angle ranges from ~180° (straight) to ~60° (deep squat)
            return Mathf.Lerp(180f, 60f, normalizedDrop);
        }

        private bool ValidateSquatWithIK()
        {
            if (leftController == null || rightController == null) return false;

            // 1. Check coordinated movement
            if (!IsCoordinatedMovement()) return false;

            // 2. Validate hand symmetry
            if (!ValidateHandSymmetry()) return false;

            // 3. Check natural hand positioning
            if (!ValidateNaturalHandPosition()) return false;

            // 4. Validate estimated knee angle
            if (!ValidateKneeAngle()) return false;

            // 5. Check for cheating attempts
            if (!ValidateAgainstCheating()) return false;

            return true;
        }

        private bool IsCoordinatedMovement()
        {
            float headVelocityY = velocity;
            float leftHandVelocityY = leftControllerVelocity.y;
            float rightHandVelocityY = rightControllerVelocity.y;

            // Check if all parts are moving in the same direction (down for squat)
            bool headMovingDown = headVelocityY < -coordinationThreshold;
            bool handsMovingDown = leftHandVelocityY < -coordinationThreshold && 
                                   rightHandVelocityY < -coordinationThreshold;

            // During squat, all should move down together
            return !headMovingDown || handsMovingDown;
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

        private bool ValidateAgainstCheating()
        {
            // Detect head-only ducking (head drops but hands stay high)
            float headDrop = BaselineHeight - xrCamera.position.y;
            float avgHandDrop = ((baselineShoulderCenter.y - leftController.position.y) + 
                                (baselineShoulderCenter.y - rightController.position.y)) / 2f;

            if (headDrop > 0.1f && avgHandDrop < 0.05f)
            {
                Debug.Log("Cheating detected: Head ducking without body movement");
                return false;
            }

            // Detect excessive forward lean (bending over vs squatting)
            Vector3 shoulderCenter = (leftController.position + rightController.position) / 2f;
            float forwardLean = Vector3.Angle(Vector3.up, (xrCamera.position - shoulderCenter).normalized);
            
            if (forwardLean > maxSpineAngle)
            {
                Debug.Log("Invalid form: Excessive forward lean detected");
                return false;
            }

            return true;
        }

        private float CalculateEnhancedSquatQuality(float depthNorm, float dwell)
        {
            // Original quality factors
            float tempoScore = Mathf.Clamp01((0.7f - Mathf.Abs(dwell - 0.7f)) / 0.7f);
            float stabilityScore = Mathf.Clamp01(1f - Mathf.Abs(velocity) * 0.02f);
            
            // Enhanced IK-based factors
            float formScore = ValidateSquatWithIK() ? 1f : 0.5f;
            float symmetryScore = 1f - (Mathf.Abs(leftController.position.y - rightController.position.y) / maxHandAsymmetry);
            symmetryScore = Mathf.Clamp01(symmetryScore);
            
            float coordinationScore = IsCoordinatedMovement() ? 1f : 0.6f;

            // Weighted quality calculation
            return 30f * depthNorm + 
                   15f * tempoScore + 
                   15f * stabilityScore + 
                   20f * formScore + 
                   10f * symmetryScore + 
                   10f * coordinationScore;
        }

        public void CalibrateStandingHeight()
        {
            if (xrCamera != null)
            {
                BaselineHeight = xrCamera.position.y;
                smoothedY = BaselineHeight;
                Debug.Log($"Standing height calibrated to: {BaselineHeight:F2}m");
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
                
                Debug.Log($"Body proportions calibrated - Hip baseline: {baselineHipPosition:F2}, " +
                         $"Hand-head distance: {baselineHandHeadDistance:F2}m");
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
                
                Debug.Log($"Enhanced calibration complete over {sampleTime}s - " +
                         $"Height: {BaselineHeight:F2}m, Hip: {baselineHipPosition:F2}");
            }
        }

        // Existing methods remain the same
        private void TriggerDodge()
        {
            if (_dodgeCoroutine != null)
                StopCoroutine(_dodgeCoroutine);
            
            _dodgeCoroutine = StartCoroutine(DodgeRoutine());
        }

        private IEnumerator DodgeRoutine()
        {
            IsDodging = true;
            CombatEvents.OnPlayerDodge?.Invoke();
            PlayDodgeFeedback();

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

        private void PlaySquatFeedback(bool isPerfect)
        {
            if (_audioSource != null)
            {
                AudioClip soundToPlay = isPerfect && perfectSquatSound != null ? perfectSquatSound : dodgeSound;
                if (soundToPlay != null)
                {
                    _audioSource.pitch = isPerfect ? 1.2f : 1f;
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
    }
}