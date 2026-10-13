using UnityEngine;

/// <summary>
/// Kontrol pergerakan kapal selam (Submarine) untuk skenario bawah laut.
///
/// Kontrol:
///   W / S         = Maju / Mundur
///   A / D         = Belok Kiri / Kanan (Yaw)
///   Space         = Naik (Ascend)
///   Left Shift    = Turun (Descend)
///   Left Ctrl     = Turbo / Sprint
///   Mouse X       = Belok Kiri / Kanan (tambahan)
///   Mouse Y       = Mendongak / Menunduk (Pitch)
///   Q / E         = Miring Kiri / Kanan (Roll) — opsional, bisa dimatikan
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("=== Kecepatan Gerak ===")]
    [Tooltip("Kecepatan maju/mundur normal")]
    public float moveSpeed = 8f;

    [Tooltip("Kecepatan saat turbo (Left Ctrl)")]
    public float turboSpeed = 16f;

    [Tooltip("Kecepatan naik/turun (Space / Shift)")]
    public float verticalSpeed = 5f;

    [Header("=== Kontrol Rotasi ===")]
    [Tooltip("Sensitivitas mouse untuk belok dan dongak")]
    public float mouseSensitivity = 2f;

    [Tooltip("Kecepatan belok menggunakan A/D")]
    public float keyboardTurnSpeed = 60f;

    [Tooltip("Kecepatan miring (Roll) menggunakan Q/E")]
    public float rollSpeed = 30f;

    [Tooltip("Batas sudut dongak (pitch) agar tidak terbalik")]
    public float maxPitchAngle = 60f;

    [Header("=== Efek Bawah Air ===")]
    [Tooltip("Hambatan air — semakin tinggi, semakin cepat kapal selam berhenti saat tidak bergerak")]
    public float waterDrag = 3f;

    [Tooltip("Hambatan rotasi air — semakin tinggi, semakin cepat kapal selam berhenti berputar")]
    public float waterAngularDrag = 5f;

    [Tooltip("Seberapa halus kapal selam berakselerasi (0.01 = sangat halus, 1 = instan)")]
    [Range(0.01f, 1f)]
    public float moveSmoothness = 0.1f;

    // Variabel internal
    private Rigidbody rb;
    private float currentPitch = 0f;  // Sudut dongak saat ini
    private float currentYaw = 0f;    // Sudut belok saat ini
    private float currentRoll = 0f;   // Sudut miring saat ini
    private Vector3 currentVelocity = Vector3.zero; // Untuk smoothing

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        // Kapal selam melayang di air — matikan gravitasi!
        rb.useGravity = false;

        // Drag (hambatan) untuk simulasi air
        rb.linearDamping = waterDrag;
        rb.angularDamping = waterAngularDrag;

        // Jangan biarkan Unity mengontrol rotasi fisika (kita kontrol manual)
        rb.freezeRotation = true;

        // Ambil rotasi awal kapal selam
        Vector3 startRotation = transform.eulerAngles;
        currentYaw = startRotation.y;
        currentPitch = startRotation.x;
        currentRoll = startRotation.z;

        // Sembunyikan kursor mouse agar lebih imersif (tekan Escape untuk keluar)
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        HandleRotation();
        HandleCursorToggle();
    }

    void FixedUpdate()
    {
        HandleMovement();
    }

    /// <summary>
    /// Mengatur rotasi kapal selam berdasarkan mouse dan keyboard
    /// </summary>
    void HandleRotation()
    {
        // === YAW (Belok Kiri/Kanan) ===
        // Gabungan Mouse X + tombol A/D
        float yawInput = Input.GetAxis("Mouse X") * mouseSensitivity;
        yawInput += Input.GetAxis("Horizontal") * keyboardTurnSpeed * Time.deltaTime;
        currentYaw += yawInput;

        // === PITCH (Dongak/Menunduk) ===
        // Mouse Y (dibalik agar gerakan natural: mouse ke atas = dongak ke atas)
        float pitchInput = -Input.GetAxis("Mouse Y") * mouseSensitivity;
        currentPitch += pitchInput;
        currentPitch = Mathf.Clamp(currentPitch, -maxPitchAngle, maxPitchAngle);

        // === ROLL (Miring Kiri/Kanan) === Opsional, pakai Q dan E
        if (Input.GetKey(KeyCode.Q))
        {
            currentRoll += rollSpeed * Time.deltaTime;
        }
        else if (Input.GetKey(KeyCode.E))
        {
            currentRoll -= rollSpeed * Time.deltaTime;
        }
        else
        {
            // Auto-stabilkan roll secara perlahan agar kapal selam kembali tegak
            currentRoll = Mathf.Lerp(currentRoll, 0f, 2f * Time.deltaTime);
        }

        // Terapkan rotasi
        transform.rotation = Quaternion.Euler(currentPitch, currentYaw, currentRoll);
    }

    /// <summary>
    /// Mengatur pergerakan kapal selam berdasarkan input keyboard
    /// </summary>
    void HandleMovement()
    {
        // Tentukan kecepatan saat ini (normal atau turbo)
        float currentSpeed = Input.GetKey(KeyCode.LeftControl) ? turboSpeed : moveSpeed;

        // === MAJU / MUNDUR ===
        // Menggunakan Vertical axis (W/S) tapi arahnya mengikuti "hidung" kapal selam
        float forwardInput = Input.GetAxisRaw("Vertical");
        Vector3 forwardForce = transform.forward * forwardInput * currentSpeed;

        // === NAIK / TURUN ===
        // Space = naik, Shift = turun (sumbu Y dunia, bukan lokal)
        float verticalInput = 0f;
        if (Input.GetKey(KeyCode.Space))
        {
            verticalInput = 1f;
        }
        else if (Input.GetKey(KeyCode.LeftShift))
        {
            verticalInput = -1f;
        }
        Vector3 verticalForce = Vector3.up * verticalInput * verticalSpeed;

        // Gabungkan semua gaya
        Vector3 targetVelocity = forwardForce + verticalForce;

        // Smoothing — agar gerakan terasa seperti di air (berat dan perlahan berakselerasi)
        currentVelocity = Vector3.Lerp(currentVelocity, targetVelocity, moveSmoothness);

        // Terapkan kecepatan (pertahankan sedikit velocity lama untuk efek inersia air)
        rb.linearVelocity = currentVelocity;
    }

    /// <summary>
    /// Tekan Escape untuk menampilkan/menyembunyikan kursor
    /// </summary>
    void HandleCursorToggle()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
    }
}
