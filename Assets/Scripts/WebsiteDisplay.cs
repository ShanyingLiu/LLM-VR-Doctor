using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class WebsiteDisplay : MonoBehaviour
{
    [Header("ScreenshotOne Settings")]
    [SerializeField] private string screenshotOneApiKey;

    [Header("Target")]
    [SerializeField] private Renderer targetRenderer;

    [Header("Screenshot Options")]
    [SerializeField] private int viewportWidth = 1280;
    [SerializeField] private int viewportHeight = 800;
    [SerializeField] private bool fullPage = false;

    /// <summary>
    /// Call this to load and display a webpage screenshot on a 3D object
    /// </summary>
    /// 
    /// 
    private bool IsValidHttpUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        url = url.Trim();

        return url.StartsWith("http://") || url.StartsWith("https://");
    }

    public void DisplayURL(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            Debug.LogError("WebsiteDisplay: URL is empty.");
            return;
        }

        url = url.Trim();

        if (!IsValidHttpUrl(url))
        {
            Debug.LogError("Invalid URL from AI: " + url);
            return;
        }

        // Replace spaces with %20 instead of encoding the whole URL
        url = url.Replace(" ", "%20");

        if (targetRenderer == null)
        {
            Debug.LogError("WebsiteDisplay: Target Renderer is not assigned.");
            return;
        }

        StartCoroutine(LoadScreenshot(url));
    }


    private IEnumerator LoadScreenshot(string url)
    {
        string requestUrl =
            "https://api.screenshotone.com/take" +
            "?access_key=" + screenshotOneApiKey +
            "&url=" + UnityWebRequest.EscapeURL(url) +
            "&format=png" +
            "&viewport_width=" + viewportWidth +
            "&viewport_height=" + viewportHeight +
            (fullPage ? "&full_page=true" : "");

        using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(requestUrl))
        {
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("WebsiteDisplay: Screenshot failed\n" + request.error);
                yield break;
            }

            Texture2D texture = DownloadHandlerTexture.GetContent(request);

            if (texture == null)
            {
                Debug.LogError("WebsiteDisplay: Received empty texture.");
                yield break;
            }

            ApplyTexture(texture);

            Debug.Log("WebsiteDisplay: Screenshot applied successfully.");
        }
    }

    private void ApplyTexture(Texture2D texture)
    {
        Material mat = targetRenderer.material;

        mat.mainTexture = texture;
        mat.mainTextureScale = Vector2.one;
        mat.mainTextureOffset = Vector2.zero;
    }
}
