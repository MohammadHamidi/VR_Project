using UnityEngine;
using UnityEngine.UI;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class RingPanelItem : MonoBehaviour
{
    [Tooltip("Yellow (unhit) state sprite")]
    public Sprite unhitSprite;
    [Tooltip("Green (hit) state sprite")]
    public Sprite hitSprite;

    Image _image;

    void Awake()
    {
        _image = GetComponent<Image>();
        // start in unhit state
        _image.sprite = unhitSprite;
    }

    /// <summary>
    /// Call this when the associated TargetRing fires its OnHit.
    /// </summary>
    public void SetHit(bool isHit)
    {
        _image.sprite = isHit ? hitSprite : unhitSprite;
    }
}
