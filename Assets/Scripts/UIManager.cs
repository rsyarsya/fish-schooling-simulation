using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class UIManager : MonoBehaviour
{
    private const int MinFishCount = 10;
    private const int MaxFishCount = 500;
    private const float MobilePanelWidth = 390f;
    private const float MobilePanelHeight = 430f;
    private const float MobileSliderSpacing = 56f;
    private const float DesktopPanelWidth = 350f;
    private const float DesktopPanelHeight = 500f;
    private const float DesktopSliderSpacing = 62f;

    private enum SettingsPreset
    {
        VisualDemo,
        PaperBased
    }

    public BoidSettings settings;
    public BoidSettings visualDemoSettings;
    public BoidSettings paperBasedSettings;
    public StochasticCurrent stochasticCurrent;
    public bool createPresetPanel = true;
    public bool forceMobileModeInEditor;

    [Header("UI Text References (TextMeshPro)")]
    public TextMeshProUGUI sepText;
    public TextMeshProUGUI aliText;
    public TextMeshProUGUI cohText;
    public TextMeshProUGUI sigmaText;
    public TextMeshProUGUI intervalText;
    public TextMeshProUGUI fishCountText;

    [Header("UI Slider References")]
    public Slider sepSlider;
    public Slider aliSlider;
    public Slider cohSlider;
    public Slider sigmaSlider;
    public Slider intervalSlider;
    public Slider fishCountSlider;

    private static int? pendingFishCount;
    private static SettingsPreset? pendingPreset;
    private SettingsPreset currentPreset;
    private SettingsPreset selectedPreset;
    private TextMeshProUGUI presetModeText;
    private TextMeshProUGUI presetSwitchText;
    private TextMeshProUGUI presetApplyText;
    private GameObject desktopControlsPanel;
    private TextMeshProUGUI desktopFishStatusText;
    private GameObject mobileSettingsPanel;
    private TextMeshProUGUI mobileSettingsButtonText;
    private bool mobileSettingsOpen;

    void Awake()
    {
        RuntimeViewMode.RegisterForceMobileModeInEditor(forceMobileModeInEditor);

        if (settings == null && visualDemoSettings == null)
        {
            Debug.LogError("[UIManager] BoidSettings belum di-assign!");
            return;
        }

        if (visualDemoSettings == null)
        {
            visualDemoSettings = settings;
        }

        currentPreset = pendingPreset ?? SettingsPreset.VisualDemo;
        selectedPreset = currentPreset;

        BoidSettings sourceSettings = GetSettingsSource(currentPreset);
        settings = Instantiate(sourceSettings);
        pendingPreset = null;

        if (pendingFishCount.HasValue)
        {
            settings.fishCount = Mathf.Clamp(pendingFishCount.Value, MinFishCount, GetRuntimeMaxFishCount());
            pendingFishCount = null;
        }

        ApplyRuntimeSettings();
    }

    void Start()
    {
        if (settings == null)
        {
            return;
        }

        if (sepSlider) sepSlider.value = settings.seperateWeight;
        if (aliSlider) aliSlider.value = settings.alignWeight;
        if (cohSlider) cohSlider.value = settings.cohesionWeight;
        if (sigmaSlider) sigmaSlider.value = settings.sigma;
        if (intervalSlider) intervalSlider.value = settings.currentInterval;
        if (fishCountSlider)
        {
            fishCountSlider.maxValue = GetRuntimeMaxFishCount();
            settings.fishCount = Mathf.Clamp(settings.fishCount, MinFishCount, GetRuntimeMaxFishCount());
            fishCountSlider.value = settings.fishCount;
        }

        UpdateLabels();
        CreatePresetPanel();
        UpdatePresetPanel();
        CreateRuntimeControls();
    }

    void ApplyRuntimeSettings()
    {
        BoidManager[] managers = FindObjectsByType<BoidManager>();
        foreach (BoidManager manager in managers)
        {
            manager.settings = settings;
        }

        if (stochasticCurrent == null)
        {
            stochasticCurrent = FindAnyObjectByType<StochasticCurrent>();
        }

        if (stochasticCurrent != null)
        {
            stochasticCurrent.settings = settings;
        }

        Spawner[] spawners = FindObjectsByType<Spawner>();
        foreach (Spawner spawner in spawners)
        {
            spawner.settings = settings;
        }
    }

    public void OnSeparationChanged(float value)
    {
        if (settings != null) settings.seperateWeight = value;
        if (sepText != null) sepText.text = $"Separation: {value:F2}";
    }

    public void OnAlignmentChanged(float value)
    {
        if (settings != null) settings.alignWeight = value;
        if (aliText != null) aliText.text = $"Alignment: {value:F2}";
    }

    public void OnCohesionChanged(float value)
    {
        if (settings != null) settings.cohesionWeight = value;
        if (cohText != null) cohText.text = $"Cohesion: {value:F2}";
    }

    public void OnSigmaChanged(float value)
    {
        if (settings != null) settings.sigma = value;
        if (stochasticCurrent != null) stochasticCurrent.SetSigma(value);
        if (sigmaText != null) sigmaText.text = $"σ Arus: {value:F2}";
    }

    public void OnIntervalChanged(float value)
    {
        if (settings != null) settings.currentInterval = value;
        if (stochasticCurrent != null) stochasticCurrent.SetInterval(value);
        if (intervalText != null) intervalText.text = $"Interval Arus: {value:F1}s";
    }

    public void OnFishCountChanged(float value)
    {
        int count = Mathf.Clamp(Mathf.RoundToInt(value), MinFishCount, GetRuntimeMaxFishCount());
        if (settings != null) settings.fishCount = count;
        if (fishCountText != null)
            fishCountText.text = $"Jumlah Ikan: {count} (Restart untuk apply)";
        if (desktopFishStatusText != null)
            desktopFishStatusText.text = "Restart required";
    }

    public void RestartScene()
    {
        if (settings != null)
        {
            pendingFishCount = Mathf.Clamp(settings.fishCount, MinFishCount, GetRuntimeMaxFishCount());
        }

        pendingPreset = currentPreset;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void CycleSelectedPreset()
    {
        selectedPreset = selectedPreset == SettingsPreset.VisualDemo ? SettingsPreset.PaperBased : SettingsPreset.VisualDemo;
        UpdatePresetPanel();
    }

    public void ApplySelectedPresetAndRestart()
    {
        pendingPreset = selectedPreset;
        pendingFishCount = null;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void UpdateLabels()
    {
        if (settings == null) return;
        if (sepText) sepText.text = $"Separation: {settings.seperateWeight:F2}";
        if (aliText) aliText.text = $"Alignment: {settings.alignWeight:F2}";
        if (cohText) cohText.text = $"Cohesion: {settings.cohesionWeight:F2}";
        if (sigmaText) sigmaText.text = $"σ Arus: {settings.sigma:F2}";
        if (intervalText) intervalText.text = $"Interval Arus: {settings.currentInterval:F1}s";
        if (fishCountText) fishCountText.text = $"Jumlah Ikan: {settings.fishCount}";
    }

    private int GetRuntimeMaxFishCount()
    {
        if (settings != null && RuntimeViewMode.IsMobileLike)
        {
            return Mathf.Clamp(settings.mobileFishCountCap, MinFishCount, MaxFishCount);
        }

        return MaxFishCount;
    }

    private BoidSettings GetSettingsSource(SettingsPreset preset)
    {
        if (preset == SettingsPreset.PaperBased && paperBasedSettings != null)
        {
            return paperBasedSettings;
        }

        if (visualDemoSettings != null)
        {
            return visualDemoSettings;
        }

        return settings;
    }

    private void CreatePresetPanel()
    {
        if (!createPresetPanel || presetModeText != null)
        {
            return;
        }

        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            return;
        }

        GameObject panelObject = new GameObject("Settings Preset Panel", typeof(RectTransform), typeof(Image));
        panelObject.transform.SetParent(canvas.transform, false);

        RectTransform panelRect = panelObject.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(1, 0);
        panelRect.anchorMax = new Vector2(1, 0);
        panelRect.pivot = new Vector2(1, 0);
        panelRect.anchoredPosition = RuntimeViewMode.IsMobileLike ? new Vector2(-24, 188) : new Vector2(-24, 24);
        panelRect.sizeDelta = RuntimeViewMode.IsMobileLike ? new Vector2(340, 148) : new Vector2(320, 132);

        Image panelImage = panelObject.GetComponent<Image>();
        panelImage.color = new Color(0, 0, 0, 0.45f);
        panelImage.raycastTarget = false;

        presetModeText = CreatePresetText(panelObject.transform, "Preset Mode Text", new Vector2(12, -56), new Vector2(-12, -12), 20);
        Button switchButton = CreatePresetButton(panelObject.transform, "Preset Switch Button", new Vector2(12, 18), new Vector2(136, 38));
        Button applyButton = CreatePresetButton(panelObject.transform, "Preset Apply Button", new Vector2(160, 18), new Vector2(148, 38));

        presetSwitchText = switchButton.GetComponentInChildren<TextMeshProUGUI>();
        presetApplyText = applyButton.GetComponentInChildren<TextMeshProUGUI>();

        switchButton.onClick.AddListener(CycleSelectedPreset);
        applyButton.onClick.AddListener(ApplySelectedPresetAndRestart);
    }

    private TextMeshProUGUI CreatePresetText(Transform parent, string objectName, Vector2 offsetMin, Vector2 offsetMax, float fontSize)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);

        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(1, 1);
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.raycastTarget = false;
        text.fontSize = fontSize;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.TopLeft;
        return text;
    }

    private Button CreatePresetButton(Transform parent, string objectName, Vector2 anchoredPosition, Vector2 size)
    {
        GameObject buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 0);
        rect.anchorMax = new Vector2(0, 0);
        rect.pivot = new Vector2(0, 0);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(1, 1, 1, 0.9f);

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;

        GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(buttonObject.transform, false);

        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(8, 4);
        labelRect.offsetMax = new Vector2(-8, -4);

        TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
        label.raycastTarget = false;
        label.fontSize = RuntimeViewMode.IsMobileLike ? 16 : 15;
        label.color = Color.black;
        label.alignment = TextAlignmentOptions.Center;

        return button;
    }

    private void UpdatePresetPanel()
    {
        if (presetModeText == null)
        {
            return;
        }

        presetModeText.text = $"Mode: {GetPresetDisplayName(currentPreset)}\nSelected: {GetPresetDisplayName(selectedPreset)}";

        if (presetSwitchText != null)
        {
            presetSwitchText.text = selectedPreset == SettingsPreset.VisualDemo ? "Paper Based" : "Visual Demo";
        }

        if (presetApplyText != null)
        {
            presetApplyText.text = "Apply & Restart";
        }
    }

    private static string GetPresetDisplayName(SettingsPreset preset)
    {
        return preset == SettingsPreset.PaperBased ? "Paper Based" : "Visual Demo";
    }

    private static string GetControlInfo(string label)
    {
        switch (label)
        {
            case "Separation":
                return "Mengatur seberapa kuat ikan menjaga jarak dari tetangga terdekat. Nilai tinggi membantu mengurangi ikan terlalu menempel.";
            case "Alignment":
                return "Mengatur kecenderungan ikan mengikuti arah rata-rata tetangga. Nilai tinggi membuat kawanan lebih kompak satu arah.";
            case "Cohesion":
                return "Mengatur tarikan ikan ke pusat kawanan terdekat. Nilai tinggi membuat ikan lebih mudah berkumpul.";
            case "Ïƒ Arus":
            case "Sigma Arus":
                return "Mengatur kekuatan gangguan arus stokastik. Nilai tinggi membuat arah gerak ikan lebih banyak terganggu arus.";
            case "Interval Arus":
                return "Mengatur jeda perubahan arus. Nilai kecil membuat arus berubah lebih sering.";
            case "Fish Count":
            case "Jumlah Ikan":
                return "Mengatur jumlah ikan yang akan dibuat saat scene dimulai ulang. Perubahan jumlah butuh restart scene.";
            default:
                return "Parameter simulasi yang dapat diubah saat demo berjalan.";
        }
    }

    private void CreateRuntimeControls()
    {
        if (RuntimeViewMode.IsMobileLike)
        {
            if (CreateMobileSettingsPanel())
            {
                SetLegacyControlsActive(false);
            }
            return;
        }

        if (CreateDesktopControlPanel())
        {
            SetLegacyControlsActive(false);
        }
    }

    private bool CreateDesktopControlPanel()
    {
        if (desktopControlsPanel != null)
        {
            return true;
        }

        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            return false;
        }

        desktopControlsPanel = new GameObject("Desktop Controls Panel", typeof(RectTransform), typeof(Image));
        desktopControlsPanel.transform.SetParent(canvas.transform, false);

        RectTransform panelRect = desktopControlsPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0, 1);
        panelRect.anchorMax = new Vector2(0, 1);
        panelRect.pivot = new Vector2(0, 1);
        panelRect.anchoredPosition = new Vector2(24, -24);
        panelRect.sizeDelta = new Vector2(DesktopPanelWidth, DesktopPanelHeight);

        Image panelImage = desktopControlsPanel.GetComponent<Image>();
        panelImage.color = new Color(0, 0, 0, 0.48f);
        panelImage.raycastTarget = true;

        TextMeshProUGUI title = CreateDesktopText(desktopControlsPanel.transform, "Desktop Controls Title", new Vector2(18, -18), new Vector2(DesktopPanelWidth - 36, 28), new Vector2(0, 1), new Vector2(0, 1), 20, TextAlignmentOptions.Left);
        title.text = "Controls";

        float y = -68;
        CreateDesktopSlider(desktopControlsPanel.transform, "Separation", settings.seperateWeight, 0, 5, y, OnSeparationChanged);
        y -= DesktopSliderSpacing;
        CreateDesktopSlider(desktopControlsPanel.transform, "Alignment", settings.alignWeight, 0, 5, y, OnAlignmentChanged);
        y -= DesktopSliderSpacing;
        CreateDesktopSlider(desktopControlsPanel.transform, "Cohesion", settings.cohesionWeight, 0, 5, y, OnCohesionChanged);
        y -= DesktopSliderSpacing;
        CreateDesktopSlider(desktopControlsPanel.transform, "σ Arus", settings.sigma, 0, 2, y, OnSigmaChanged);
        y -= DesktopSliderSpacing;
        CreateDesktopSlider(desktopControlsPanel.transform, "Interval Arus", settings.currentInterval, 0.5f, 5, y, OnIntervalChanged);
        y -= DesktopSliderSpacing;
        CreateDesktopSlider(desktopControlsPanel.transform, "Fish Count", settings.fishCount, MinFishCount, GetRuntimeMaxFishCount(), y, OnFishCountChanged, true);

        desktopFishStatusText = CreateDesktopText(desktopControlsPanel.transform, "Desktop Restart Status", new Vector2(18, 62), new Vector2(DesktopPanelWidth - 36, 20), Vector2.zero, Vector2.zero, 13, TextAlignmentOptions.Left);
        desktopFishStatusText.text = "Fish count applies after restart";
        desktopFishStatusText.color = new Color(1f, 1f, 1f, 0.72f);

        Button restartButton = CreateDesktopButton(desktopControlsPanel.transform, "Desktop Restart Button", new Vector2(18, 18), new Vector2(DesktopPanelWidth - 36, 42));
        restartButton.GetComponentInChildren<TextMeshProUGUI>().text = "Restart";
        restartButton.onClick.AddListener(RestartScene);
        return true;
    }

    private bool CreateMobileSettingsPanel()
    {
        if (!RuntimeViewMode.IsMobileLike)
        {
            return false;
        }

        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            return false;
        }

        Button settingsButton = CreateMobileButton(canvas.transform, "Mobile Settings Toggle", new Vector2(24, -24), new Vector2(150, 46), new Vector2(0, 1), new Vector2(0, 1));
        mobileSettingsButtonText = settingsButton.GetComponentInChildren<TextMeshProUGUI>();
        settingsButton.onClick.AddListener(ToggleMobileSettings);

        mobileSettingsPanel = new GameObject("Mobile Settings Panel", typeof(RectTransform), typeof(Image));
        mobileSettingsPanel.transform.SetParent(canvas.transform, false);

        RectTransform panelRect = mobileSettingsPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0, 1);
        panelRect.anchorMax = new Vector2(0, 1);
        panelRect.pivot = new Vector2(0, 1);
        panelRect.anchoredPosition = new Vector2(24, -82);
        panelRect.sizeDelta = new Vector2(MobilePanelWidth, MobilePanelHeight);

        Image panelImage = mobileSettingsPanel.GetComponent<Image>();
        panelImage.color = new Color(0, 0, 0, 0.62f);
        panelImage.raycastTarget = true;

        float y = -18;
        CreateMobileSlider(mobileSettingsPanel.transform, "Separation", settings.seperateWeight, 0, 5, y, OnSeparationChanged);
        y -= MobileSliderSpacing;
        CreateMobileSlider(mobileSettingsPanel.transform, "Alignment", settings.alignWeight, 0, 5, y, OnAlignmentChanged);
        y -= MobileSliderSpacing;
        CreateMobileSlider(mobileSettingsPanel.transform, "Cohesion", settings.cohesionWeight, 0, 5, y, OnCohesionChanged);
        y -= MobileSliderSpacing;
        CreateMobileSlider(mobileSettingsPanel.transform, "Sigma Arus", settings.sigma, 0, 2, y, OnSigmaChanged);
        y -= MobileSliderSpacing;
        CreateMobileSlider(mobileSettingsPanel.transform, "Interval Arus", settings.currentInterval, 0.5f, 5, y, OnIntervalChanged);
        y -= MobileSliderSpacing;
        CreateMobileSlider(mobileSettingsPanel.transform, "Jumlah Ikan", settings.fishCount, MinFishCount, GetRuntimeMaxFishCount(), y, OnFishCountChanged, true);

        Button restartButton = CreateMobileButton(mobileSettingsPanel.transform, "Mobile Restart Button", new Vector2(22, 16), new Vector2(346, 38), Vector2.zero, Vector2.zero);
        restartButton.GetComponentInChildren<TextMeshProUGUI>().text = "Restart Scene";
        restartButton.onClick.AddListener(RestartScene);

        mobileSettingsOpen = false;
        UpdateMobileSettingsVisibility();
        return true;
    }

    private void CreateDesktopSlider(Transform parent, string label, float value, float min, float max, float y, UnityEngine.Events.UnityAction<float> callback, bool wholeNumbers = false)
    {
        GameObject rowObject = new GameObject(label + " Desktop Row", typeof(RectTransform));
        rowObject.transform.SetParent(parent, false);

        RectTransform rowRect = rowObject.GetComponent<RectTransform>();
        rowRect.anchorMin = new Vector2(0, 1);
        rowRect.anchorMax = new Vector2(1, 1);
        rowRect.offsetMin = new Vector2(18, y - 48);
        rowRect.offsetMax = new Vector2(-18, y);

        TextMeshProUGUI labelText = CreateDesktopText(rowObject.transform, label + " Label", new Vector2(0, -2), new Vector2(136, 22), new Vector2(0, 1), new Vector2(0, 1), 16, TextAlignmentOptions.Left);
        TextMeshProUGUI valueText = CreateDesktopText(rowObject.transform, label + " Value", new Vector2(0, -2), new Vector2(76, 22), new Vector2(1, 1), new Vector2(1, 1), 16, TextAlignmentOptions.Right);
        labelText.text = label;
        RuntimeInfoPopup.CreateInfoButton(rowObject.transform, label, GetControlInfo(label), new Vector2(142, -3), new Vector2(18, 18), new Vector2(0, 1), new Vector2(0, 1), 10);

        GameObject sliderObject = new GameObject(label + " Desktop Slider", typeof(RectTransform), typeof(Slider));
        sliderObject.transform.SetParent(rowObject.transform, false);

        RectTransform sliderRect = sliderObject.GetComponent<RectTransform>();
        sliderRect.anchorMin = new Vector2(0, 0);
        sliderRect.anchorMax = new Vector2(1, 0);
        sliderRect.pivot = new Vector2(0.5f, 0);
        sliderRect.anchoredPosition = new Vector2(0, 4);
        sliderRect.sizeDelta = new Vector2(0, 22);

        Slider slider = sliderObject.GetComponent<Slider>();
        slider.minValue = min;
        slider.maxValue = max;
        slider.wholeNumbers = wholeNumbers;
        slider.value = Mathf.Clamp(value, min, max);

        RectTransform background = CreateSliderImage(sliderObject.transform, "Background", new Color(1, 1, 1, 0.22f), Vector2.zero, Vector2.one);
        RectTransform fillArea = CreateSliderContainer(sliderObject.transform, "Fill Area", new Vector2(0, 0.3f), new Vector2(1, 0.7f), new Vector2(11, 0), new Vector2(-11, 0));
        RectTransform fill = CreateSliderImage(fillArea, "Fill", new Color(0.45f, 0.82f, 1f, 0.92f), Vector2.zero, Vector2.one);
        RectTransform handle = CreateSliderImage(sliderObject.transform, "Handle", Color.white, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        handle.sizeDelta = new Vector2(24, 24);

        slider.targetGraphic = handle.GetComponent<Image>();
        slider.fillRect = fill;
        slider.handleRect = handle;

        void UpdateValueText(float sliderValue)
        {
            valueText.text = wholeNumbers ? Mathf.RoundToInt(sliderValue).ToString() : sliderValue.ToString("F2");
        }

        UpdateValueText(slider.value);
        slider.onValueChanged.AddListener(UpdateValueText);
        slider.onValueChanged.AddListener(callback);
    }

    private static TextMeshProUGUI CreateDesktopText(Transform parent, string objectName, Vector2 anchoredPosition, Vector2 size, Vector2 anchorMin, Vector2 anchorMax, float fontSize, TextAlignmentOptions alignment)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);

        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(anchorMin.x, anchorMax.y);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.raycastTarget = false;
        text.fontSize = fontSize;
        text.color = Color.white;
        text.alignment = alignment;
        return text;
    }

    private void ToggleMobileSettings()
    {
        mobileSettingsOpen = !mobileSettingsOpen;
        UpdateMobileSettingsVisibility();
    }

    private void UpdateMobileSettingsVisibility()
    {
        if (mobileSettingsPanel != null)
        {
            mobileSettingsPanel.SetActive(mobileSettingsOpen);
        }

        if (mobileSettingsButtonText != null)
        {
            mobileSettingsButtonText.text = mobileSettingsOpen ? "Close" : "Settings";
        }
    }

    private void SetLegacyControlsActive(bool active)
    {
        SetObjectActive(sepText, active);
        SetObjectActive(aliText, active);
        SetObjectActive(cohText, active);
        SetObjectActive(sigmaText, active);
        SetObjectActive(intervalText, active);
        SetObjectActive(fishCountText, active);
        SetObjectActive(sepSlider, active);
        SetObjectActive(aliSlider, active);
        SetObjectActive(cohSlider, active);
        SetObjectActive(sigmaSlider, active);
        SetObjectActive(intervalSlider, active);
        SetObjectActive(fishCountSlider, active);
        SetLegacyResetButtonsActive(active);
    }

    private static void SetObjectActive(Component component, bool active)
    {
        if (component != null)
        {
            component.gameObject.SetActive(active);
        }
    }

    private static void SetLegacyResetButtonsActive(bool active)
    {
        Button[] buttons = FindObjectsByType<Button>();
        foreach (Button button in buttons)
        {
            TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null && label.text.Trim() == "Reset")
            {
                button.gameObject.SetActive(active);
            }
        }
    }

    private void CreateMobileSlider(Transform parent, string label, float value, float min, float max, float y, UnityEngine.Events.UnityAction<float> callback, bool wholeNumbers = false)
    {
        GameObject rowObject = new GameObject(label + " Row", typeof(RectTransform));
        rowObject.transform.SetParent(parent, false);

        RectTransform rowRect = rowObject.GetComponent<RectTransform>();
        rowRect.anchorMin = new Vector2(0, 1);
        rowRect.anchorMax = new Vector2(1, 1);
        rowRect.offsetMin = new Vector2(22, y - 46);
        rowRect.offsetMax = new Vector2(-22, y);

        TextMeshProUGUI valueText = CreateMobileText(rowObject.transform, label + " Text", new Vector2(0, 22), new Vector2(-34, 0), 15);
        RuntimeInfoPopup.CreateInfoButton(rowObject.transform, label, GetControlInfo(label), new Vector2(-20, -1), new Vector2(20, 20), new Vector2(1, 1), new Vector2(1, 1), 11);

        GameObject sliderObject = new GameObject(label + " Slider", typeof(RectTransform), typeof(Slider));
        sliderObject.transform.SetParent(rowObject.transform, false);

        RectTransform sliderRect = sliderObject.GetComponent<RectTransform>();
        sliderRect.anchorMin = new Vector2(0, 0);
        sliderRect.anchorMax = new Vector2(1, 0);
        sliderRect.pivot = new Vector2(0.5f, 0);
        sliderRect.anchoredPosition = new Vector2(0, 4);
        sliderRect.sizeDelta = new Vector2(0, 20);

        Slider slider = sliderObject.GetComponent<Slider>();
        slider.minValue = min;
        slider.maxValue = max;
        slider.wholeNumbers = wholeNumbers;
        slider.value = Mathf.Clamp(value, min, max);

        RectTransform background = CreateSliderImage(sliderObject.transform, "Background", new Color(1, 1, 1, 0.28f), Vector2.zero, Vector2.one);
        RectTransform fillArea = CreateSliderContainer(sliderObject.transform, "Fill Area", new Vector2(0, 0.25f), new Vector2(1, 0.75f), new Vector2(10, 0), new Vector2(-10, 0));
        RectTransform fill = CreateSliderImage(fillArea, "Fill", new Color(0.6f, 0.8f, 1f, 0.85f), Vector2.zero, Vector2.one);
        RectTransform handle = CreateSliderImage(sliderObject.transform, "Handle", Color.white, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        handle.sizeDelta = new Vector2(24, 24);

        slider.targetGraphic = handle.GetComponent<Image>();
        slider.fillRect = fill;
        slider.handleRect = handle;

        void UpdateValueText(float sliderValue)
        {
            valueText.text = wholeNumbers ? $"{label}: {Mathf.RoundToInt(sliderValue)}" : $"{label}: {sliderValue:F2}";
        }

        UpdateValueText(slider.value);
        slider.onValueChanged.AddListener(UpdateValueText);
        slider.onValueChanged.AddListener(callback);
    }

    private static TextMeshProUGUI CreateMobileText(Transform parent, string objectName, Vector2 offsetMin, Vector2 offsetMax, float fontSize)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);

        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.raycastTarget = false;
        text.fontSize = fontSize;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Left;
        return text;
    }

    private static RectTransform CreateSliderContainer(Transform parent, string objectName, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        GameObject containerObject = new GameObject(objectName, typeof(RectTransform));
        containerObject.transform.SetParent(parent, false);

        RectTransform rect = containerObject.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
        return rect;
    }

    private static RectTransform CreateSliderImage(Transform parent, string objectName, Color color, Vector2 anchorMin, Vector2 anchorMax)
    {
        GameObject imageObject = new GameObject(objectName, typeof(RectTransform), typeof(Image));
        imageObject.transform.SetParent(parent, false);

        RectTransform rect = imageObject.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image image = imageObject.GetComponent<Image>();
        image.color = color;
        return rect;
    }

    private Button CreateDesktopButton(Transform parent, string objectName, Vector2 anchoredPosition, Vector2 size)
    {
        GameObject buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.zero;
        rect.pivot = Vector2.zero;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 0.9f);

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;

        TextMeshProUGUI label = CreateDesktopText(buttonObject.transform, "Label", Vector2.zero, Vector2.zero, Vector2.zero, Vector2.one, 15, TextAlignmentOptions.Center);
        RectTransform labelRect = label.GetComponent<RectTransform>();
        labelRect.offsetMin = new Vector2(8, 4);
        labelRect.offsetMax = new Vector2(-8, -4);
        label.color = Color.black;

        return button;
    }

    private Button CreateMobileButton(Transform parent, string objectName, Vector2 anchoredPosition, Vector2 size, Vector2 anchorMin, Vector2 anchorMax)
    {
        GameObject buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(anchorMin.x, anchorMax.y);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(1, 1, 1, 0.92f);

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;

        TextMeshProUGUI label = CreateMobileText(buttonObject.transform, "Label", new Vector2(8, 4), new Vector2(-8, -4), 16);
        label.color = Color.black;
        label.alignment = TextAlignmentOptions.Center;

        return button;
    }
}
