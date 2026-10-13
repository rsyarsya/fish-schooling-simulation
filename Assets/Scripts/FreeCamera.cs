using UnityEngine;

public class FreeCamera : MonoBehaviour
{
    [Header("Movement Settings")]
    public float movementSpeed = 10f;
    public float fastMovementSpeed = 30f;

    [Header("Look Settings")]
    public float lookSensitivity = 3f;
    public bool enableTouchControlsOnMobile = true;
    public bool forceMobileModeInEditor;
    public float touchLookSensitivity = 0.12f;
    public float mobilePitchMin = -85f;
    public float mobilePitchMax = 85f;
    public float mobileLookDeadzone = 0.2f;
    public float joystickRadius = 90f;

    float rotationX;
    float rotationY;
    bool isLooking;
    bool useTouchControls;
    Vector2 mobileMoveInput;

    void Awake()
    {
        RuntimeViewMode.RegisterForceMobileModeInEditor(forceMobileModeInEditor);
    }

    void Start()
    {
        Vector3 rot = transform.localRotation.eulerAngles;
        rotationX = Mathf.Clamp(NormalizeAngle(rot.x), -90f, 90f);
        rotationY = NormalizeAngle(rot.y);

        useTouchControls = enableTouchControlsOnMobile && ShouldUseTouchControls();
        if (useTouchControls)
        {
            MobileTouchControls.Create(this, joystickRadius);
        }
    }

    void Update()
    {
        if (useTouchControls)
        {
            ApplyMobileMovement();
            return;
        }

        HandleDesktopControls();
    }

    public void SetMobileMoveInput(Vector2 input)
    {
        mobileMoveInput = Vector2.ClampMagnitude(input, 1f);
    }

    public void ClearMobileMoveInput()
    {
        mobileMoveInput = Vector2.zero;
    }

    public void AddMobileLookDelta(Vector2 delta)
    {
        if (delta.sqrMagnitude < mobileLookDeadzone * mobileLookDeadzone)
        {
            return;
        }

        rotationY += delta.x * touchLookSensitivity;
        rotationX -= delta.y * touchLookSensitivity;
        rotationX = Mathf.Clamp(rotationX, mobilePitchMin, mobilePitchMax);
        ApplyLookRotation();
    }

    void HandleDesktopControls()
    {
        bool isFast = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        float currentSpeed = isFast ? fastMovementSpeed : movementSpeed;

        if (Input.GetKey(KeyCode.W)) transform.position += transform.forward * currentSpeed * Time.deltaTime;
        if (Input.GetKey(KeyCode.S)) transform.position -= transform.forward * currentSpeed * Time.deltaTime;
        if (Input.GetKey(KeyCode.D)) transform.position += transform.right * currentSpeed * Time.deltaTime;
        if (Input.GetKey(KeyCode.A)) transform.position -= transform.right * currentSpeed * Time.deltaTime;
        if (Input.GetKey(KeyCode.E)) transform.position += Vector3.up * currentSpeed * Time.deltaTime;
        if (Input.GetKey(KeyCode.Q)) transform.position -= Vector3.up * currentSpeed * Time.deltaTime;

        if (Input.GetMouseButtonDown(1))
        {
            isLooking = true;
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
        else if (Input.GetMouseButtonUp(1))
        {
            isLooking = false;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        if (isLooking)
        {
            rotationY += Input.GetAxis("Mouse X") * lookSensitivity;
            rotationX -= Input.GetAxis("Mouse Y") * lookSensitivity;
            rotationX = Mathf.Clamp(rotationX, -90f, 90f);
            ApplyLookRotation();
        }
    }

    void ApplyMobileMovement()
    {
        Vector3 forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
        Vector3 right = Vector3.ProjectOnPlane(transform.right, Vector3.up).normalized;
        Vector3 movement = forward * mobileMoveInput.y + right * mobileMoveInput.x;

        if (movement.sqrMagnitude > 1f)
        {
            movement.Normalize();
        }

        transform.position += movement * movementSpeed * Time.deltaTime;
    }

    void ApplyLookRotation()
    {
        transform.localRotation = Quaternion.Euler(rotationX, rotationY, 0f);
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

    bool ShouldUseTouchControls()
    {
        return RuntimeViewMode.IsMobileLike;
    }
}
