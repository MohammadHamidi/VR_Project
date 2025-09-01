using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

public class HandController : MonoBehaviour
{
    [FormerlySerializedAs("_select")] [SerializeField] private InputActionReference ActivateReference;
    [FormerlySerializedAs("_activate")] [FormerlySerializedAs("_pinch")] [SerializeField] private InputActionReference SelectReference; 
    [SerializeField] private Animator _animator;
    
    private float gripValue = 0f;
    private float triggerValue = 0f;

    private const string GRIP_PARAM = "Grip";
    private const string TRIGGER_PARAM = "Trigger";

    private void Update()
    {
        AnimatePinch();
        AnimateTrigger();
    }

    private void AnimateTrigger()
    {
        if (ActivateReference?.action != null)
        {
            triggerValue = ActivateReference.action.ReadValue<float>();
            _animator.SetFloat(GRIP_PARAM, triggerValue);
        }
    }

    private void AnimatePinch()
    {
        if (SelectReference?.action != null)
        {
            gripValue = SelectReference.action.ReadValue<float>();
            _animator.SetFloat(TRIGGER_PARAM, gripValue);
        }
    }
}