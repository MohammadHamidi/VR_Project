using UnityEngine;

[DefaultExecutionOrder(100)]
public class EditorWASDController : MonoBehaviour
{
    public bool enableOutsideEditor = false;

    [Header("Move")]
    public float moveSpeed = 2.5f;
    public float sprintMultiplier = 3f;
    public float verticalSpeed = 2.0f; // Space / Ctrl when noclip
    public bool noclip = false;        // <-- turn OFF to collide with bridge

    [Header("Look")]
    public bool holdRightMouseToLook = true;
    public float mouseSensitivity = 2.0f;
    public float pitchMin = -85f;
    public float pitchMax = 85f;

    [Header("QoL")]
    public bool lockCursorWhileLooking = true;
    public KeyCode toggleKey = KeyCode.F1;

    float _yaw, _pitch;
    bool _enabledRuntime = true;
    CharacterController _cc;
    Transform _cam;
    float _verticalVel = 0f;

    void Awake()
    {
#if !UNITY_EDITOR
        _enabledRuntime = enableOutsideEditor;
#else
        _enabledRuntime = true;
#endif
        _cam = GetComponentInChildren<Camera>() ? GetComponentInChildren<Camera>().transform : transform;
        _cc = GetComponent<CharacterController>();

        var e = transform.eulerAngles;
        _yaw = e.y; _pitch = e.x;
        if (noclip && _cc != null) _cc.enabled = false;
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey)) _enabledRuntime = !_enabledRuntime;
        if (!_enabledRuntime) { ReleaseCursor(); return; }

        // ---- Look ----
        bool looking = !holdRightMouseToLook || Input.GetMouseButton(1);
        if (looking)
        {
            float mx = Input.GetAxis("Mouse X") * mouseSensitivity;
            float my = Input.GetAxis("Mouse Y") * mouseSensitivity;
            _yaw += mx; _pitch = Mathf.Clamp(_pitch - my, pitchMin, pitchMax);
            transform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
            if (lockCursorWhileLooking) LockCursor();
        }
        else ReleaseCursor();

        // ---- Move ----
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector3 planar = (transform.right * h + transform.forward * v).normalized;
        float speed = moveSpeed * (Input.GetKey(KeyCode.LeftShift) ? sprintMultiplier : 1f);

        if (noclip || _cc == null)
        {
            // Free-fly
            float up = (Input.GetKey(KeyCode.Space) ? 1f : 0f) + (Input.GetKey(KeyCode.LeftControl) ? -1f : 0f);
            Vector3 delta = (planar * speed + Vector3.up * up * verticalSpeed) * Time.deltaTime;
            transform.position += delta;
        }
        else
        {
            // Collide with world
            if (_cc.isGrounded && _verticalVel < 0) _verticalVel = -2f; // keep grounded
            _verticalVel += Physics.gravity.y * Time.deltaTime;

            Vector3 move = planar * speed + Vector3.up * _verticalVel;
            _cc.Move(move * Time.deltaTime);
        }
    }

    void OnDisable() => ReleaseCursor();
    void LockCursor(){ if (Cursor.lockState != CursorLockMode.Locked){ Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; } }
    void ReleaseCursor(){ if (Cursor.lockState == CursorLockMode.Locked){ Cursor.lockState = CursorLockMode.None; Cursor.visible = true; } }
}
