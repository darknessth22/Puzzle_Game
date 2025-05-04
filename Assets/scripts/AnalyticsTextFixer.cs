using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AnalyticsTextFixer : MonoBehaviour
{
    public TextMeshProUGUI analyticsText;
    public RectTransform analyticsTextRectTransform;
    public ScrollRect scrollRect;

    public void Start()
    {
        Debug.Log("AnalyticsTextFixer: Starting to fix analytics text alignment");

        // Set up scroll view if it doesn't exist
        SetupScrollView();

        // Find the analytics text if not assigned
        if (analyticsText == null)
        {
            analyticsText = GetComponentInChildren<TextMeshProUGUI>(true);

            if (analyticsText == null)
            {
                GameObject textObj = GameObject.Find("GameAnalyticsText");
                if (textObj != null)
                {
                    analyticsText = textObj.GetComponent<TextMeshProUGUI>();
                    Debug.Log("AnalyticsTextFixer: Found GameAnalyticsText by name");
                }
            }
        }

        if (analyticsText != null)
        {
            Debug.Log("AnalyticsTextFixer: Found analytics text, configuring it");

            // Get the RectTransform
            analyticsTextRectTransform = analyticsText.GetComponent<RectTransform>();

            // Configure the TextMeshProUGUI component with simpler settings
            analyticsText.horizontalAlignment = HorizontalAlignmentOptions.Left;
            analyticsText.verticalAlignment = VerticalAlignmentOptions.Top;
            analyticsText.enableWordWrapping = true;
            analyticsText.overflowMode = TextOverflowModes.Overflow;
            analyticsText.margin = new Vector4(10, 10, 10, 10); // Simple margins
            analyticsText.alignment = TextAlignmentOptions.TopLeft; // Force top-left alignment

            // Force text to update
            analyticsText.ForceMeshUpdate(true);

            // Configure the RectTransform
            if (analyticsTextRectTransform != null)
            {
                // If we have a scroll rect, make the text a child of the content
                if (scrollRect != null && scrollRect.content != null)
                {
                    analyticsTextRectTransform.SetParent(scrollRect.content, false);

                    // Set anchors to stretch horizontally but not vertically
                    analyticsTextRectTransform.anchorMin = new Vector2(0, 1);
                    analyticsTextRectTransform.anchorMax = new Vector2(1, 1);

                    // Set pivot to top-left
                    analyticsTextRectTransform.pivot = new Vector2(0, 1);

                    // Reset position with a small offset
                    analyticsTextRectTransform.anchoredPosition = new Vector2(5, 0);

                    // Set width to match parent with minimal padding
                    analyticsTextRectTransform.sizeDelta = new Vector2(-10, 0);

                    Debug.Log("AnalyticsTextFixer: RectTransform configured for scroll view");
                }
                else
                {
                    // Set anchors to stretch in both directions
                    analyticsTextRectTransform.anchorMin = new Vector2(0, 0);
                    analyticsTextRectTransform.anchorMax = new Vector2(1, 1);

                    // Reset position
                    analyticsTextRectTransform.anchoredPosition = Vector2.zero;

                    // Set size delta with minimal padding
                    analyticsTextRectTransform.sizeDelta = new Vector2(-10, -10);

                    // Set pivot to top-left
                    analyticsTextRectTransform.pivot = new Vector2(0, 1);

                    Debug.Log("AnalyticsTextFixer: RectTransform configured for standard view");
                }
            }
            else
            {
                Debug.LogError("AnalyticsTextFixer: Could not get RectTransform component");
            }
        }
        else
        {
            Debug.LogError("AnalyticsTextFixer: Could not find analytics text");
        }
    }

    private void SetupScrollView()
    {
        // Check if we already have a ScrollRect
        scrollRect = GetComponent<ScrollRect>();

        if (scrollRect == null)
        {
            Debug.Log("AnalyticsTextFixer: Creating ScrollRect");

            // Create a viewport
            GameObject viewportObj = new GameObject("Viewport");
            RectTransform viewportRect = viewportObj.AddComponent<RectTransform>();
            viewportObj.AddComponent<Image>().color = new Color(1, 1, 1, 0.1f); // Slightly visible for debugging
            viewportObj.AddComponent<Mask>().showMaskGraphic = false;
            viewportRect.SetParent(transform, false);

            // Set viewport to fill the panel with minimal padding
            viewportRect.anchorMin = new Vector2(0, 0);
            viewportRect.anchorMax = new Vector2(1, 1);
            viewportRect.sizeDelta = new Vector2(-5, -5); // Minimal margin
            viewportRect.anchoredPosition = Vector2.zero;

            // Create content
            GameObject contentObj = new GameObject("Content");
            RectTransform contentRect = contentObj.AddComponent<RectTransform>();

            // Configure vertical layout group with minimal padding
            VerticalLayoutGroup vlg = contentObj.AddComponent<VerticalLayoutGroup>();
            vlg.childForceExpandHeight = false;
            vlg.padding = new RectOffset(5, 5, 5, 5); // Minimal padding

            contentObj.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            contentRect.SetParent(viewportRect, false);

            // Set content to fill width but expand height as needed
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0, 1); // Top-left pivot
            contentRect.sizeDelta = new Vector2(0, 0);
            contentRect.anchoredPosition = Vector2.zero;

            // Add ScrollRect to the panel
            scrollRect = gameObject.AddComponent<ScrollRect>();
            scrollRect.viewport = viewportRect;
            scrollRect.content = contentRect;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.scrollSensitivity = 20;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;

            // Move existing text to content if it exists
            TextMeshProUGUI existingText = GetComponentInChildren<TextMeshProUGUI>(true);
            if (existingText != null)
            {
                existingText.transform.SetParent(contentRect, false);
                Debug.Log("AnalyticsTextFixer: Moved existing text to scroll content");
            }

            Debug.Log("AnalyticsTextFixer: ScrollRect setup complete");
        }
        else
        {
            Debug.Log("AnalyticsTextFixer: ScrollRect already exists");
        }
    }

    // Force the text to update after a delay to ensure layout calculations are complete
    private void OnEnable()
    {
        // Invoke the update method after a short delay
        Invoke("ForceTextUpdate", 0.1f);
    }

    // Force the text to update
    private void ForceTextUpdate()
    {
        if (analyticsText != null)
        {
            // Force the text to update
            analyticsText.ForceMeshUpdate(true);

            // Log the current text content for debugging
            Debug.Log($"AnalyticsTextFixer: Forced text update. Text content length: {analyticsText.text.Length}");

            // If we have a scroll rect, force it to update as well
            if (scrollRect != null)
            {
                Canvas.ForceUpdateCanvases();
                scrollRect.verticalNormalizedPosition = 1; // Scroll to top
                Debug.Log("AnalyticsTextFixer: Forced scroll view update");
            }
        }
    }
}
