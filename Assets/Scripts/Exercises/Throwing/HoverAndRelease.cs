using UnityEngine;
using DG.Tweening;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(Rigidbody), typeof(XRGrabInteractable))]
public class HoverAndRelease : MonoBehaviour
{
    [Header("Hover Settings")]
    public float hoverAmplitude   = 0.1f;
    public float hoverDuration    = 1f;
    public float rotationDuration = 5f;

    [Header("Release Settings")]
    [Tooltip("Initial velocity multiplier when released")]
    public float releaseVelocityMultiplier = 2f;
    [Tooltip("Use custom gravity strength (0 = no custom gravity, 1 = normal gravity, 0.5 = half gravity)")]
    [Range(0f, 2f)]
    public float customGravityStrength = 0f; // 0 = use Unity gravity, >0 = custom gravity

    private Rigidbody rb;
    private XRGrabInteractable grab;
    private float startY;

    // Gravity state
    private bool useCustomGravity = false;

    void Awake()
    {
        rb   = GetComponent<Rigidbody>();
        grab = GetComponent<XRGrabInteractable>();

        // Start in "hover" mode: disable Unity's built‐in gravity
        rb.useGravity  = false;
        rb.isKinematic = false;

        // Hook up release callback
        grab.selectExited.AddListener(OnRelease);
    }

    void OnEnable()
    {
        startY = transform.position.y;

        // HOVER: Move the Rigidbody (not the Transform) so physics tracks its velocity
        rb
            .DOMoveY(startY + hoverAmplitude, hoverDuration)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine);

        // ROTATION: purely visual, rotate the Transform
        transform
            .DORotate(new Vector3(0, 360, 0), rotationDuration, RotateMode.FastBeyond360)
            .SetLoops(-1, LoopType.Incremental)
            .SetEase(Ease.Linear);
    }

    void OnRelease(SelectExitEventArgs args)
    {
        // 1) Kill any ongoing Rigidbody tween
        DOTween.Kill(rb);

        // 2) Setup gravity system
        if (customGravityStrength > 0f)
        {
            // Use custom gravity strength
            rb.useGravity = false;
            useCustomGravity = true;
        }
        else
        {
            // Use Unity's built-in gravity
            rb.useGravity = true;
            useCustomGravity = false;
        }

        // 3) Give the ball initial velocity based on how it was thrown
        ApplyReleaseVelocity(args);
    }

    void FixedUpdate()
    {
        if (useCustomGravity && customGravityStrength > 0f)
        {
            // Apply custom gravity strength
            rb.AddForce(Physics.gravity * customGravityStrength, ForceMode.Acceleration);
        }
    }

    private void ApplyReleaseVelocity(SelectExitEventArgs args)
    {
        if (args.interactorObject == null) return;

        // Get the velocity from the interactor (hand/controller)
        var interactor = args.interactorObject as XRBaseInteractor;
        if (interactor == null) return;

        // Try to get velocity from the interactor's attach transform or rigidbody
        Vector3 releaseVelocity = Vector3.zero;

        // Method 1: Check if interactor has velocity tracking
        var interactorRigidbody = interactor.GetComponent<Rigidbody>();
        if (interactorRigidbody != null)
        {
            releaseVelocity = interactorRigidbody.velocity;
        }

        // Method 2: Use attach transform velocity if available
        if (releaseVelocity.magnitude < 0.1f && interactor.attachTransform != null)
        {
            var attachRigidbody = interactor.attachTransform.GetComponent<Rigidbody>();
            if (attachRigidbody != null)
            {
                releaseVelocity = attachRigidbody.velocity;
            }
        }

        // Method 3: Fallback - use current ball velocity
        if (releaseVelocity.magnitude < 0.1f)
        {
            releaseVelocity = rb.velocity;
        }

        // Apply velocity multiplier
        if (releaseVelocity.magnitude > 0.1f)
        {
            rb.velocity = releaseVelocity * releaseVelocityMultiplier;
        }
        else
        {
            // Minimum throw velocity if barely moving
            rb.velocity = transform.forward * releaseVelocityMultiplier * 2f;
        }

        Debug.Log($"Ball released with velocity: {rb.velocity.magnitude:F2} m/s");
    }

    void OnDisable()
    {
        DOTween.Kill(rb);
        if (grab != null)
        {
            grab.selectExited.RemoveListener(OnRelease);
        }
    }
}
