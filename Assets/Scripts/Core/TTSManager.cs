using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using LLMUnity;
using UnityEngine.Networking;
using System.Text;

public class TTSManager : MonoBehaviour
{
    private OpenAIWrapper openAIWrapper;
    [SerializeField] public AudioPlayer audioPlayer;
    [SerializeField] public TTSModel model = TTSModel.TTS_1;
    [SerializeField] public TTSVoice voice = TTSVoice.Alloy;
    [SerializeField, Range(0.25f, 4.0f)] public float speed = 1f;
    
    private void OnEnable()
    {
        if (!openAIWrapper) this.openAIWrapper = FindObjectOfType<OpenAIWrapper>();
        if (!audioPlayer) this.audioPlayer = GetComponentInChildren<AudioPlayer>();
    }

    private void OnValidate() => OnEnable();

    private static readonly char[] sentenceEndings = { '.', '!', '?' };

    private static List<string> ChunkText(string text, int maxChunkLength = 100)
    {
        List<string> chunks = new List<string>();
        string[] sentences = text.Split(sentenceEndings, StringSplitOptions.RemoveEmptyEntries);

        string current = "";

        foreach (string raw in sentences)
        {
            string sentence = raw.Trim();
            if (sentence.Length == 0) continue;

            // Add punctuation back
            char ending = text[text.IndexOf(raw) + raw.Length];
            sentence += ending;

            // If adding this sentence exceeds our chunk size → start a new one
            if ((current + " " + sentence).Length > maxChunkLength)
            {
                if (current.Length > 0)
                    chunks.Add(current.Trim());
                current = sentence;
            }
            else
            {
                current += " " + sentence;
            }
        }

        if (current.Length > 0)
            chunks.Add(current.Trim());

        return chunks;
    }


    public async void SynthesizeAndPlay(string text)
    {
        Debug.Log("Trying to synthesize " + text);

        List<string> chunks = ChunkText(text);

        foreach (var chunk in chunks)
        {
            Debug.Log("Synthesizing chunk: " + chunk);

            byte[] audioData = await openAIWrapper.RequestTextToSpeech(chunk, model, voice, speed);

            if (audioData != null)
            {
                audioPlayer.ProcessAudioBytes(audioData);  // Ensure AudioPlayer queues, not replaces
            }
            else
            {
                Debug.LogError("Failed to get audio data from OpenAI chunk.");
            }
        }
    }
}