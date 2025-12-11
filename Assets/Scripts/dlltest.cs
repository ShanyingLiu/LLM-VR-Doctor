using System.Runtime.InteropServices;
using UnityEngine;

public class dlltest : MonoBehaviour
{
    [DllImport("libwhisper")]
    private static extern int whisper_version();

    void Start()
    {
        try
        {
            Debug.Log("Whisper version: " + whisper_version());
        }
        catch (System.Exception e)
        {
            Debug.LogError("DLL Load Failed: " + e);
        }
        

    }
}
