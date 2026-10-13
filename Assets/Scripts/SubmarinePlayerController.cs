using UnityEngine;

public class SubmarinePlayerController : MonoBehaviour
{
    public float maxSpeed = 10f;
    public float reverseSpeed = 4f;
    public float acceleration = 8f;
    public float boostMultiplier = 1.8f;
    public float yawAssistSpeed = 70f;
    public float mouseSensitivity = 2.2f;
    public float pitchMin = -55f;
    public float pitchMax = 55f;
    public float collisionRadius = 1.1f;
    public float collisionSkin = 0.08f;
    public LayerMask obstacleMask;
    public bool lockCursorOnStart = true;

    Vector3 velocity;
    float yaw;
    float pitch;
    bool controlsLocked;

    public Vector3 Velocity => velocity;

    public void TeleportTo(Vector3 position, float yawAngle)
    {
        velocity = Vector3.zero;
        yaw = yawAngle;
        pitch = 0f;
        transform.SetPositionAndRotation(position, Quaternion.Euler(pitch, yaw, 0f));
    }

    void Start()
    {
        Vector3 euler = transform.eulerAngles;
        yaw = euler.y;
        pitch = NormalizeAngle(euler.x);

        if (lockCursorOnStart)
        {
            SetCursorLocked(true);
        }
    }

    void Update()
    {
        HandleCursorToggle();
        HandleRotation();
        HandleMovement();
    }

    void HandleCursorToggle()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            SetCursorLocked(false);
        }

        if (Input.GetMouseButtonDown(0))
        {
            SetCursorLocked(true);
        }
    }

    void HandleRotation()
    {
        float mouseX = controlsLocked ? Input.GetAxis("Mouse X") : 0f;
        float mouseY = controlsLocked ? Input.GetAxis("Mouse Y") : 0f;
        float keyboardYaw = 0f;

        if (Input.GetKey(KeyCode.A))
        {
            keyboardYaw -= 1f;
        }

        if (Input.GetKey(KeyCode.D))
        {
            keyboardYaw += 1f;
        }

        yaw += mouseX * mouseSensitivity + keyboardYaw * yawAssistSpeed * Time.deltaTime;
        pitch -= mouseY * mouseSensitivity;
        pitch = Mathf.Clamp(pitch, pitchMin, pitchMax);
        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    void HandleMovement()
    {
        float throttle = 0f;
        if (Input.GetKey(KeyCode.W))
        {
            throttle += 1f;
        }

        if (Input.GetKey(KeyCode.S))
        {
            throttle -= 1f;
        }

        float speedLimit = throttle >= 0f ? maxSpeed : reverseSpeed;
        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
        {
            speedLimit *= boostMultiplier;
        }

        Vector3 targetVelocity = transform.forward * throttle * speedLimit;
        velocity = Vector3.MoveTowards(velocity, targetVelocity, acceleration * Time.deltaTime);
        MoveWithCollision(velocity * Time.deltaTime);
    }

    void MoveWithCollision(Vector3 displacement)
    {
        float distance = displacement.magnitude;
        if (distance <= Mathf.Epsilon)
        {
            return;
        }

        Vector3 direction = displacement / distance;
        if (Physics.SphereCast(transform.position, collisionRadius, direction, out RaycastHit hit, distance + collisionSkin, obstacleMask, QueryTriggerInteraction.Ignore))
        {
            float safeDistance = Mathf.Max(0f, hit.distance - collisionSkin);
            transform.position += direction * safeDistance;
            velocity = Vector3.ProjectOnPlane(velocity, hit.normal) * 0.65f;
            return;
        }

        transform.position += displacement;
    }

    void SetCursorLocked(bool locked)
    {
        controlsLocked = locked;
        Cursor.visible = !locked;
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
    }

    static float NormalizeAngle(float angle)
    {
        while (angle > 180f)
        {
            angle -= 360f;
        }

        while (angle < -180f)
        {
            angle += 360f;
        }

        return angle;
    }
}
