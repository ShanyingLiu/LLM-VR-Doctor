using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ChatBubble : MonoBehaviour
{
    public Image background;
    public TMP_Text messageText;
    public RectTransform root;

    public void Setup(string text, Color bgColor)
    {
        messageText.text = text;
        background.color = bgColor;

        LayoutRebuilder.ForceRebuildLayoutImmediate(root);
    }

    public float Height => root.sizeDelta.y;
}
