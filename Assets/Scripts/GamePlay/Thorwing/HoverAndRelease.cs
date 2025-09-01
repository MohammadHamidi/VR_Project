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
    private Rigidbody rb;
    private XRGrabInteractable grab;
    private float startY;

    // When true, we apply half‐strength gravity manually in FixedUpdate
    private bool applyCustomGravity = false;

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

        // 2) Stop using built‐in gravity and begin applying custom half‐gravity
        rb.useGravity = true;
        applyCustomGravity = true;

        // 3) Give the ball a strong initial velocity so it flies off quickly
       
    }

    // void FixedUpdate()
    // {
    //     if (applyCustomGravity)
    //     {
    //         // Apply half-strength gravity manually (ForceMode.Acceleration ignores mass)
    //         rb.AddForce(Physics.gravity *0.504f, ForceMode.Acceleration);
    //     }
    // }

    void OnDisable()
    {
        DOTween.Kill(rb);
        grab.selectExited.RemoveListener(OnRelease);
    }
}
