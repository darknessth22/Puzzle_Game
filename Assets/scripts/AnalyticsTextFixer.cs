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
        SetupScrollView();

        if (analyticsText == null)
        {
            analyticsText = GetComponentInChildren<TextMeshProUGUI>(true);

            if (analyticsText == null)
            {
                GameObject textObj = GameObject.Find("GameAnalyticsText");
                if (textObj != null)
                {
                    analyticsText = textObj.GetComponent<TextMeshProUGUI>();
                }
            }
        }

        if (analyticsText != null)
        {
            analyticsTextRectTransform = analyticsText.GetComponent<RectTransform>();

            analyticsText.horizontalAlignment = HorizontalAlignmentOptions.Left;
            analyticsText.verticalAlignment = VerticalAlignmentOptions.Top;
            analyticsText.enableWordWrapping = true;
            analyticsText.overflowMode = TextOverflowModes.Overflow;
            analyticsText.margin = new Vector4(10, 10, 10, 10);
            analyticsText.alignment = TextAlignmentOptions.TopLeft;

            analyticsText.ForceMeshUpdate(true);

            if (analyticsTextRectTransform != null)
            {
                if (scrollRect != null && scrollRect.content != null)
                {
                    analyticsTextRectTransform.SetParent(scrollRect.content, false);

                    analyticsTextRectTransform.anchorMin = new Vector2(0, 1);
                    analyticsTextRectTransform.anchorMax = new Vector2(1, 1);

                    analyticsTextRectTransform.pivot = new Vector2(0, 1);

                    analyticsTextRectTransform.anchoredPosition = new Vector2(5, 0);

                    analyticsTextRectTransform.sizeDelta = new Vector2(-10, 0);
                }
                else
                {
                    analyticsTextRectTransform.anchorMin = new Vector2(0, 0);
                    analyticsTextRectTransform.anchorMax = new Vector2(1, 1);

                    analyticsTextRectTransform.anchoredPosition = Vector2.zero;

                    analyticsTextRectTransform.sizeDelta = new Vector2(-10, -10);

                    analyticsTextRectTransform.pivot = new Vector2(0, 1);
                }
            }
        }
    }

    private void SetupScrollView()
    {
        scrollRect = GetComponent<ScrollRect>();

        if (scrollRect == null)
        {
            GameObject viewportObj = new GameObject("Viewport");
            RectTransform viewportRect = viewportObj.AddComponent<RectTransform>();
            viewportObj.AddComponent<Image>().color = new Color(1, 1, 1, 0.1f);
            viewportObj.AddComponent<Mask>().showMaskGraphic = false;
            viewportRect.SetParent(transform, false);

            viewportRect.anchorMin = new Vector2(0, 0);
            viewportRect.anchorMax = new Vector2(1, 1);
            viewportRect.sizeDelta = new Vector2(-5, -5);
            viewportRect.anchoredPosition = Vector2.zero;

            GameObject contentObj = new GameObject("Content");
            RectTransform contentRect = contentObj.AddComponent<RectTransform>();

            VerticalLayoutGroup vlg = contentObj.AddComponent<VerticalLayoutGroup>();
            vlg.childForceExpandHeight = false;
            vlg.padding = new RectOffset(5, 5, 5, 5);

            contentObj.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            contentRect.SetParent(viewportRect, false);

            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0, 1);
            contentRect.sizeDelta = new Vector2(0, 0);
            contentRect.anchoredPosition = Vector2.zero;

            scrollRect = gameObject.AddComponent<ScrollRect>();
            scrollRect.viewport = viewportRect;
            scrollRect.content = contentRect;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.scrollSensitivity = 20;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;

            TextMeshProUGUI existingText = GetComponentInChildren<TextMeshProUGUI>(true);
            if (existingText != null)
            {
                existingText.transform.SetParent(contentRect, false);
            }
        }
    }

    private void OnEnable()
    {
        Invoke("ForceTextUpdate", 0.1f);
    }

    private void ForceTextUpdate()
    {
        if (analyticsText != null)
        {
            analyticsText.ForceMeshUpdate(true);

            if (scrollRect != null)
            {
                Canvas.ForceUpdateCanvases();
                scrollRect.verticalNormalizedPosition = 1;
            }
        }
    }
}
