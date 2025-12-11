using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using LLMUnity;
using UnityEngine.Networking;
using System.Text;

namespace LLMUnitySamples
{
    public class ChatBot : MonoBehaviour
    {
        [SerializeField] private TTSManager ttsManager;
        [SerializeField] private UnityEngine.UI.ScrollRect scrollRect; 


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
        private string lastResponse = "";
        private List<string> userHistory = new List<string>();


        private string openAI_prefix =
            "You are my oncologist. Please respond kindly and professionally. Be personable and not robotic, try to be specific. Never refer to prompts directly and always respond in max 2-3 sentences. My question is: ";

        void Start()
        {
            if (font == null)
                font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

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

            WarmUpCallback();
        }

        public void SubmitVoiceCommand(string message)
        {
            StartCoroutine(SubmitAfterSetText(message));
        }

        private IEnumerator ScrollToBottomNextFrame()
        {
            // Wait for end of frame so layout updates are done
            yield return new WaitForEndOfFrame();

            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = 0f; // scroll to bottom
            Canvas.ForceUpdateCanvases();
        }
        private void ScrollToBottom()
        {
            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = 0f;
            Canvas.ForceUpdateCanvases();
        }

        private IEnumerator ScrollNextFrame()
        {
            yield return new WaitForEndOfFrame(); // wait for Unity to update layout
            ScrollToBottom();
        }

        string EscapeForJson(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n");
        }


        private IEnumerator SubmitAfterSetText(string message)
        {
            inputBubble.SafeSetText(message, this);

            float timer = 0f;
            while (inputBubble.GetText() != message && timer < 1f)
            {
                timer += Time.deltaTime;
                yield return null;
            }

            onInputFieldSubmit(message);
        }

        void onInputFieldSubmit(string newText)
        {
            if (blockInput || string.IsNullOrWhiteSpace(newText))
            {
                StartCoroutine(BlockInteraction());
                return;
            }

            blockInput = true;

            string message = inputBubble.GetText().Replace("\v", "\n");
            inputBubble.SafeSetText("", this);

            userHistory.Add(message);
            int maxHistory = 10;
            if (userHistory.Count > maxHistory)
                userHistory.RemoveAt(0);


            Bubble playerBubble = new Bubble(chatContainer, playerUI, "PlayerBubble", message);
            Bubble aiBubble = new Bubble(chatContainer, aiUI, "AIBubble", "...");

            chatBubbles.Add(playerBubble);
            chatBubbles.Add(aiBubble);
            StartCoroutine(ScrollNextFrame());


            StartCoroutine(ScrollToBottomNextFrame());

            string historyContext = string.Join("\n", userHistory);
            string openaiMessage = openAI_prefix + message + "\n\nPrevious messages:\n" + historyContext;
            openaiMessage = EscapeForJson(openaiMessage);
            StartCoroutine(SendToOpenAI(openaiMessage, aiBubble));
            //StartCoroutine(SendToOpenAI(openAI_prefix + message, aiBubble));

            

        }

        IEnumerator SendToOpenAI(string userInput, Bubble aiBubble)
        {
            string apiKey = "key";
            string apiUrl = "https://api.openai.com/v1/chat/completions";

            string jsonData = $@"
            {{
                ""model"": ""gpt-3.5-turbo"",
                ""messages"": [
                    {{ ""role"": ""system"", ""content"": ""You are a helpful assistant."" }},
                    {{ ""role"": ""user"", ""content"": ""{userInput}"" }}
                ]
            }}";

            using (UnityWebRequest request = new UnityWebRequest(apiUrl, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("Authorization", "Bearer " + apiKey);

                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    aiBubble.SetText("Error: " + request.error);
                }
                else
                {
                    OpenAIResponse responseObj =
                        JsonUtility.FromJson<OpenAIResponse>(request.downloadHandler.text);

                    if (responseObj.choices != null &&
                        responseObj.choices.Length > 0)
                    {
                        string reply = responseObj.choices[0].message.content;
                        lastResponse = reply;
                        aiBubble.SetText(reply);
                        StartCoroutine(ScrollNextFrame());
                        AllowInput();
                    }
                    else
                    {
                        aiBubble.SetText("Error: Unexpected response format.");
                    }
                }
            }
        }

        void SendToTTS(string text)
        {
            if (ttsManager != null)
                ttsManager.SynthesizeAndPlay(text);
        }

        public void AllowInput()
        {
            if (!string.IsNullOrEmpty(lastResponse))
                SendToTTS(lastResponse);

            blockInput = false;
            inputBubble.ReActivateInputField();
        }

        public void WarmUpCallback()
        {
            inputBlocker.SetActive(false);
            warmUpDone = true;
            inputBubble.SetPlaceHolderText("Start a conversation with your virtual oncologist...");
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
                inputBubble.SafeSetText("", this);
        }

        // ------- REMOVED BROKEN MANUAL LAYOUT CODE -------
        // UpdateBubblePositions()
        // bubble Y math
        // OnResize(UpdateBubblePositions)
        // -------------------------------------------------

        public void ExitGame()
        {
            Application.Quit();
        }

        [System.Serializable]
        public class OpenAIResponse { public Choice[] choices; }

        [System.Serializable]
        public class Choice { public Message message; }

        [System.Serializable]
        public class Message { public string role; public string content; }
    }
}
