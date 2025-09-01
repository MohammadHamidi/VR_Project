using UnityEngine;

public class TargetRingTest : MonoBehaviour
{
    [Tooltip("Drag in your 3 (or however many) TargetRing instances here")]
    public TargetRing[] targets;

    void Update()
    {
        // press 1,2,3 to simulate hit on ring[0], ring[1], ring[2]
        if (Input.GetKeyDown(KeyCode.Alpha1) && targets.Length > 0)
            targets[0].SimulateHit();

        if (Input.GetKeyDown(KeyCode.Alpha2) && targets.Length > 1)
            targets[1].SimulateHit();

        if (Input.GetKeyDown(KeyCode.Alpha3) && targets.Length > 2)
            targets[2].SimulateHit();
    }
}