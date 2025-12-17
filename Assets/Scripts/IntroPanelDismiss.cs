using UnityEngine;
using UnityEngine.XR;

public class IntroPanelDismiss : MonoBehaviour
{
    private const string PrefKey = "IntroPanelDismissed";

    private InputDevice rightHandDevice;

    private void Start()
    {
/*        if (PlayerPrefs.GetInt(PrefKey, 0) == 1)
        {
            gameObject.SetActive(false);
            return;
        }*/

        // Try to get right-hand XR controller
        TryInitializeXR();
    }

    private void TryInitializeXR()
    {
        var devices = new System.Collections.Generic.List<InputDevice>();
        InputDevices.GetDevicesWithCharacteristics(
            InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller,
            devices
        );

        if (devices.Count > 0)
            rightHandDevice = devices[0];
    }

    private void Update()
    {
        // XR controller B (secondary button)
        if (rightHandDevice.isValid &&
            rightHandDevice.TryGetFeatureValue(CommonUsages.secondaryButton, out bool pressed) &&
            pressed)
        {
            Debug.Log("Dismissed intro panel");
            Dismiss();
        }
    }

    private void Dismiss()
    {
        PlayerPrefs.SetInt(PrefKey, 1);
        PlayerPrefs.Save();
        gameObject.SetActive(false);
    }
}
