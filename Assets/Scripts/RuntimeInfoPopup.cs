using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class RuntimeInfoPopup {

    static GameObject popupPanel;
    static TextMeshProUGUI popupTitleText;
    static TextMeshProUGUI popupBodyText;

    public static Button CreateInfoButton (Transform parent, string title, string body, Vector2 anchoredPosition, Vector2 size, Vector2 anchorMin, Vector2 anchorMax, float fontSize = 13f) {
        GameObject buttonObject = new GameObject (title + " Info Button", typeof (RectTransform), typeof (Image), typeof (Button));
        buttonObject.transform.SetParent (parent, false);

        RectTransform rect = buttonObject.GetComponent<RectTransform> ();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2 (anchorMin.x, anchorMax.y);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        Image image = buttonObject.GetComponent<Image> ();
        image.color = new Color (1f, 1f, 1f, 0.16f);

        Button button = buttonObject.GetComponent<Button> ();
        button.targetGraphic = image;
        button.onClick.AddListener (() => Show (parent, title, body));

        GameObject labelObject = new GameObject ("Label", typeof (RectTransform), typeof (TextMeshProUGUI));
        labelObject.transform.SetParent (buttonObject.transform, false);

        RectTransform labelRect = labelObject.GetComponent<RectTransform> ();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI> ();
        label.raycastTarget = false;
        label.text = "i";
        label.fontSize = fontSize;
        label.fontStyle = FontStyles.Bold;
        label.color = new Color (1f, 1f, 1f, 0.9f);
        label.alignment = TextAlignmentOptions.Center;

        EnsureEventSystemExists ();
        return button;
    }

    static void Show (Transform source, string title, string body) {
        Canvas canvas = source.GetComponentInParent<Canvas> ();
        if (canvas == null) {
            canvas = Object.FindAnyObjectByType<Canvas> ();
        }

        if (canvas == null) {
            return;
        }

        EnsurePopupExists (canvas);
        popupTitleText.text = title;
        popupBodyText.text = body;
        popupPanel.transform.SetAsLastSibling ();
        popupPanel.SetActive (true);
    }

    static void EnsurePopupExists (Canvas canvas) {
        if (popupPanel != null) {
            if (popupPanel.transform.parent != canvas.transform) {
                popupPanel.transform.SetParent (canvas.transform, false);
            }
            return;
        }

        popupPanel = new GameObject ("Runtime Info Popup", typeof (RectTransform), typeof (Image));
        popupPanel.transform.SetParent (canvas.transform, false);

        Vector2 panelSize = RuntimeViewMode.IsMobileLike ? new Vector2 (340, 190) : new Vector2 (380, 170);

        RectTransform panelRect = popupPanel.GetComponent<RectTransform> ();
        panelRect.anchorMin = new Vector2 (0.5f, 0.5f);
        panelRect.anchorMax = new Vector2 (0.5f, 0.5f);
        panelRect.pivot = new Vector2 (0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = panelSize;

        Image panelImage = popupPanel.GetComponent<Image> ();
        panelImage.color = new Color (0f, 0f, 0f, 0.78f);
        panelImage.raycastTarget = true;

        GameObject headerObject = new GameObject ("Header", typeof (RectTransform), typeof (Image));
        headerObject.transform.SetParent (popupPanel.transform, false);

        RectTransform headerRect = headerObject.GetComponent<RectTransform> ();
        headerRect.anchorMin = new Vector2 (0, 1);
        headerRect.anchorMax = new Vector2 (1, 1);
        headerRect.pivot = new Vector2 (0.5f, 1);
        headerRect.anchoredPosition = Vector2.zero;
        headerRect.sizeDelta = new Vector2 (0, 42);

        Image headerImage = headerObject.GetComponent<Image> ();
        headerImage.color = new Color (1f, 1f, 1f, 0.08f);
        headerImage.raycastTarget = false;

        popupTitleText = CreateAnchoredText (popupPanel.transform, "Title", new Vector2 (18, -11), new Vector2 (panelSize.x - 72, 24), new Vector2 (0, 1), new Vector2 (0, 1), new Vector2 (0, 1), 18, TextAlignmentOptions.Left);
        popupBodyText = CreateAnchoredText (popupPanel.transform, "Body", new Vector2 (18, -56), new Vector2 (panelSize.x - 36, panelSize.y - 72), new Vector2 (0, 1), new Vector2 (0, 1), new Vector2 (0, 1), RuntimeViewMode.IsMobileLike ? 14 : 15, TextAlignmentOptions.TopLeft);

        Button closeButton = CreateCloseButton (popupPanel.transform);
        closeButton.onClick.AddListener (() => popupPanel.SetActive (false));
        popupPanel.SetActive (false);
    }

    static TextMeshProUGUI CreateAnchoredText (Transform parent, string objectName, Vector2 anchoredPosition, Vector2 size, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, float fontSize, TextAlignmentOptions alignment) {
        GameObject textObject = new GameObject (objectName, typeof (RectTransform), typeof (TextMeshProUGUI));
        textObject.transform.SetParent (parent, false);

        RectTransform rect = textObject.GetComponent<RectTransform> ();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI> ();
        text.raycastTarget = false;
        text.fontSize = fontSize;
        text.color = Color.white;
        text.alignment = alignment;
        text.textWrappingMode = TextWrappingModes.Normal;
        return text;
    }

    static Button CreateCloseButton (Transform parent) {
        GameObject buttonObject = new GameObject ("Close Button", typeof (RectTransform), typeof (Image), typeof (Button));
        buttonObject.transform.SetParent (parent, false);

        RectTransform rect = buttonObject.GetComponent<RectTransform> ();
        rect.anchorMin = new Vector2 (1, 1);
        rect.anchorMax = new Vector2 (1, 1);
        rect.pivot = new Vector2 (1, 1);
        rect.anchoredPosition = new Vector2 (-10, -8);
        rect.sizeDelta = new Vector2 (30, 28);

        Image image = buttonObject.GetComponent<Image> ();
        image.color = new Color (1f, 1f, 1f, 0.9f);

        Button button = buttonObject.GetComponent<Button> ();
        button.targetGraphic = image;

        GameObject labelObject = new GameObject ("Label", typeof (RectTransform), typeof (TextMeshProUGUI));
        labelObject.transform.SetParent (buttonObject.transform, false);

        RectTransform labelRect = labelObject.GetComponent<RectTransform> ();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI> ();
        label.raycastTarget = false;
        label.text = "X";
        label.fontSize = 14;
        label.color = Color.black;
        label.alignment = TextAlignmentOptions.Center;

        return button;
    }

    static void EnsureEventSystemExists () {
        if (EventSystem.current != null) {
            return;
        }

        GameObject eventSystemObject = new GameObject ("EventSystem");
        eventSystemObject.AddComponent<EventSystem> ();
        eventSystemObject.AddComponent<StandaloneInputModule> ();
    }
}
