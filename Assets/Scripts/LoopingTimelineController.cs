using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
public class LoopingTimelineController : MonoBehaviour
{
    public PlayableDirector director;
    public PlayableAsset listeningTimeline;
    public PlayableAsset speakingTimeline;

    private bool isLooping = false;

    public enum LoopMode { Listening, Speaking }
    private LoopMode currentMode = LoopMode.Listening;

    void Start()
    {
        // Optionally sample initial pose
        director.playableAsset = listeningTimeline;
        director.time = 9.1;
        director.Evaluate();
        Debug.Log("[ANIM] Sampled initial listening pose.");
    }

    void Update()
    {
        if (!isLooping) return;

        if (director.time >= director.duration)
        {
            director.time = 0;
            director.Play();
        }
    }

    public void StartLoop(LoopMode mode)
    {
        if (isLooping && mode == currentMode) return;

        currentMode = mode;
        director.playableAsset = (mode == LoopMode.Listening) ? listeningTimeline : speakingTimeline;
        isLooping = true;
        director.time = 0;
        director.Play();
        Debug.Log($"[ANIM] {mode} loop started.");
    }

    public void StopLoop()
    {
        isLooping = false;
        director.Stop();
        director.time = 0;
        Debug.Log("[ANIM] Loop stopped.");
    }
}
