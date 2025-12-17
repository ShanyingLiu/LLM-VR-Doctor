using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Events;
using System.IO;
using UnityEngine.Android;
using OpenAI;
using UnityEngine.InputSystem;

public class SpeechRecognitionController : MonoBehaviour
{
    [Header("UI & Events")]
    [SerializeField] private UnityEvent onStartRecording;
    [SerializeField] private UnityEvent onSendRecording;
    [SerializeField] public UnityEvent<string> onResponse;
    [SerializeField] private TMP_Dropdown m_deviceDropdown;
    [SerializeField] private Image m_progress;

    [Header("Animation & AI")]
    public LoopingTimelineController animationLoop;

    [Header("Recording Settings")]
    [SerializeField] private Button recordButton;
    [SerializeField] private int recordingDuration = 4;   // UI fill time
    [SerializeField] private string apiKey;

    private const float MaxRecordingTime = 15f; // HARD CEILING

    private AudioClip m_clip;
    private string m_deviceName;
    private bool m_recording;
    private float m_time;
    private OpenAIApi openai;

    private void Awake()
    {
        openai = new OpenAIApi(apiKey);

        if (Microphone.devices.Length == 0)
        {
            Debug.LogError("No microphones detected!");
            return;
        }

        m_deviceName = Microphone.devices[0];

        foreach (var device in Microphone.devices)
            m_deviceDropdown.options.Add(new TMP_Dropdown.OptionData(device));

        m_deviceDropdown.value = 0;
        m_deviceDropdown.onValueChanged.AddListener(OnDeviceChanged);

        if (recordButton != null)
            recordButton.onClick.AddListener(OnRecordButtonClicked);
    }

    private void Start()
    {
        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
            Permission.RequestUserPermission(Permission.Microphone);
    }

    private void OnDeviceChanged(int idx)
    {
        m_deviceName = Microphone.devices[idx];
        PlayerPrefs.SetInt("user-mic-device-index", idx);
    }

    private void OnRecordButtonClicked()
    {
        if (!m_recording)
        {
            StartRecording();
            animationLoop?.StartLoop(LoopingTimelineController.LoopMode.Listening);
        }
        else
        {
            StopRecording();
            animationLoop?.StopLoop();
        }
    }

    private void StartRecording()
    {
        onStartRecording.Invoke();

        // Allow up to 10s maximum recording camera
        m_clip = Microphone.Start(m_deviceName, false, (int)MaxRecordingTime, 44100);

        m_recording = true;
        m_time = 0f;
        m_progress.fillAmount = 0f;

        Debug.Log($"🎤 Recording started on device: {m_deviceName}");
    }

    private void ToggleRecording()
    {
        if (!m_recording)
        {
            StartRecording();
            animationLoop?.StartLoop(LoopingTimelineController.LoopMode.Listening);
        }
        else
        {
            StopRecording();
            animationLoop?.StopLoop();
        }
    }


    private void StopRecording()
    {
        if (!m_recording)
            return;

        if (!Microphone.IsRecording(m_deviceName))
        {
            Debug.LogWarning("Tried to stop mic but it wasn't recording.");
            return;
        }

        int position = Microphone.GetPosition(m_deviceName);
        Microphone.End(m_deviceName);

        m_recording = false;

        if (m_clip == null || position == 0)
        {
            Debug.LogWarning("No audio recorded.");
            return;
        }

        // Trim audio
        float[] samples = new float[position * m_clip.channels];
        m_clip.GetData(samples, 0);

        AudioClip trimmed = AudioClip.Create(
            "TrimmedClip",
            position,
            m_clip.channels,
            m_clip.frequency,
            false
        );
        trimmed.SetData(samples, 0);
        m_clip = trimmed;

        Debug.Log($"🎤 Trimmed Audio Length: {m_clip.length}s — Samples: {position}");

        SendRecording();
    }

    private async void SendRecording()
    {
        onSendRecording.Invoke();

        if (m_clip == null)
        {
            Debug.LogError("AudioClip is NULL in SendRecording!");
            return;
        }

        byte[] data = SaveWav.Save("output.wav", m_clip);

        try
        {
            var req = new CreateAudioTranscriptionsRequest
            {
                FileData = new FileData { Data = data, Name = "audio.wav" },
                Model = "whisper-1",
                Language = "en"
            };

            Debug.Log("📤 Sending recording to Whisper...");
            var res = await openai.CreateAudioTranscription(req);

            onResponse.Invoke(res.Text);
        }
        catch (System.Exception e)
        {
            Debug.LogError("Whisper transcription failed: " + e);
        }
    }
    
    private void Update()
    {
        // Controller A button (Gamepad / XR)
        if (Gamepad.current != null &&
            Gamepad.current.buttonSouth.wasPressedThisFrame)
        {
            ToggleRecording();
        }

    #if UNITY_EDITOR
        // Spacebar fallback in editor
        if (Keyboard.current != null &&
            Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            ToggleRecording();
        }
    #endif

        if (m_recording)
        {
            m_time += Time.deltaTime;

            m_progress.fillAmount = Mathf.Clamp01(m_time / recordingDuration);

            // HARD CEILING
            if (m_time >= MaxRecordingTime)
            {
                Debug.Log("Auto-stop: reached maximum recording time");
                StopRecording();
                animationLoop?.StopLoop();
            }
        }
    }
}