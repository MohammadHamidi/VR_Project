using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;

public interface animatable
{
    public UnityEvent type { get; set; }        
    void Animate();
}


public class Player_Direction_Change : MonoBehaviour,animatable
{
    [SerializeField] private Transform playerTranform;
    [SerializeField] private Transform StartPoint;
    [SerializeField] private Transform GoalPoint;
    [SerializeField] private Transform CenterofBridge;
    [SerializeField] private float bridgeWidth;


    // s-------g 
    // ply-start 
    // 
    public float Progress()
    {

        var distance = GoalPoint.position - StartPoint.position;

        var difrence=playerTranform.position - StartPoint.position;

        var progress = Vector3.Magnitude(distance - difrence);
        
        return progress;
    }

    public float offset()
    {
        var maxdif = bridgeWidth / 2;
        var dif=CenterofBridge.position.x-playerTranform.position.x;
        
        return dif / maxdif;
    }


    public UnityEvent type { get; set; }

    public void Animate()
    {
        type.Invoke();
    }
}
