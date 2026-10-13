using UnityEngine;

public static class RuntimeViewMode {

    static bool forceMobileModeInEditor;

    [RuntimeInitializeOnLoadMethod (RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void ResetRuntimeState () {
        forceMobileModeInEditor = false;
    }

    public static bool IsMobileLike {
        get {
            if (Application.isMobilePlatform || SystemInfo.deviceType == DeviceType.Handheld) {
                return true;
            }

#if UNITY_EDITOR
            return forceMobileModeInEditor || IsLikelyMobileEditorViewport ();
#else
            return Input.touchSupported && IsLikelySmallTouchViewport ();
#endif
        }
    }

    public static void RegisterForceMobileModeInEditor (bool enabled) {
#if UNITY_EDITOR
        if (enabled) {
            forceMobileModeInEditor = true;
        }
#endif
    }

    static bool IsLikelySmallTouchViewport () {
        int shortSide = Mathf.Min (Screen.width, Screen.height);
        int longSide = Mathf.Max (Screen.width, Screen.height);

        float aspect = shortSide > 0 ? longSide / (float) shortSide : 0;

        return shortSide <= 1200 && longSide <= 3000 && aspect >= 2f;
    }

    static bool IsLikelyMobileEditorViewport () {
        int shortSide = Mathf.Min (Screen.width, Screen.height);
        int longSide = Mathf.Max (Screen.width, Screen.height);
        float aspect = shortSide > 0 ? longSide / (float) shortSide : 0;

        return shortSide <= 1200 && longSide <= 3000 && aspect >= 2f;
    }
}
