using UnityEngine;
using DG.Tweening;
using UnityEngine.XR.Interaction.Toolkit;


using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(Rigidbody), typeof(XRGrabInteractable))]
public class HoverAndRelease : MonoBehaviour
{
    [Header("Hover Settings")]
    public float hoverAmplitude   = 0.1f;
    public float hoverDuration    = 1f;
    public float rotationDuration = 5f;

    Rigidbody rb;
    XRGrabInteractable grab;
    float startY;

    void Awake()
    {
        rb   = GetComponent<Rigidbody>();
        grab = GetComponent<XRGrabInteractable>();

        // Start in “hover” mode: no gravity
        rb.useGravity  = false;
        rb.isKinematic = false;

        // When the user *releases* the ball, enable gravity
        grab.selectExited.AddListener(OnRelease);
    }

    void OnEnable()
    {
        startY = transform.position.y;

        // vertical hover tween
        transform
            .DOMoveY(startY + hoverAmplitude, hoverDuration)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine);

        // slow rotation
        transform
            .DORotate(new Vector3(0, 360, 0), rotationDuration, RotateMode.FastBeyond360)
            .SetLoops(-1, LoopType.Incremental)
            .SetEase(Ease.Linear);
    }

    void OnRelease(SelectExitEventArgs args)
    {
        // stop hovering
        DOTween.Kill(transform);
        // let physics take over
        rb.useGravity = true;
    }

    void OnDisable()
    {
        // clean up
        DOTween.Kill(transform);
        grab.selectExited.RemoveListener(OnRelease);
    }
}