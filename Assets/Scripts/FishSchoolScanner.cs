using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FishSchoolScanner : MonoBehaviour
{
    public BoidManager boidManager;
    public Transform player;
    public float scanRange = 14f;
    public float scanDuration = 2f;
    public float metricsUpdateInterval = 0.25f;

    TMP_Text objectiveText;
    TMP_Text metricsText;
    TMP_Text resultText;
    Image progressFill;

    MetricsSnapshot currentMetrics;
    MetricsSnapshot capturedMetrics;
    float scanProgress;
    float nextMetricsUpdate;
    bool scanComplete;

    void Start()
    {
        CreateHud();
    }

    void Update()
    {
        if (boidManager == null || player == null)
        {
            return;
        }

        if (Time.time >= nextMetricsUpdate)
        {
            nextMetricsUpdate = Time.time + metricsUpdateInterval;
            currentMetrics = CalculateMetrics(boidManager.Boids);
            UpdateMetricsText();
        }

        UpdateScanState();
    }

    void UpdateScanState()
    {
        float distance = currentMetrics.count > 0
            ? Vector3.Distance(player.position, currentMetrics.center)
            : float.PositiveInfinity;

        bool inRange = distance <= scanRange;
        if (!scanComplete && inRange && Input.GetKey(KeyCode.F))
        {
            scanProgress += Time.deltaTime;
            if (scanProgress >= scanDuration)
            {
                scanProgress = scanDuration;
                scanComplete = true;
                capturedMetrics = currentMetrics;
                UpdateResultText();
            }
        }
        else if (!scanComplete)
        {
            scanProgress = Mathf.MoveTowards(scanProgress, 0f, Time.deltaTime * 1.5f);
        }

        progressFill.fillAmount = scanDuration > Mathf.Epsilon ? scanProgress / scanDuration : 0f;

        if (scanComplete)
        {
            objectiveText.text = "Scan complete. Review the captured schooling metrics.";
        }
        else if (currentMetrics.count == 0)
        {
            objectiveText.text = "Searching for fish school...";
        }
        else if (inRange)
        {
            objectiveText.text = $"Hold F to scan fish school  |  Distance {distance:F1} m";
        }
        else
        {
            objectiveText.text = $"Find the fish school  |  Distance {distance:F1} m";
        }
    }

    void UpdateMetricsText()
    {
        if (currentMetrics.count == 0)
        {
            metricsText.text = "Metrics\nFish Count: 0";
            return;
        }

        metricsText.text =
            "Live Metrics\n" +
            $"Fish Count: {currentMetrics.count}\n" +
            $"Polarization: {currentMetrics.polarization:F2}\n" +
            $"Avg Spread: {currentMetrics.averageSpread:F2}\n" +
            $"Nearest Neighbor: {currentMetrics.averageNearestNeighbor:F2}\n" +
            $"Avg Speed: {currentMetrics.averageSpeed:F2}";
    }

    void UpdateResultText()
    {
        resultText.text =
            "Scan Result\n" +
            $"P: {capturedMetrics.polarization:F2}   " +
            $"Spread: {capturedMetrics.averageSpread:F2}   " +
            $"NN: {capturedMetrics.averageNearestNeighbor:F2}   " +
            $"Speed: {capturedMetrics.averageSpeed:F2}";
    }

    MetricsSnapshot CalculateMetrics(IReadOnlyList<Boid> boids)
    {
        MetricsSnapshot snapshot = new MetricsSnapshot();
        if (boids == null || boids.Count == 0)
        {
            return snapshot;
        }

        Vector3 center = Vector3.zero;
        int validCount = 0;
        for (int i = 0; i < boids.Count; i++)
        {
            Boid boid = boids[i];
            if (boid == null)
            {
                continue;
            }

            center += boid.position;
            validCount++;
        }

        if (validCount == 0)
        {
            return snapshot;
        }

        center /= validCount;
        Vector3 directionSum = Vector3.zero;
        float spreadSum = 0f;
        float nearestSum = 0f;
        float speedSum = 0f;

        for (int i = 0; i < boids.Count; i++)
        {
            Boid boid = boids[i];
            if (boid == null)
            {
                continue;
            }

            Vector3 velocity = boid.Velocity;
            Vector3 direction = velocity.sqrMagnitude > 0.0001f ? velocity.normalized : boid.forward;
            directionSum += direction;
            speedSum += velocity.magnitude;
            spreadSum += Vector3.Distance(boid.position, center);

            float nearest = float.PositiveInfinity;
            for (int j = 0; j < boids.Count; j++)
            {
                if (i == j || boids[j] == null)
                {
                    continue;
                }

                float distance = Vector3.Distance(boid.position, boids[j].position);
                if (distance < nearest)
                {
                    nearest = distance;
                }
            }

            if (!float.IsInfinity(nearest))
            {
                nearestSum += nearest;
            }
        }

        snapshot.count = validCount;
        snapshot.center = center;
        snapshot.polarization = directionSum.magnitude / validCount;
        snapshot.averageSpread = spreadSum / validCount;
        snapshot.averageNearestNeighbor = validCount > 1 ? nearestSum / validCount : 0f;
        snapshot.averageSpeed = speedSum / validCount;
        return snapshot;
    }

    void CreateHud()
    {
        Canvas canvas = new GameObject("Adventure HUD").AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20;
        CanvasScaler scaler = canvas.gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        canvas.gameObject.AddComponent<GraphicRaycaster>();

        GameObject panel = CreatePanel(canvas.transform, "Objective Panel", new Vector2(430f, 160f), new Vector2(24f, -24f), new Vector2(0f, 1f));
        objectiveText = CreateText(panel.transform, "Objective", new Vector2(390f, 54f), new Vector2(20f, -18f), 18f, FontStyles.Bold);
        metricsText = CreateText(panel.transform, "Metrics", new Vector2(390f, 74f), new Vector2(20f, -72f), 14f, FontStyles.Normal);

        GameObject barBack = CreateUiObject(panel.transform, "Scan Progress");
        RectTransform barRect = barBack.AddComponent<RectTransform>();
        barRect.anchorMin = new Vector2(0f, 0f);
        barRect.anchorMax = new Vector2(1f, 0f);
        barRect.pivot = new Vector2(0.5f, 0f);
        barRect.offsetMin = new Vector2(20f, 16f);
        barRect.offsetMax = new Vector2(-20f, 30f);
        Image barImage = barBack.AddComponent<Image>();
        barImage.color = new Color(1f, 1f, 1f, 0.18f);

        GameObject fill = CreateUiObject(barBack.transform, "Fill");
        progressFill = fill.AddComponent<Image>();
        progressFill.color = new Color(0.1f, 0.95f, 1f, 0.95f);
        progressFill.type = Image.Type.Filled;
        progressFill.fillMethod = Image.FillMethod.Horizontal;
        progressFill.fillAmount = 0f;
        RectTransform fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        GameObject resultPanel = CreatePanel(canvas.transform, "Scan Result Panel", new Vector2(620f, 72f), new Vector2(24f, 24f), new Vector2(0f, 0f));
        resultText = CreateText(resultPanel.transform, "Result", new Vector2(580f, 42f), new Vector2(20f, -16f), 16f, FontStyles.Bold);
        resultText.text = "Scan Result\nNo scan captured yet.";
    }

    GameObject CreatePanel(Transform parent, string name, Vector2 size, Vector2 anchoredPosition, Vector2 anchor)
    {
        GameObject panel = CreateUiObject(parent, name);
        RectTransform rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.sizeDelta = size;
        rect.anchoredPosition = anchoredPosition;
        Image image = panel.AddComponent<Image>();
        image.color = new Color(0.01f, 0.06f, 0.1f, 0.78f);
        return panel;
    }

    TMP_Text CreateText(Transform parent, string name, Vector2 size, Vector2 anchoredPosition, float fontSize, FontStyles style)
    {
        GameObject textObject = CreateUiObject(parent, name);
        RectTransform rect = textObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.sizeDelta = size;
        rect.anchoredPosition = anchoredPosition;

        TMP_Text text = textObject.AddComponent<TextMeshProUGUI>();
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.color = Color.white;
        text.enableWordWrapping = true;
        return text;
    }

    GameObject CreateUiObject(Transform parent, string name)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go;
    }

    struct MetricsSnapshot
    {
        public int count;
        public Vector3 center;
        public float polarization;
        public float averageSpread;
        public float averageNearestNeighbor;
        public float averageSpeed;
    }
}
