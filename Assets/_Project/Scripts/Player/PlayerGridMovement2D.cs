using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerGridMovement2D : MonoBehaviour
{
    [Header("Referências")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private GridNavigation2D navigation;
    [SerializeField] private PlayerFacing2D facing;
    [SerializeField] private PlayerPathRecorder2D pathRecorder;

    [Header("Movimento")]
    [Min(0.01f)]
    [SerializeField] private float moveSpeed = 4f;

    [Min(0.0001f)]
    [SerializeField] private float arrivalDistance = 0.01f;

    [Tooltip("Se ligado, segurar WASD continua andando célula por célula.")]
    [SerializeField] private bool allowHeldInput = true;

    [Header("Áudio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip stepClip;

    private Vector3Int currentCell;
    private Vector3Int targetCell;
    private Vector2 targetWorldPosition;
    private bool isMoving;

    public bool IsMoving => isMoving;
    public Vector3Int CurrentCell => currentCell;
    public Vector3Int TargetCell => targetCell;
    public Vector3Int CurrentOrTargetCell => isMoving ? targetCell : currentCell;

    private void Reset()
    {
        rb = GetComponent<Rigidbody2D>();
        pathRecorder = GetComponent<PlayerPathRecorder2D>();
        facing = GetComponent<PlayerFacing2D>();
    }

    private void Start()
    {
        if (rb == null || navigation == null)
        {
            Debug.LogError("PlayerGridMovement2D: Rigidbody2D ou GridNavigation2D não configurado.", this);
            enabled = false;
            return;
        }

        currentCell = navigation.WorldToCell(rb.position);
        targetCell = currentCell;

        rb.position = navigation.CellToWorldCenter(currentCell);

        if (pathRecorder != null)
            pathRecorder.ResetCurrentAttempt(currentCell);
    }

    private void Update()
    {
        if (!CanMove())
            return;

        Vector3Int direction = ReadDirection();

        if (direction == Vector3Int.zero)
            return;

        if (facing != null)
            facing.SetDirection(direction);

        if (!isMoving)
            TryStartMove(direction);
    }

    private void FixedUpdate()
    {
        if (!isMoving)
            return;

        if (!CanMove())
            return;

        Vector2 nextPosition = Vector2.MoveTowards(
            rb.position,
            targetWorldPosition,
            moveSpeed * Time.fixedDeltaTime
        );

        rb.MovePosition(nextPosition);

        if (Vector2.Distance(nextPosition, targetWorldPosition) <= arrivalDistance)
        {
            rb.MovePosition(targetWorldPosition);
            currentCell = targetCell;
            isMoving = false;

            if (pathRecorder != null)
                pathRecorder.RecordCell(currentCell);

            PlayStepSound();
        }
    }

    private void TryStartMove(Vector3Int direction)
    {
        Vector3Int candidate = currentCell + direction;

        if (!navigation.IsWalkable(candidate))
            return;

        targetCell = candidate;
        targetWorldPosition = navigation.CellToWorldCenter(targetCell);
        isMoving = true;
    }

    private Vector3Int ReadDirection()
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
            return Vector3Int.zero;

        if (allowHeldInput)
        {
            if (keyboard.wKey.isPressed) return Vector3Int.up;
            if (keyboard.sKey.isPressed) return Vector3Int.down;
            if (keyboard.aKey.isPressed) return Vector3Int.left;
            if (keyboard.dKey.isPressed) return Vector3Int.right;
        }
        else
        {
            if (keyboard.wKey.wasPressedThisFrame) return Vector3Int.up;
            if (keyboard.sKey.wasPressedThisFrame) return Vector3Int.down;
            if (keyboard.aKey.wasPressedThisFrame) return Vector3Int.left;
            if (keyboard.dKey.wasPressedThisFrame) return Vector3Int.right;
        }

        return Vector3Int.zero;
    }

    private bool CanMove()
    {
        return GameFlowManager.Instance != null &&
               GameFlowManager.Instance.CanGameplayMove;
    }

    private void PlayStepSound()
    {
        if (audioSource != null && stepClip != null)
            audioSource.PlayOneShot(stepClip);
    }

    public void StopMovement()
    {
        isMoving = false;
        targetCell = currentCell;
        targetWorldPosition = rb != null ? rb.position : transform.position;
    }
}
