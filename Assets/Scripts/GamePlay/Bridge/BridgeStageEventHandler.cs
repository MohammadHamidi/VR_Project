using System;
using UnityEngine;
namespace GamePlay.Bridge
{
    public class BridgeStageEventHandler:MonoBehaviour
    {
        public static event Action onPlayerFall;

        public static void OnPlayerFallCalled()
        {
            onPlayerFall?.Invoke();
        }

    }
}