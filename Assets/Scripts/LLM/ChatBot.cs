using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using LLMUnity;
using UnityEngine.Networking;
using System.Text;
using System;

namespace LLMUnitySamples
{
    public class ChatBot : MonoBehaviour
    {
        [SerializeField] private TTSManager ttsManager;
        public Transform chatContainer;
        public Color playerColor = new Color32(81, 164, 81, 255);
        public Color aiColor = new Color32(29, 29, 73, 255);
        public Color fontColor = Color.white;
        public Font font;
        public int fontSize = 16;
        public int bubbleWidth = 400;
        public LLMCharacter llmCharacter;
        public float textPadding = 10f;
        public float bubbleSpacing = 10f;
        public Sprite sprite;

        [SerializeField] private GameObject inputBlocker;

        private InputBubble inputBubble;
        private List<Bubble> chatBubbles = new List<Bubble>();
        private bool blockInput = true;
        private BubbleUI playerUI, aiUI;
        private bool warmUpDone = false;
        private int lastBubbleOutsideFOV = -1;
        private string openAI_prefix = "You are my oncologist. Please respond kindly and professionally to my question, staying fully in character without ever acknowledging the prompt directly. Keep answer short at maximum 2 sentences. My question is: ";

        private string lastResponse = "";  // To store the latest LLM response

        void Start()
        {
            Debug.Log("Chatbot_debug: ✅ ChatBot script is running on!!!: " + gameObject.name);
            if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            playerUI = new BubbleUI
            {
                sprite = sprite,
                font = font,
                fontSize = fontSize,
                fontColor = fontColor,
                bubbleColor = playerColor,
                bottomPosition = 0,
                leftPosition = 0,
                textPadding = textPadding,
                bubbleOffset = bubbleSpacing,
                bubbleWidth = bubbleWidth,
                bubbleHeight = -1
            };
            aiUI = playerUI;
            aiUI.bubbleColor = aiColor;
            aiUI.leftPosition = 1;

            inputBubble = new InputBubble(chatContainer, playerUI, "InputBubble", "Loading...", 4);
            inputBubble.AddSubmitListener(onInputFieldSubmit);
            inputBubble.AddValueChangedListener(onValueChanged);
            inputBubble.setInteractable(false);
            //inputBubble.setInteractable(true);
           // Debug.Log("Chatbot_debug: setinteractible true");
            //_ = llmCharacter.Warmup(WarmUpCallback);
            Debug.Log("Chatbot_debug: pre-warm");
            //warm up call back calls allow input
            WarmUpCallback();
            Debug.Log("Chatbot_debug: ✅ WarmUpCallback manually called in Start()");
            // now that we have the model  external don;t neeed warmup routine to allowinput(), leaving in case this leads to new issues
            

        }

        public void SubmitVoiceCommand(string message)
        {
            Debug.Log($"Chatbot_debug: ✅ SubmitVoiceCommand called - Message: {message}");
            StartCoroutine(SubmitAfterSetText(message));
        }

        private IEnumerator SubmitAfterSetText(string message)
        {
            inputBubble.SafeSetText(message, this);

            // Wait until inputField.text actually reflects the new message
            float timeout = 1f;
            float timer = 0f;

            while (inputBubble.GetText() != message && timer < timeout)
            {
                yield return null;
                timer += Time.deltaTime;
            }

            Debug.Log($"Chatbot_debug: ✅ Verified input text: {inputBubble.GetText()} — proceeding to submit.");
            onInputFieldSubmit(message);
        }





