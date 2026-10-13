using UnityEngine;

public class AdventureCameraFollow : MonoBehaviour
{
    public Transform target;
    public Vector3 followOffset = new Vector3(0f, 3.2f, -9f);
    public float lookAheadDistance = 8f;
    public float smoothTime = 0.12f;
    public float rotationSmoothSpeed = 6f;
    public LayerMask obstacleMask;
    public float cameraRadius = 0.35f;
    public float collisionPadding = 0.2f;

    Vector3 smoothVelocity;

    void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        Vector3 desiredPosition = target.position
            + target.right * followOffset.x
            + target.up * followOffset.y
            + target.forward * followOffset.z;

        Vector3 targetToCamera = desiredPosition - target.position;
        float targetDistance = targetToCamera.magnitude;
        if (targetDistance > Mathf.Epsilon)
        {
            Vector3 direction = targetToCamera / targetDistance;
            if (Physics.SphereCast(target.position, cameraRadius, direction, out RaycastHit hit, targetDistance, obstacleMask, QueryTriggerInteraction.Ignore))
            {
                desiredPosition = target.position + direction * Mathf.Max(0f, hit.distance - collisionPadding);
            }
        }

        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref smoothVelocity, smoothTime);

        Vector3 lookTarget = target.position + target.forward * lookAheadDistance;
        Quaternion desiredRotation = Quaternion.LookRotation(lookTarget - transform.position, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, rotationSmoothSpeed * Time.deltaTime);
    }
}
