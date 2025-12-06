using UnityEngine;
using UnityEngine.Playables;

public class LoopTimeline : MonoBehaviour
{
    public PlayableDirector director;

    void Start()
    {
        Debug.Log("LoopTimeline script is alive");
    }

    void Update()
    {
        //Debug.Log($"Director time: {director.time} / {director.duration}");

        if (director.time >= director.duration)
        {
            Debug.Log("Timeline reached end — restarting");
            director.time = 0;
            director.Play();
        }
    }

}

