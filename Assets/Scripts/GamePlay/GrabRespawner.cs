using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(XRGrabInteractable))]
public class GrabRespawner : MonoBehaviour
{
    [Tooltip("Assigned by BallSpawner when instantiating")]
    public BallSpawner spawner;

    XRGrabInteractable _grab;

    void Awake()
    {
        _grab = GetComponent<XRGrabInteractable>();
        _grab.selectEntered.AddListener(OnGrab);
    }

    void OnGrab(SelectEnterEventArgs args)
    {
        // Immediately spawn the next ball
        if (spawner != null)
            spawner.SpawnBall();
    }

    void OnDestroy()
    {
        _grab.selectEntered.RemoveListener(OnGrab);
    }
}