using UnityEngine;

public class EnemyFacing2D : MonoBehaviour
{
    [Header("Visual")]
    [SerializeField] private Transform visualRoot;

    [Header("Rotação")]
    [Tooltip("0 = Leste, 90 = Norte, 180 = Oeste, -90 = Sul.")]
    [SerializeField] private float initialAngle = 0f;

    [Min(0.001f)]
    [SerializeField] private float smoothTime = 0.08f;

    [SerializeField] private float maxDegreesPerSecond = 1080f;

    private float targetAngle;
    private float rotationVelocity;

    private void Awake()
    {
        targetAngle = initialAngle;

        if (visualRoot != null)
            visualRoot.localRotation = Quaternion.Euler(0f, 0f, initialAngle);
    }

    private void Update()
    {
        if (visualRoot == null)
            return;

        float currentAngle = visualRoot.localEulerAngles.z;

        float smoothedAngle = Mathf.SmoothDampAngle(
            currentAngle,
            targetAngle,
            ref rotationVelocity,
            smoothTime,
            maxDegreesPerSecond,
            Time.deltaTime
        );

        visualRoot.localRotation =
            Quaternion.Euler(0f, 0f, smoothedAngle);
    }

    public void SetDirection(Vector3Int direction)
    {
        if (direction == Vector3Int.right)
            targetAngle = 0f;
        else if (direction == Vector3Int.up)
            targetAngle = 90f;
        else if (direction == Vector3Int.left)
            targetAngle = 180f;
        else if (direction == Vector3Int.down)
            targetAngle = -90f;
    }
}