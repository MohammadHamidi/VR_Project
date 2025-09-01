using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using Unity.XR.CoreUtils;    // for XROrigin
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
    public bool  enableTilt       = true;
    [Tooltip("Degrees to tilt downward on X-axis")]
    public float tiltAngleX       = 15f;
    [Tooltip("Tween time to reapply orientation on release")]
    public float reorientDuration = 0.5f;

    [SerializeField, Tooltip("If left blank, will use the XR camera at runtime")]
    private Camera _mainCam;
    private XRGrabInteractable _grab;
    private bool _isGrabbed;

    void Awake()
    {
        // If no camera is assigned in Inspector, try to grab it from the XR Origin
        if (_mainCam == null)
        {
            var xrOrigin = FindObjectOfType<XROrigin>();
            if (xrOrigin != null && xrOrigin.Camera != null)
            {
                _mainCam = xrOrigin.Camera;
            }
            else
            {
                // Fallback to Camera.main if no XROrigin or XR camera found
                _mainCam = Camera.main;
                if (_mainCam == null)
                    Debug.LogWarning("BillboardUI: No camera found (XR or Main). Assign one manually if needed.");
            }
        }

        _grab = GetComponent<XRGrabInteractable>();
        _grab.selectEntered.AddListener(_ => OnGrab());
        _grab.selectExited .AddListener(_ => OnRelease());
    }

    void Start()
    {
        if (_mainCam == null)
            return; // nothing to do if we couldn't find a camera

        // Determine horizontal anchor based on enum
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

        // Initial rotation: camera’s Y rotation + optional tilt
        float camY = _mainCam.transform.eulerAngles.y;
        float tilt = enableTilt ? tiltAngleX : 0f;
        transform.rotation = Quaternion.Euler(tilt, camY, 0);
    }

    void LateUpdate()
    {
        if (_isGrabbed || _mainCam == null)
            return;

        // Keep the panel facing the camera horizontally
        Vector3 dir = (_mainCam.transform.position - transform.position).normalized;
        dir.y = 0; // ignore vertical component
        if (dir.sqrMagnitude < 0.001f)
            return;

        Quaternion target = Quaternion.LookRotation(dir);
        if (enableTilt)
            target = target * Quaternion.Euler(tiltAngleX, 0, 0);

        transform.rotation = target;
    }

    void OnGrab()
    {
        _isGrabbed = true;
        DOTween.Kill(transform); // stop any existing tweens
    }

    void OnRelease()
    {
        _isGrabbed = false;
        if (_mainCam == null)
            return;

        Vector3 dir = (_mainCam.transform.position - transform.position).normalized;
        dir.y = 0;
        if (dir.sqrMagnitude < 0.001f)
            return;

        Quaternion target = Quaternion.LookRotation(dir);
        if (enableTilt)
            target = target * Quaternion.Euler(tiltAngleX, 0, 0);

        transform.DORotateQuaternion(target, reorientDuration)
                 .SetEase(Ease.OutQuad);
    }
}