        void onInputFieldSubmit(string newText)
        {
            Debug.Log($"Chatbot_debug:✅ onInputFieldSubmit triggered! Text: {newText}");

            Debug.Log($"Chatbot_debug: 🔍 Checking inputBubble before activating. IsNull: {inputBubble == null}");
            Debug.Log($"Chatbot_debug: 🚧 blockInput state: {blockInput}");
            Debug.Log($"Chatbot_debug: 🎈 inputBubble text before activation: {inputBubble.GetText()}");
            Debug.Log($"Chatbot_debug: 🔍 Input Blocker Active: {inputBlocker.activeSelf}");

            //snag flag 1.1
            inputBubble.ActivateInputField();
            Debug.Log($"Chatbot_debug:✅ 1");

            if (blockInput || string.IsNullOrWhiteSpace(newText))
            {
                StartCoroutine(BlockInteraction());
                Debug.Log($"Chatbot_debug:✅ 2 (Input blocked or empty  message)");
                return;
            }
            Debug.Log($"Chatbot_debug:✅ 3");

            blockInput = true;
            string message = inputBubble.GetText().Replace("\v", "\n");
            //inputBubble.SetText("");
            inputBubble.SafeSetText("", this);
            Debug.Log("Chatbot_debug: -- input bubble text set to NULL");

            Debug.Log($"Chatbot_debug:📢 Creating player  chat bubble...");
            Bubble playerBubble = new Bubble(chatContainer, playerUI, "PlayerBubble", message);
            Bubble aiBubble = new Bubble(chatContainer, aiUI, "AIBubble", "...");

            Debug.Log($"Chatbot_debug: about to add to player bubble");

            chatBubbles.Add(playerBubble);
            Debug.Log($"Chatbot_debug: added to player bubble");

            chatBubbles.Add(aiBubble);
            Debug.Log($"Chatbot_debug: added ai bubble");

            playerBubble.OnResize(UpdateBubblePositions);
            Debug.Log($"Chatbot_debug: resized user bubble");

            aiBubble.OnResize(UpdateBubblePositions);

            Debug.Log($"Chatbot_debug:✅ Chat bubbles created successfully!");

            string openai_message = openAI_prefix + message;

            Debug.Log($"Chatbot_debug:📢 Starting Coroutine: SendToOpenAI('{openai_message}')");
            StartCoroutine(SendToOpenAI(openai_message, aiBubble));
            Debug.Log("Chatbot_debug:✅ StartCoroutine was called!");
            

        }


