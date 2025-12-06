using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LockHeadWorldYaw : MonoBehaviour
{
    public Transform headBone;
    private Quaternion initialWorldRotation;

    void Start()
    {
        // Save the original world-space rotation
        if (headBone != null)
            initialWorldRotation = headBone.rotation;
    }

    void LateUpdate()
    {
        if (headBone != null)
        {
            // Get the current rotation in world space
            Vector3 currentEuler = headBone.rotation.eulerAngles;
            Vector3 initialEuler = initialWorldRotation.eulerAngles;

            // Lock Y to the original value, allow current X and Z
            Vector3 lockedEuler = new Vector3(
                currentEuler.x,     // keep current pitch (nodding)
                initialEuler.y + 12.0f,     // lock yaw
                currentEuler.z      // keep current roll (tilting)
            );

            headBone.rotation = Quaternion.Euler(lockedEuler);
        }
    }
}
