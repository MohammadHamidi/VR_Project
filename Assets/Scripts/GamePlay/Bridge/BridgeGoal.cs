using UnityEngine;
using UnityEngine.Events;

public class BridgeGoal : MonoBehaviour
{
    [Header("Goal Settings")]
    [SerializeField] private string nextSceneName = "CombatScene";
    [SerializeField] private bool requireGroundedPlayer = true;
    [SerializeField] private float completionDelay = 1f;

    [Header("Player Detection")]
    [SerializeField] private LayerMask playerLayer = -1;
    [SerializeField] private string[] playerTags = { "Player", "MainCamera", "XRCamera" };
    [SerializeField] private Transform specificPlayerTransform; // Assign manually if needed

    [Header("Visual Feedback")]
    [SerializeField] private ParticleSystem completionEffect;
    [SerializeField] private AudioClip completionSound;

    [Header("Events")]
    public UnityEvent OnGoalReached;
    public UnityEvent OnTransitionStart;

    private bool goalReached = false;
    private AudioSource audioSource;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && completionSound != null)
            audioSource = gameObject.AddComponent<AudioSource>();

        // Auto-find player transform if not assigned
        if (specificPlayerTransform == null)
            specificPlayerTransform = FindPlayerTransform();
    }

    void OnTriggerEnter(Collider other)
    {
        if (goalReached) return;

        // Check if it's the player
        if (IsPlayerCollider(other))
        {
            if (requireGroundedPlayer && !IsPlayerGrounded(other))
                return;

            TriggerGoalCompletion();
        }
    }

    private Transform FindPlayerTransform()
    {
        // Try to find main camera first
        if (Camera.main != null)
            return Camera.main.transform;

        // Look for cameras with player tags
        var cameras = FindObjectsOfType<Camera>();
        foreach (var cam in cameras)
        {
            foreach (var tag in playerTags)
            {
                if (cam.CompareTag(tag))
                    return cam.transform;
            }
        }

        // Look for any GameObject with player tags
        foreach (var tag in playerTags)
        {
            var playerObj = GameObject.FindGameObjectWithTag(tag);
            if (playerObj != null)
                return playerObj.transform;
        }

        // Try to find XR Origin (common in VR setups)
        var xrOrigins = FindObjectsOfType<Transform>();
        foreach (var transform in xrOrigins)
        {
            if (transform.name.ToLower().Contains("xr") && 
                (transform.name.ToLower().Contains("origin") || transform.name.ToLower().Contains("rig")))
            {
                // Look for camera in XR Origin hierarchy
                var camera = transform.GetComponentInChildren<Camera>();
                if (camera != null)
                    return camera.transform;
            }
        }

        Debug.LogWarning("BridgeGoal: Could not find player transform automatically. Please assign manually.");
        return null;
    }

    private bool IsPlayerCollider(Collider other)
    {
        // Check against manually assigned player transform
        if (specificPlayerTransform != null && other.transform == specificPlayerTransform)
            return true;

        // Check layer mask
        if (((1 << other.gameObject.layer) & playerLayer) == 0)
            return false;

        // Check for player tags
        foreach (var tag in playerTags)
        {
            if (other.CompareTag(tag))
                return true;
        }

        // Check if it's the main camera
        if (other.transform == Camera.main?.transform)
            return true;

        // Check if collider has a Camera component
        if (other.GetComponent<Camera>() != null)
            return true;

        // Check if collider is child of known player transforms
        if (specificPlayerTransform != null && other.transform.IsChildOf(specificPlayerTransform))
            return true;

        // Check if collider is child of main camera
        if (Camera.main != null && other.transform.IsChildOf(Camera.main.transform))
            return true;

        // Fallback: check for common VR component names
        if (HasVRComponents(other.gameObject))
            return true;

        return false;
    }

    private bool HasVRComponents(GameObject obj)
    {
        // Check for common VR-related component names
        var components = obj.GetComponents<MonoBehaviour>();
        foreach (var component in components)
        {
            if (component == null) continue;
            
            string typeName = component.GetType().Name.ToLower();
            if (typeName.Contains("xr") || 
                typeName.Contains("vr") || 
                typeName.Contains("headset") ||
                typeName.Contains("hmd") ||
                typeName.Contains("tracked"))
            {
                return true;
            }
        }

        // Check object name patterns
        string objName = obj.name.ToLower();
        return objName.Contains("camera") || 
               objName.Contains("head") || 
               objName.Contains("player") ||
               objName.Contains("xr") ||
               objName.Contains("vr");
    }

    private bool IsPlayerGrounded(Collider other)
    {
        if (!requireGroundedPlayer) return true;

        // Get the lowest point of the collider for ground check
        var rayOrigin = other.bounds.min;
        rayOrigin.y = other.transform.position.y;
        
        var rayDirection = Vector3.down;
        var maxDistance = 2f;

        // Perform raycast to check for ground
        RaycastHit hit;
        if (Physics.Raycast(rayOrigin, rayDirection, out hit, maxDistance))
        {
            // Check if hit object is ground (not the bridge itself)
            return !hit.collider.transform.IsChildOf(transform.root);
        }

        return false;
    }

    private void TriggerGoalCompletion()
    {
        goalReached = true;
        OnGoalReached?.Invoke();

        Debug.Log("Bridge Goal Reached!");

        // Play effects
        if (completionEffect != null)
            completionEffect.Play();

        if (audioSource != null && completionSound != null)
            audioSource.PlayOneShot(completionSound);

        // Start transition after delay
        StartCoroutine(DelayedTransition());
    }

    private System.Collections.IEnumerator DelayedTransition()
    {
        yield return new WaitForSeconds(completionDelay);
        
        OnTransitionStart?.Invoke();
        
        // Load next scene
        var transitionManager = FindObjectOfType<SceneTransitionManager>();
        if (transitionManager != null)
        {
            transitionManager.LoadScene(nextSceneName);
        }
        else
        {
            // Fallback to direct scene loading
            if (Application.CanStreamedLevelBeLoaded(nextSceneName))
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(nextSceneName);
            }
            else
            {
                Debug.LogError($"BridgeGoal: Scene '{nextSceneName}' not found in Build Settings!");
            }
        }
    }

    private void OnDrawGizmos()
    {
        var trigger = GetComponent<BoxCollider>();
        if (trigger != null && trigger.isTrigger)
        {
            Gizmos.color = goalReached ? Color.blue : Color.green;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(trigger.center, trigger.size);
            
            // Draw arrow pointing to goal
            Gizmos.color = Color.yellow;
            var center = transform.TransformPoint(trigger.center);
            var forward = transform.forward * 0.5f;
            Gizmos.DrawRay(center, forward);
            Gizmos.DrawRay(center + forward, Quaternion.AngleAxis(-30, transform.up) * -forward * 0.3f);
            Gizmos.DrawRay(center + forward, Quaternion.AngleAxis(30, transform.up) * -forward * 0.3f);
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Show ground check visualization
        if (requireGroundedPlayer)
        {
            Gizmos.color = Color.red;
            var center = transform.position;
            Gizmos.DrawRay(center, Vector3.down * 2f);
        }
    }

    // Public methods for external control
    public void SetNextScene(string sceneName)
    {
        nextSceneName = sceneName;
    }

    public void SetPlayerTransform(Transform playerTransform)
    {
        specificPlayerTransform = playerTransform;
    }

    public void ForceGoalCompletion()
    {
        if (!goalReached)
            TriggerGoalCompletion();
    }
}