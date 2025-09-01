using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class PlayerBridgeStatusUI:MonoBehaviour
    {
        
        Player_Direction_Change player_Direction_Change;
        private Slider Progress;
        private Slider Offset;


        private void Update()
        {
            if (player_Direction_Change!=null && Progress!=null && Offset!=null)
            {
                Progress.value=player_Direction_Change.Progress();
                Offset.value = player_Direction_Change.offset();
            }
        }
    }
}