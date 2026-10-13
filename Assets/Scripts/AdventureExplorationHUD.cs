using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AdventureExplorationHUD : MonoBehaviour
{
    public string controlsText = "W/S thrust  |  A/D yaw  |  Mouse steer  |  Shift boost  |  Esc unlock";

    void Start()
    {
        CreateHud();
    }

    void CreateHud()
    {
        Canvas canvas = new GameObject("Adventure Exploration HUD").AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        CanvasScaler scaler = canvas.gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        canvas.gameObject.AddComponent<GraphicRaycaster>();

        GameObject panel = new GameObject("Controls Hint", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(canvas.transform, false);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0f);
        panelRect.anchorMax = new Vector2(0.5f, 0f);
        panelRect.pivot = new Vector2(0.5f, 0f);
        panelRect.sizeDelta = new Vector2(680f, 42f);
        panelRect.anchoredPosition = new Vector2(0f, 22f);

        Image panelImage = panel.GetComponent<Image>();
        panelImage.color = new Color(0.01f, 0.05f, 0.08f, 0.58f);

        GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(panel.transform, false);
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(14f, 4f);
        textRect.offsetMax = new Vector2(-14f, -4f);

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.text = controlsText;
        text.fontSize = 15f;
        text.alignment = TextAlignmentOptions.Center;
        text.color = new Color(0.86f, 0.98f, 1f, 0.9f);
        text.enableWordWrapping = false;
    }
}
