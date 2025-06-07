using UnityEngine;


using UnityEngine;
using UnityEngine;

public class TargetRing : MonoBehaviour
{
    public ParticleSystem hitVFX;
    public AudioSource hitSfx;
    public System.Action<TargetRing> OnHit;

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Throwable")) return;
        DoHit();
    }

    // new helper method
    public void SimulateHit()
    {
        DoHit();
    }

    void DoHit()
    {
        hitVFX?.Play();
        // hitSfx?.Play();
        OnHit?.Invoke(this);
        // gameObject.SetActive(false);
    }
}