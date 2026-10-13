using UnityEngine;

public class MobileRuntimeOptimizer : MonoBehaviour {

    const int MobileQualityLevel = 1;
    const int DesktopTargetFrameRate = 60;
    const int MobileTargetFrameRate = 30;

    [RuntimeInitializeOnLoadMethod (RuntimeInitializeLoadType.AfterSceneLoad)]
    static void ApplyRuntimeOptimizations () {
        if (RuntimeViewMode.IsMobileLike) {
            Application.targetFrameRate = MobileTargetFrameRate;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            if (QualitySettings.names.Length > MobileQualityLevel) {
                QualitySettings.SetQualityLevel (MobileQualityLevel, true);
            }

            return;
        }

        Application.targetFrameRate = DesktopTargetFrameRate;
    }
}
