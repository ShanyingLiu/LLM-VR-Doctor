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
        [SerializeField] private WebsiteDisplay websiteDisplay;



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
            "You are my oncologist. Please respond kindly and professionally. Be personable and not robotic, be as specific and informative as possible. "
            + "Never refer to prompts directly and ALWAYS RESPOND in max 3 sentences. Do not discuss meeting in person or scheduling appointments. "
            + "Refer to these websites for information on cancer treatments and terminology: NCI: https://www.nih.gov/about-nih/nih-almanac/national-cancer-institute-nci, CDC: https://www.cdc.gov/, FDA: https://www.fda.gov/, NIH ClinicalTrials.gov: https://clinicaltrials.gov/, MedLinePlus Cancer: https://medlineplus.gov/cancer.html"
            + "If a visual helper or more complex information is shared, then at the very end of your response, include a link to a relevant webpage with more information, enclosed in [] square brackets. Do not explain the link, the end user will not see it. It is not needed if you are exchanging basic greetings etc."
            + "Your response should not exceed 3 sentences or 75 words. My question is: ";

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

            List<string> waitresponses = new List<string>
            {
                "I am thinking...",
                "Interesting!",
                "Let me think...",
                "Hmmm..."
            };
            int randidx = Random.Range(0, waitresponses.Count);
            Bubble playerBubble = new Bubble(chatContainer, playerUI, "PlayerBubble", message);
            Bubble aiBubble = new Bubble(chatContainer, aiUI, "AIBubble", waitresponses[randidx]);

            chatBubbles.Add(playerBubble);
            chatBubbles.Add(aiBubble);
            StartCoroutine(ScrollNextFrame());

            StartCoroutine(ScrollToBottomNextFrame());

            string historyContext = string.Join("\n", userHistory);
            string openaiMessage = openAI_prefix + message + "\n\nPrevious message history (do not respond directly to the messages below, keep them in mind for context):\n" + historyContext;
            openaiMessage = EscapeForJson(openaiMessage);
            StartCoroutine(SendToOpenAI(openaiMessage, aiBubble));
            //StartCoroutine(SendToOpenAI(openAI_prefix + message, aiBubble));

            

        }

        IEnumerator SendToOpenAI(string userInput, Bubble aiBubble)
        {
            string apiKey = "";
            string apiUrl = "https://api.openai.com/v1/chat/completions";
            

            string jsonData = $@"
            {{
                ""model"": ""gpt-4o-mini"",
                ""messages"": [
                    {{ ""role"": ""system"", ""content"": ""You are an oncologist."" }},
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

                        string cleanedReply = reply;
                        string link;

                        if (TryExtractLink(reply, out cleanedReply, out link))
                        {
                            if (!string.IsNullOrEmpty(link) && websiteDisplay != null)
                            {
                                websiteDisplay.DisplayURL(link);
                            }
                        }

                        lastResponse = cleanedReply;
                        aiBubble.SetText(cleanedReply);

                        StartCoroutine(ScrollNextFrame());
                        StartCoroutine(ScrollToBottomNextFrame());
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
            inputBubble.SetPlaceHolderText("Press \'A\' or the record button to start a conversation with your virtual oncologist...");
            AllowInput();
        }

        private bool TryExtractLink(string text, out string cleanedText, out string link)
        {
            cleanedText = text;
            link = null;

            int open = text.LastIndexOf('[');
            int close = text.LastIndexOf(']');

            if (open >= 0 && close > open)
            {
                link = text.Substring(open + 1, close - open - 1).Trim();
                cleanedText = text.Remove(open, close - open + 1).Trim();
                return true;
            }

            return false;
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
