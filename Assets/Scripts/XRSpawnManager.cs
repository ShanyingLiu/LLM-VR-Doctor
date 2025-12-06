using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

public class XRSpawnManager : MonoBehaviour
{
    [Header("Assign your XR Origin (the rig root)")]
    public GameObject xrOrigin;

    [Header("Desired spawn position in world space")]
    public Vector3 spawnPosition = new Vector3(0f, 0f, 0f);

    [Header("Desired Y-facing direction (in degrees)")]
    public float spawnRotationY = 0f;

    [Header("Set tracking origin mode (Floor or Device)")]
    public TrackingOriginModeFlags trackingOriginMode = TrackingOriginModeFlags.Floor;

    void Start()
    {
        StartCoroutine(WaitForXRTrackingAndReposition());
    }

    IEnumerator WaitForXRTrackingAndReposition()
    {
        // Get XR Input Subsystem
        List<XRInputSubsystem> subsystems = new List<XRInputSubsystem>();
        SubsystemManager.GetInstances(subsystems);

        XRInputSubsystem xrInput = null;
        if (subsystems.Count > 0)
            xrInput = subsystems[0];

        // Wait until tracking is running
        while (xrInput != null && !xrInput.running)
        {
            yield return null;
        }

        // Optional: wait an extra frame or two
        yield return new WaitForSeconds(0.5f);

        Debug.Log("✅ XR tracking active. Applying spawn transformation...");

        // Explicitly set tracking origin mode
        if (xrInput != null && xrInput.TrySetTrackingOriginMode(trackingOriginMode))
        {
            Debug.Log($"✅ Set tracking origin mode to: {trackingOriginMode}");
        }
        else
        {
            Debug.LogWarning("⚠️ Could not set tracking origin mode (may already be set or unsupported)");
        }

        // Apply the desired spawn position
        xrOrigin.transform.position = spawnPosition;

        // Calculate current headset Y rotation (yaw) and correct for it
        Vector3 cameraForward = Camera.main.transform.forward;
        cameraForward.y = 0;
        cameraForward.Normalize();

        Quaternion currentYaw = Quaternion.LookRotation(cameraForward);
        Quaternion desiredYaw = Quaternion.Euler(0, spawnRotationY, 0);
        Quaternion correction = desiredYaw * Quaternion.Inverse(currentYaw);

        xrOrigin.transform.rotation = correction * xrOrigin.transform.rotation;

        Debug.Log("✅ XR Origin repositioned and rotated successfully.");
    }
}
