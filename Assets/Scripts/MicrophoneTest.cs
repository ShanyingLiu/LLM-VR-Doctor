using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class MicrophoneTest : MonoBehaviour
{
    public string micName = "Android voice recognition input"; // Use correct mic device
    public AudioClip microphoneClip;
    public GameObject sphere;  // Assign a sphere in Unity to change color
    private bool isRecording = false;

    void Start()
    {
        Debug.Log("🎤 Microphone Test Started!");
        StartRecording();  // Start recording immediately
    }

    void StartRecording()
    {
        if (isRecording) return;

        Debug.Log("🎤 Attempting to start recording...");
        microphoneClip = Microphone.Start(micName, true, 5, 44100);

        if (microphoneClip == null)
        {
            Debug.LogError("🚨 Microphone failed to start!");
            return;
        }

        isRecording = true;
        Debug.Log("✅ Microphone started successfully!");
        InvokeRepeating("CheckAudio", 1f, 1f); // Check audio every second
    }

    void CheckAudio()
    {
        if (microphoneClip == null) return;

        float[] samples = new float[microphoneClip.samples];
        microphoneClip.GetData(samples, 0);

        float maxAmplitude = 0f;
        foreach (float sample in samples)
        {
            if (Mathf.Abs(sample) > maxAmplitude)
                maxAmplitude = Mathf.Abs(sample);
        }

        Debug.Log($"📊 Max Audio Amplitude: {maxAmplitude}");

        // If amplitude is high enough, turn sphere green
        if (maxAmplitude > 0.01f)
        {
            Debug.Log("🎙️ Sound detected!");
            sphere.GetComponent<Renderer>().material.color = Color.green;
        }
        else
        {
            Debug.Log("🔇 No significant sound detected.");
            sphere.GetComponent<Renderer>().material.color = Color.red;
        }
    }
}