        IEnumerator SendToOpenAI(string userInput, Bubble aiBubble)
{
    Debug.Log($"Chatbot_debug:🚀 Preparing OpenAI API request for: '{userInput}'");

    string apiKey = "key"; // your project key
    string orgId = ""; // optional: leave blank if unsure
    string apiUrl = "https://api.openai.com/v1/chat/completions";

    string jsonData = $@"
{{
    ""model"": ""gpt-3.5-turbo"",
    ""messages"": [
        {{ ""role"": ""system"", ""content"": ""You are a helpful assistant."" }},
        {{ ""role"": ""user"", ""content"": ""{userInput}"" }}
    ]
}}";

    Debug.Log("Chatbot_debug:✅ OpenAI JSON Payload: " + jsonData);

    using (UnityWebRequest request = new UnityWebRequest(apiUrl, "POST"))
    {
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + apiKey);

        // Add the OpenAI-Organization header if using sk-proj keys
        if (!string.IsNullOrEmpty(orgId))
        {
            request.SetRequestHeader("OpenAI-Organization", orgId);
            Debug.Log($"Chatbot_debug:📡 Using OpenAI-Organization header: {orgId}");
        }

        Debug.Log("Chatbot_debug:📡 Sending OpenAI request...");

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Chatbot_debug:❌ OpenAI Request Failed: " + request.error);
            Debug.LogError("Chatbot_debug:❌ Server Response: " + request.downloadHandler.text);
            aiBubble.SetText("Error: Unable to fetch response.");
        }
        else
        {
            Debug.Log("Chatbot_debug:✅ OpenAI Request Successful!");
            Debug.Log("Chatbot_debug:✅ Response JSON: " + request.downloadHandler.text);

            OpenAIResponse responseObj = JsonUtility.FromJson<OpenAIResponse>(request.downloadHandler.text);

            if (responseObj.choices != null && responseObj.choices.Length > 0)
            {
                string reply = responseObj.choices[0].message.content;
                lastResponse = reply;
                aiBubble.SetText(reply);
                AllowInput();
            }
            else
            {
                Debug.LogError("Chatbot_debug:❌ Invalid response format from OpenAI!");
                aiBubble.SetText("Error: Unexpected response format.");
            }
        }
    }
}





        // Helper class to parse OpenAI's JSON response
        [System.Serializable]
        public class OpenAIResponse
        {
            public Choice[] choices;
        }

        [System.Serializable]
        public class Choice
        {
            public Message message;
        }

        [System.Serializable]
        public class Message
        {
            public string role;
            public string content;
        }


        void SendToTTS(string text)
        {
            if (ttsManager == null)
            {
                Debug.LogError("Chatbot_debug:TTSManager is not assigned! Please assign it in the Inspector.");
                return;
            }

            Debug.Log($"Chatbot_debug:Sending to TTS: {text}");
            ttsManager.SynthesizeAndPlay(text);
        }

        public void AllowInput()
        {
            Debug.Log("Chatbot_debug: 🔄 AllowInput() called!");

            // Check if the chatbot has a response
            if (!string.IsNullOrEmpty(lastResponse))
            {
                Debug.Log($"Chatbot_debug: 📢 Sending response to TTS: {lastResponse}");
                SendToTTS(lastResponse);  // Trigger TTS
            }

            blockInput = false;
            Debug.Log("Chatbot_debug: 🚀 Unlocking input...");

            inputBubble.ReActivateInputField();

            Debug.Log("Chatbot_debug: 🔄 Calling ActivateInputField()...");
           // inputBubble.ActivateInputField();

            //Debug.Log("Chatbot_debug: ✅ AllowInput() complete.");
        }


        public void WarmUpCallback()
        {
            inputBlocker.SetActive(false);
            warmUpDone = true;
            inputBubble.SetPlaceHolderText("Message me");
            AllowInput();
        }

        IEnumerator BlockInteraction()
        {
            inputBubble.setInteractable(false);
            yield return null;
            inputBubble.setInteractable(true);
            inputBubble.MoveTextEnd();
        }


        void onValueChanged(string newText)
        {
            if (Input.GetKey(KeyCode.Return) && string.IsNullOrWhiteSpace(inputBubble.GetText()))
            {
                //inputBubble.SetText("");
                inputBubble.SafeSetText("", this);

            }
        }

        public void UpdateBubblePositions()
        {
            float y = inputBubble.GetSize().y + inputBubble.GetRectTransform().offsetMin.y + bubbleSpacing;
            float containerHeight = chatContainer.GetComponent<RectTransform>().rect.height;

            for (int i = chatBubbles.Count - 1; i >= 0; i--)
            {
                Bubble bubble = chatBubbles[i];
                RectTransform childRect = bubble.GetRectTransform();
                childRect.anchoredPosition = new Vector2(childRect.anchoredPosition.x, y);

                if (y > containerHeight && lastBubbleOutsideFOV == -1)
                {
                    lastBubbleOutsideFOV = i;
                }
                y += bubble.GetSize().y + bubbleSpacing;
            }
        }

        void Update()
        {
            if (!inputBubble.inputFocused() && warmUpDone) //removed warmupdone in the if
            {
                inputBubble.ActivateInputField();
                StartCoroutine(BlockInteraction());
            }

            if (lastBubbleOutsideFOV != -1)
            {
                for (int i = 0; i <= lastBubbleOutsideFOV; i++)
                {
                    chatBubbles[i].Destroy();
                }
                chatBubbles.RemoveRange(0, lastBubbleOutsideFOV + 1);
                lastBubbleOutsideFOV = -1;
            }
        }

        public void ExitGame()
        {
            Debug.Log("Chatbot_debug:Exit button clicked");
            Application.Quit();
        }

        void OnValidate()
        {
            if (!llmCharacter.remote && llmCharacter.llm != null && llmCharacter.llm.model == "")
            {
                Debug.LogWarning($"Chatbot_debug:Please select a model in the {llmCharacter.llm.gameObject.name} GameObject!");
            }
        }
    }
}
