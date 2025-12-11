using UnityEngine;
using System.Text;
using System.Threading.Tasks;
using Whisper;
using System;

public class RunWhisper : MonoBehaviour
{
    [HideInInspector] public AudioClip audioClip;
    public SpeechRecognitionController speechRecognitionController;

    public WhisperManager whisperManager;
    private bool isLoaded = false;

    private async void Awake()
    {
        whisperManager = GetComponent<WhisperManager>();
        if (whisperManager == null)
        {
            whisperManager = gameObject.AddComponent<WhisperManager>();
        }

        await LoadModelAsync();
    }

    private async Task LoadModelAsync()
    {
        Debug.Log("Whisper: Loading model...");

        if (whisperManager == null)
        {
            Debug.LogError("WhisperManager is null!");
            return;
        }

        string modelPath = whisperManager.IsModelPathInStreamingAssets
            ? System.IO.Path.Combine(Application.streamingAssetsPath, whisperManager.ModelPath)
            : whisperManager.ModelPath;

        Debug.Log("Whisper: Absolute model path = " + modelPath);
        Debug.Log("Whisper: Exists? " + System.IO.File.Exists(modelPath));

        if (!System.IO.File.Exists(modelPath))
        {
            Debug.LogError("Whisper: Model file not found!");
            return;
        }

        try
        {
            await whisperManager.InitModel();
            isLoaded = whisperManager.IsLoaded;
            Debug.Log("Whisper: Model loaded successfully? " + isLoaded);
        }
        catch (Exception e)
        {
            Debug.LogError("WhisperManager.InitModel() exception: " + e);
        }
    }

    public void Transcribe()
    {
        Debug.Log("Whisper: Transcribe() called within RunWhisper.");
        Debug.Log("RunWhisper: audioClip = " + audioClip);

        if (!isLoaded)
        {
            Debug.Log("Whisper: Model not loaded yet.");
            return;
        }

        if (audioClip == null)
        {
            Debug.Log("Whisper: AudioClip is NULL!");
            return;
        }
        Debug.Log("Whisper: AudioClip is valid and model loaded, proceeding with transcription.");
        RunTranscriptionSafe(audioClip);
    }

    private async void RunTranscriptionSafe(AudioClip clip)
    {
        Debug.Log($"Whisper: Starting transcription for clip {clip.name}, length {clip.length}s");

        WhisperResult result = null;

        try
        {
            // Run the GetTextAsync fully on a background thread to prevent Unity main thread deadlocks
            result = await Task.Run(async () =>
            {
                Debug.Log("Whisper: Background thread started for GetTextAsync...");
                var r = await whisperManager.GetTextAsync(clip);
                Debug.Log("Whisper: Background thread finished GetTextAsync.");
                return r;
            });
        }
        catch (Exception e)
        {
            Debug.LogError("Whisper: Exception in background transcription: " + e);
            return;
        }

        if (result == null)
        {
            Debug.LogWarning("Whisper: Result was null. Transcription failed or hung.");
            return;
        }

        if (result.Segments == null || result.Segments.Count == 0)
        {
            Debug.LogWarning("Whisper: No segments returned.");
            return;
        }

        StringBuilder sb = new StringBuilder();
        foreach (var segment in result.Segments)
            sb.Append(segment.Text);

        string finalText = sb.ToString();
        Debug.Log("Whisper: Transcription result = " + finalText);

        // Send to UI / callback
        speechRecognitionController.onResponse.Invoke(finalText);
    }
}
