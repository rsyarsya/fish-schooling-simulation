using UnityEngine;

public class StochasticCurrent : MonoBehaviour
{
    public BoidSettings settings;

    [Tooltip("READ ONLY — nilai sigma aktual dibaca dari BoidSettings.sigma saat runtime")]
    public float sigma; // Hanya untuk tampilan di Inspector, TIDAK dipakai di kalkulasi

    public float currentInterval;

    private float timer;

    void Start()
    {
        if (settings != null)
        {
            sigma = settings.sigma;
            currentInterval = settings.currentInterval;
            GenerateCurrent();
        }
        else
        {
            Debug.LogError("[StochasticCurrent] BoidSettings is missing!");
        }
    }

    void Update()
    {
        if (settings == null) return;

        // Bug Fix #3: Baca interval dari settings (sumber kebenaran tunggal)
        // agar perubahan dari UIManager langsung terasa
        currentInterval = settings.currentInterval;

        timer += Time.deltaTime;
        if (timer >= currentInterval)
        {
            timer = 0f;
            GenerateCurrent();
        }

        // Sinkronisasi tampilan Inspector agar user bisa melihat nilai aktif
        sigma = settings.sigma;
    }

    void GenerateCurrent()
    {
        // Bug Fix #3: Selalu baca sigma dari settings (single source of truth)
        // Ini memastikan slider σ di UI benar-benar mempengaruhi kalkulasi
        float activeSigma = settings.sigma;

        // Box-Muller Transform untuk sampling distribusi Normal N(0, 1)
        float u1 = Mathf.Max(Random.value, 0.0001f);
        float u2 = Mathf.Max(Random.value, 0.0001f);

        // Sesuai dengan spesifikasi akademik
        float z1 = Mathf.Sqrt(-2f * Mathf.Log(u1)) * Mathf.Cos(2f * Mathf.PI * u2);
        float z2 = Mathf.Sqrt(-2f * Mathf.Log(u1)) * Mathf.Sin(2f * Mathf.PI * u2);
        float z3 = Mathf.Sqrt(-2f * Mathf.Log(u2)) * Mathf.Cos(2f * Mathf.PI * u1);

        // Mengalikannya dengan deviasi standar aktif (activeSigma)
        // Note: Komponen Y dikali 0.3 agar arus laut dominan horizontal
        settings.currentForce = new Vector3(
            activeSigma * z1,
            activeSigma * z2 * 0.3f,
            activeSigma * z3
        );

        // Mencetak log sesuai requirements
        Debug.Log($"[StochasticCurrent] σ={activeSigma:F2} | " +
                  $"U1={u1:F4} U2={u2:F4} | " +
                  $"Z=({z1:F3},{z2:F3},{z3:F3}) | " +
                  $"Force={settings.currentForce}");
    }

    // Bug Fix #3: Hanya tulis ke settings, BUKAN ke variabel lokal sigma
    public void SetSigma(float value)
    {
        if (settings != null) settings.sigma = value;
    }

    public void SetInterval(float newInterval)
    {
        if (settings != null) settings.currentInterval = newInterval;
    }
}
