using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using DG.Tweening;

[RequireComponent(typeof(Canvas), typeof(XRGrabInteractable))]
public class BillboardUI : MonoBehaviour
{
    public enum AnchorPosition { TopLeft, TopCenter, TopRight }

    [Header("Anchor Settings")]
    public AnchorPosition anchor = AnchorPosition.TopRight;
    [Range(0.5f, 1f), Tooltip("How far from edge (0=left,1=right or bottom=0,top=1)")]
    public float horizontalViewport = 0.9f;
    [Range(0.5f, 1f), Tooltip("Vertical viewport position (bottom=0, top=1)")]
    public float verticalViewport   = 0.9f;
    [Tooltip("Distance in meters from the camera")]
    public float distanceFromCamera = 2f;

    [Header("Tilt Settings")]
    public bool  enableTilt   = true;
    [Tooltip("Degrees to tilt downward on X-axis")]
    public float tiltAngleX   = 15f;
    [Tooltip("Tween time to reapply orientation on release")]
    public float reorientDuration = 0.5f;

    [SerializeField] Camera _mainCam;
    XRGrabInteractable _grab;
    bool _isGrabbed;

    void Awake()
    {
        
        _grab    = GetComponent<XRGrabInteractable>();
        _grab.selectEntered.AddListener(_ => OnGrab());
        _grab.selectExited .AddListener(_ => OnRelease());
    }

    void Start()
    {
        // Determine horizontal anchor based on enum if not customizing:
        float h = horizontalViewport;
        switch (anchor)
        {
            case AnchorPosition.TopLeft:   h = 1f - horizontalViewport; break;
            case AnchorPosition.TopCenter: h = 0.5f;                   break;
            case AnchorPosition.TopRight:  h = horizontalViewport;      break;
        }

        // Compute world position from viewport coords
        Vector3 vp = new Vector3(h, verticalViewport, distanceFromCamera);
        transform.position = _mainCam.ViewportToWorldPoint(vp);

        // Initial rotation: camera Y + optional tilt
        float camY = _mainCam.transform.eulerAngles.y;
        float tilt = enableTilt ? tiltAngleX : 0f;
        transform.rotation = Quaternion.Euler(tilt, camY, 0);
    }

    void LateUpdate()
    {
        if (_isGrabbed) return;

        Vector3 dir = (_mainCam.transform.position - transform.position).normalized;
        dir.y = 0;
        if (dir.sqrMagnitude < 0.001f) return;

        Quaternion target = Quaternion.LookRotation(dir);
        if (enableTilt)
            target = target * Quaternion.Euler(tiltAngleX, 0, 0);

        transform.rotation = target;
    }

    void OnGrab()
    {
        _isGrabbed = true;
        DOTween.Kill(transform);
    }

    void OnRelease()
    {
        _isGrabbed = false;

        Vector3 dir = (_mainCam.transform.position - transform.position).normalized;
        dir.y = 0;
        if (dir.sqrMagnitude < 0.001f) return;

        Quaternion target = Quaternion.LookRotation(dir);
        if (enableTilt)
            target = target * Quaternion.Euler(tiltAngleX, 0, 0);

        transform.DORotateQuaternion(target, reorientDuration)
                 .SetEase(Ease.OutQuad);
    }
}
