using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class MultiplyVeocity : MonoBehaviour
{
    [SerializeField]private TextMeshProUGUI _textMeshProUGUI;

    private float multiplier = 1f;
    public static Action OnMultiplyVelocity;

    private void Start()
    {
        _textMeshProUGUI.text = multiplier.ToString();
    }


    private void Update()
    {
        if (Input.GetKey(KeyCode.V))
        {
            OnCLickButton();
        }
    }

    public void OnCLickButton()
    {
        OnMultiplyVelocity?.Invoke();
        multiplier *= 2;
        _textMeshProUGUI.text = multiplier.ToString();
    }

}
