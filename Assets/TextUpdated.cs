using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class TextUpdated : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI TextMeshProUGUI;

    [SerializeField] private XRGrabInteractable _xrGrabInteractable;
     private void OnValidate()
    {
        if (TextMeshProUGUI!=null && _xrGrabInteractable!=null)
        {
            TextMeshProUGUI.text = _xrGrabInteractable.movementType.ToString();
        }
    }
}
