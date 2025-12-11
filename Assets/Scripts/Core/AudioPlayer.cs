using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

[RequireComponent(typeof(AudioSource))]
public class AudioPlayer : MonoBehaviour
{
    private AudioSource audioSource;
    private Queue<string> audioQueue = new Queue<string>();   // file paths
    private bool isPlaying = false;

    private void OnEnable()
    {
        if (!audioSource) this.audioSource = GetComponent<AudioSource>();
    }

    private void OnValidate() => OnEnable();

    // Called by TTS system for each chunk
    public void ProcessAudioBytes(byte[] audioData)
    {
        // Unique file name per chunk to avoid collisions
        string filePath = Path.Combine(
            Application.persistentDataPath,
            "tts_" + Guid.NewGuid().ToString("N") + ".mp3"
        );

        File.WriteAllBytes(filePath, audioData);
        audioQueue.Enqueue(filePath);

        if (!isPlaying)
            StartCoroutine(PlaybackQueueLoop());
    }

    // Main loop that plays queued clips in order
    private IEnumerator PlaybackQueueLoop()
    {
        isPlaying = true;

        while (audioQueue.Count > 0)
        {
            string filePath = audioQueue.Dequeue();

            using UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip("file://" + filePath, AudioType.MPEG);
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
                audioSource.PlayOneShot(clip);

                // Wait for audio to finish
                yield return new WaitForSeconds(clip.length);
            }
            else
            {
                Debug.LogError("Error loading TTS audio: " + www.error);
            }

            // Delete file after use
            try { File.Delete(filePath); } catch {}
        }

        isPlaying = false;
    }
}
