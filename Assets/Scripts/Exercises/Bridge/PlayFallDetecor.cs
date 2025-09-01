using System;
using System.Collections;
using System.Collections.Generic;
using GamePlay.Bridge;
using UnityEngine;

public class PlayFallDetecor : MonoBehaviour
{
    

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Player")
        {
            BridgeStageEventHandler.OnPlayerFallCalled();
        }
    }
}
