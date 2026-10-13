using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 10f;
    public float rotationSpeed = 10f;

    void Update()
    {
        // Mengambil input dari keyboard (W/S/A/D atau panah)
        float horizontal = Input.GetAxis("Horizontal"); // A/D atau Kiri/Kanan
        float vertical = Input.GetAxis("Vertical");     // W/S atau Atas/Bawah

        // Menghitung arah gerakan berdasarkan input
        Vector3 moveDirection = new Vector3(horizontal, 0f, vertical).normalized;

        if (moveDirection.magnitude >= 0.1f)
        {
            // Menghitung sudut rotasi agar model menghadap ke arah gerakan
            float targetAngle = Mathf.Atan2(moveDirection.x, moveDirection.z) * Mathf.Rad2Deg;

            // Memutar model secara halus (smooth)
            Quaternion rotation = Quaternion.Euler(0f, targetAngle, 0f);
            transform.rotation = Quaternion.Slerp(transform.rotation, rotation, rotationSpeed * Time.deltaTime);

            // Menggerakkan model
            transform.Translate(moveDirection * moveSpeed * Time.deltaTime, Space.World);
        }

        // Opsional: Naik/Turun dengan tombol Space dan LeftShift
        if (Input.GetKey(KeyCode.Space))
        {
            transform.Translate(Vector3.up * moveSpeed * Time.deltaTime, Space.World);
        }
        else if (Input.GetKey(KeyCode.LeftShift))
        {
            transform.Translate(Vector3.down * moveSpeed * Time.deltaTime, Space.World);
        }
    }
}
