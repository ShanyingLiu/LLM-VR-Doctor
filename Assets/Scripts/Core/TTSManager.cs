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

        if (string.IsNullOrWhiteSpace(text))
            return chunks;

        StringBuilder current = new StringBuilder();
        StringBuilder sentence = new StringBuilder();

        foreach (char c in text)
        {
            sentence.Append(c);

            if (Array.IndexOf(sentenceEndings, c) >= 0)
            {
                AppendSentence(sentence.ToString(), ref current, chunks, maxChunkLength);
                sentence.Clear();
            }
        }

        // leftover text without punctuation
        if (sentence.Length > 0)
            AppendSentence(sentence.ToString(), ref current, chunks, maxChunkLength);

        if (current.Length > 0)
            chunks.Add(current.ToString().Trim());

        return chunks;
    }

    private static void AppendSentence(
        string sentence,
        ref StringBuilder current,
        List<string> chunks,
        int maxChunkLength)
    {
        sentence = sentence.Trim();
        if (sentence.Length == 0) return;

        if ((current.Length + sentence.Length + 1) > maxChunkLength)
        {
            if (current.Length > 0)
                chunks.Add(current.ToString().Trim());

            current.Clear();
            current.Append(sentence);
        }
        else
        {
            if (current.Length > 0)
                current.Append(" ");

            current.Append(sentence);
        }
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