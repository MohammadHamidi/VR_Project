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
        if (_grab != null)
        {
            _grab.selectEntered.AddListener(OnGrab);
        }
        else
        {
            Debug.LogError("GrabRespawner: XRGrabInteractable component not found!");
        }
    }

    void OnGrab(SelectEnterEventArgs args)
    {
        if (spawner == null)
        {
            Debug.LogWarning("GrabRespawner: Spawner reference is null. Cannot spawn ball.");
            return;
        }

        // Check if spawner already has a ball before spawning
        if (spawner.GetCurrentBall() != null)
        {
            Debug.LogWarning("GrabRespawner: Ball already exists, not spawning another one.");
            return;
        }

        // Spawn the next ball
        spawner.SpawnBall();
    }

    void OnDestroy()
    {
        // Safe cleanup with null check
        if (_grab != null)
        {
            _grab.selectEntered.RemoveListener(OnGrab);
        }
    }
}