using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotateTowardsTarget : MonoBehaviour
{
    [Header("Target to face")]
    public Transform target;

    [Header("Rotation Settings")]
    public float rotationSpeed = 2f; // how fast it rotates
    public float maxYawAngle = 45f;  // optional limit from initial facing

    private Quaternion initialRotation;

    void Start()
    {
        // Save the original rotation
        initialRotation = transform.rotation;
    }

    void LateUpdate()
    {
        if (target == null) return;

        // Get direction to target in world space
        Vector3 direction = target.position - transform.position;
        direction.y = 0; // ignore pitch, only rotate on Y

        if (direction.sqrMagnitude < 0.0001f) return;

        // Desired rotation to look at target
        Quaternion targetRotation = Quaternion.LookRotation(direction);

        // Optionally clamp yaw relative to initial rotation
        float yawDiff = Quaternion.Angle(initialRotation, targetRotation);
        if (yawDiff > maxYawAngle)
            targetRotation = Quaternion.RotateTowards(initialRotation, targetRotation, maxYawAngle);

        // Smoothly rotate towards target
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
    }
}
