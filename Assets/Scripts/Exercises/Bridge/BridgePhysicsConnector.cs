using UnityEngine;

public class BridgePhysicsConnector : IPhysicsConnector
{
    public void ConnectComponents(IBridgeComponent componentA, IBridgeComponent componentB, BridgeConfiguration config)
    {
        if (componentA?.GameObject == null || componentB?.GameObject == null)
        {
            Debug.LogWarning("Cannot connect components: one or both components are null");
            return;
        }

        var rbA = componentA.GameObject.GetComponent<Rigidbody>();
        var rbB = componentB.GameObject.GetComponent<Rigidbody>();
        
        if (rbA == null || rbB == null)
        {
            Debug.LogWarning("Cannot connect components: missing Rigidbody components");
            return;
        }

        var joint = componentA.GameObject.AddComponent<HingeJoint>();
        joint.connectedBody = rbB;
        joint.anchor = new Vector3(0, 0, config.PlankLength * 0.5f);
        joint.connectedAnchor = new Vector3(0, 0, -config.PlankLength * 0.5f);
        joint.axis = Vector3.right;
        
        joint.useSpring = true;
        var spring = new JointSpring
        {
            spring = config.jointSpring,
            damper = config.jointDamper,
            targetPosition = 0f
        };
        joint.spring = spring;

        joint.useLimits = true;
        joint.limits = new JointLimits { min = -5f, max = 5f };
    }

    public void ConnectToAnchor(IBridgeComponent component, IBridgeComponent anchor, Vector3 anchorPoint, BridgeConfiguration config)
    {
        if (component?.GameObject == null || anchor?.GameObject == null)
        {
            Debug.LogWarning("Cannot connect to anchor: one or both components are null");
            return;
        }

        var componentRb = component.GameObject.GetComponent<Rigidbody>();
        var anchorRb = anchor.GameObject.GetComponent<Rigidbody>();
        
        if (componentRb == null || anchorRb == null)
        {
            Debug.LogWarning("Cannot connect to anchor: missing Rigidbody components");
            return;
        }

        var joint = component.GameObject.AddComponent<SpringJoint>();
        joint.connectedBody = anchorRb;
        joint.anchor = anchorPoint;
        joint.spring = config.jointSpring * 2f;
        joint.damper = config.jointDamper;
        joint.maxDistance = 0.1f;
    }
}