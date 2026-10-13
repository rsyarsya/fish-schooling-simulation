using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class BoidMetrics : MonoBehaviour {

    const float MinDirectionSqrMagnitude = 0.0001f;

    public BoidManager manager;
    public TextMeshProUGUI metricsText;
    public float updateInterval = 0.5f;
    public bool createRuntimePanel = true;

    public float Polarization { get; private set; }
    public float AverageSpread { get; private set; }
    public float AverageNearestNeighborDistance { get; private set; }
    public float AverageSpeed { get; private set; }
    public float CurrentForceMagnitude { get; private set; }
    public float AverageTurbulenceForce { get; private set; }
    public int FishCount { get; private set; }

    float timer;
    readonly StringBuilder textBuilder = new StringBuilder (220);

    void Awake () {
        BoidMetrics[] metricsInstances = FindObjectsByType<BoidMetrics> ();
        if (metricsInstances.Length > 1) {
            Destroy (gameObject);
            return;
        }

        DontDestroyOnLoad (gameObject);
        RefreshSceneReferences ();
    }

    void OnEnable () {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable () {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded (Scene scene, LoadSceneMode mode) {
        RefreshSceneReferences ();
    }

    void RefreshSceneReferences () {
        manager = FindAnyObjectByType<BoidManager> ();

        if (metricsText == null && createRuntimePanel) {
            metricsText = CreateRuntimeMetricsPanel ();
        }

        timer = updateInterval;
    }

    void Update () {
        timer += Time.deltaTime;
        if (timer < updateInterval) {
            return;
        }

        timer = 0;

        if (manager == null) {
            manager = FindAnyObjectByType<BoidManager> ();
        }

        ComputeMetrics ();
        UpdateMetricsText ();
    }

    void ComputeMetrics () {
        if (manager == null || manager.Boids == null || manager.Boids.Count == 0) {
            FishCount = 0;
            Polarization = 0;
            AverageSpread = 0;
            AverageNearestNeighborDistance = 0;
            AverageSpeed = 0;
            CurrentForceMagnitude = 0;
            AverageTurbulenceForce = 0;
            return;
        }

        var boids = manager.Boids;
        int count = boids.Count;

        FishCount = count;

        Vector3 directionSum = Vector3.zero;
        Vector3 positionSum = Vector3.zero;
        float speedSum = 0;
        float turbulenceForceSum = 0;

        for (int i = 0; i < count; i++) {
            Boid boid = boids[i];
            Vector3 velocity = boid.Velocity;
            float speed = velocity.magnitude;
            Vector3 direction = speed > MinDirectionSqrMagnitude ? velocity / speed : boid.transform.forward;

            directionSum += direction.normalized;
            positionSum += boid.position;
            speedSum += speed;
            turbulenceForceSum += boid.StochasticForce.magnitude;
        }

        Vector3 centroid = positionSum / count;
        float spreadSum = 0;
        float nearestNeighborSum = 0;

        for (int i = 0; i < count; i++) {
            Vector3 position = boids[i].position;
            spreadSum += Vector3.Distance (position, centroid);

            float nearestSqrDistance = float.PositiveInfinity;
            for (int j = 0; j < count; j++) {
                if (i == j) {
                    continue;
                }

                float sqrDistance = (position - boids[j].position).sqrMagnitude;
                if (sqrDistance < nearestSqrDistance) {
                    nearestSqrDistance = sqrDistance;
                }
            }

            if (!float.IsInfinity (nearestSqrDistance)) {
                nearestNeighborSum += Mathf.Sqrt (nearestSqrDistance);
            }
        }

        Polarization = directionSum.magnitude / count;
        AverageSpread = spreadSum / count;
        AverageNearestNeighborDistance = count > 1 ? nearestNeighborSum / count : 0;
        AverageSpeed = speedSum / count;
        CurrentForceMagnitude = manager.Settings != null ? manager.Settings.currentForce.magnitude : 0;
        AverageTurbulenceForce = turbulenceForceSum / count;
    }

    void UpdateMetricsText () {
        if (metricsText == null) {
            return;
        }

        textBuilder.Clear ();
        textBuilder.AppendLine ("Metrics");
        textBuilder.Append ("Fish Count: ").Append (FishCount).AppendLine ();
        textBuilder.Append ("Polarization: ").Append (Polarization.ToString ("F2")).AppendLine ();
        textBuilder.Append ("Avg Spread: ").Append (AverageSpread.ToString ("F2")).AppendLine ();
        textBuilder.Append ("Nearest Neighbor: ").Append (AverageNearestNeighborDistance.ToString ("F2")).AppendLine ();
        textBuilder.Append ("Avg Speed: ").Append (AverageSpeed.ToString ("F2")).AppendLine ();
        textBuilder.Append ("Current Mag: ").Append (CurrentForceMagnitude.ToString ("F2")).AppendLine ();
        textBuilder.Append ("Avg Turbulence: ").Append (AverageTurbulenceForce.ToString ("F2"));

        metricsText.text = textBuilder.ToString ();
    }

    TextMeshProUGUI CreateRuntimeMetricsPanel () {
        Canvas canvas = FindAnyObjectByType<Canvas> ();
        if (canvas == null) {
            GameObject canvasObject = new GameObject ("Metrics Canvas");
            canvas = canvasObject.AddComponent<Canvas> ();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<CanvasScaler> ();
            canvasObject.AddComponent<GraphicRaycaster> ();
        }

        GameObject panelObject = new GameObject ("Metrics Panel", typeof (RectTransform), typeof (Image));
        panelObject.transform.SetParent (canvas.transform, false);

        RectTransform panelRect = panelObject.GetComponent<RectTransform> ();
        panelRect.anchorMin = new Vector2 (1, 1);
        panelRect.anchorMax = new Vector2 (1, 1);
        panelRect.pivot = new Vector2 (1, 1);
        panelRect.anchoredPosition = RuntimeViewMode.IsMobileLike ? new Vector2 (-16, -16) : new Vector2 (-24, -24);
        panelRect.sizeDelta = RuntimeViewMode.IsMobileLike ? new Vector2 (250, 168) : new Vector2 (300, 210);

        Image panelImage = panelObject.GetComponent<Image> ();
        panelImage.color = new Color (0, 0, 0, 0.45f);
        panelImage.raycastTarget = false;

        GameObject textObject = new GameObject ("Metrics Text", typeof (RectTransform), typeof (TextMeshProUGUI));
        textObject.transform.SetParent (panelObject.transform, false);

        RectTransform textRect = textObject.GetComponent<RectTransform> ();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2 (12, 10);
        textRect.offsetMax = new Vector2 (-34, -10);

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI> ();
        text.raycastTarget = false;
        text.fontSize = RuntimeViewMode.IsMobileLike ? 15 : 20;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.TopLeft;
        text.text = "Metrics\nWaiting for boids...";

        CreateMetricInfoButtons (panelObject.transform);
        return text;
    }

    void CreateMetricInfoButtons (Transform parent) {
        float buttonSize = RuntimeViewMode.IsMobileLike ? 16 : 17;
        float startY = RuntimeViewMode.IsMobileLike ? -30 : -40;
        float rowStep = RuntimeViewMode.IsMobileLike ? 18 : 24;

        CreateMetricInfoButton (parent, "Fish Count", "Jumlah ikan aktif yang sedang dihitung oleh BoidManager.", startY, buttonSize);
        CreateMetricInfoButton (parent, "Polarization", "Ukuran keseragaman arah gerak kawanan. Nilai mendekati 1 berarti ikan bergerak sangat searah.", startY - rowStep, buttonSize);
        CreateMetricInfoButton (parent, "Avg Spread", "Rata-rata jarak ikan dari pusat kawanan. Nilai tinggi berarti kawanan lebih menyebar.", startY - rowStep * 2, buttonSize);
        CreateMetricInfoButton (parent, "Nearest Neighbor", "Rata-rata jarak ke ikan terdekat. Berguna untuk melihat apakah kawanan terlalu rapat atau terlalu renggang.", startY - rowStep * 3, buttonSize);
        CreateMetricInfoButton (parent, "Avg Speed", "Rata-rata kecepatan ikan saat ini. Nilai ini membantu membandingkan mode visual dan paper-based.", startY - rowStep * 4, buttonSize);
        CreateMetricInfoButton (parent, "Current Mag", "Besar gaya arus global yang sedang aktif pada simulasi.", startY - rowStep * 5, buttonSize);
        CreateMetricInfoButton (parent, "Avg Turbulence", "Rata-rata gangguan stokastik per ikan. Nilai ini membantu membuktikan efek Box-Muller/turbulence secara kuantitatif.", startY - rowStep * 6, buttonSize);
    }

    void CreateMetricInfoButton (Transform parent, string title, string body, float y, float buttonSize) {
        RuntimeInfoPopup.CreateInfoButton (parent, title, body, new Vector2 (-10, y), new Vector2 (buttonSize, buttonSize), new Vector2 (1, 1), new Vector2 (1, 1), RuntimeViewMode.IsMobileLike ? 9 : 10);
    }

}
