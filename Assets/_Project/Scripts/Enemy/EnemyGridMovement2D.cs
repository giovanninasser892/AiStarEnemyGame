using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyGridMovement2D : MonoBehaviour
{
    [Header("Referências")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private GridNavigation2D navigation;
    [SerializeField] private EnemyFacing2D facing;

    [Header("Velocidade")]
    [Min(0.01f)]
    [SerializeField] private float baseMoveSpeed = 3f;

    [Tooltip("Aumento de velocidade por vitória já consolidada do jogador. Use 0 para desativar.")]
    [Min(0f)]
    [SerializeField] private float speedGainPerSuccessfulRun = 0.05f;

    [Min(0.01f)]
    [SerializeField] private float maxMoveSpeed = 6f;

    [Min(0.0001f)]
    [SerializeField] private float arrivalDistance = 0.01f;

    [Header("Áudio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip stepClip;

    private Vector3Int currentCell;
    private Vector3Int targetCell;

    private Vector2 targetWorldPosition;

    private bool isMoving;
    private float runtimeMoveSpeed;

    public bool IsMoving => isMoving;

    public Vector3Int CurrentCell => currentCell;

    public float RuntimeMoveSpeed => runtimeMoveSpeed;

    private void Reset()
    {
        rb = GetComponent<Rigidbody2D>();
        facing = GetComponent<EnemyFacing2D>();
    }

    private void Start()
    {
        if (rb == null || navigation == null)
        {
            Debug.LogError(
                "EnemyGridMovement2D: Rigidbody2D ou GridNavigation2D não configurado.",
                this
            );

            enabled = false;
            return;
        }

        currentCell = navigation.WorldToCell(rb.position);

        targetCell = currentCell;

        rb.position =
            navigation.CellToWorldCenter(currentCell);

        int completedRuns =
            AIMemoryManager.Instance != null
            ? AIMemoryManager.Instance.SuccessfulRuns
            : 0;

        runtimeMoveSpeed = Mathf.Min(
            baseMoveSpeed +
            completedRuns * speedGainPerSuccessfulRun,

            maxMoveSpeed
        );
    }

    private void FixedUpdate()
    {
        if (!isMoving)
            return;

        if (!CanMove())
            return;

        Vector2 nextPosition =
            Vector2.MoveTowards(
                rb.position,
                targetWorldPosition,
                runtimeMoveSpeed *
                Time.fixedDeltaTime
            );

        rb.MovePosition(nextPosition);

        if (
            Vector2.Distance(
                nextPosition,
                targetWorldPosition
            ) <= arrivalDistance
        )
        {
            rb.MovePosition(targetWorldPosition);

            currentCell = targetCell;

            isMoving = false;

            PlayStepSound();
        }
    }

    public bool BeginMoveToCell(Vector3Int cell)
    {
        if (isMoving || navigation == null)
            return false;

        if (!navigation.IsWalkable(cell))
            return false;

        if (!navigation.AreCardinalNeighbors(
            currentCell,
            cell
        ))
        {
            return false;
        }

        Vector3Int direction =
            cell - currentCell;

        if (facing != null)
            facing.SetDirection(direction);

        targetCell = cell;

        targetWorldPosition =
            navigation.CellToWorldCenter(cell);

        isMoving = true;

        return true;
    }

    public void StopMovement()
    {
        isMoving = false;

        targetCell = currentCell;

        targetWorldPosition =
            rb != null
            ? rb.position
            : transform.position;
    }

    private bool CanMove()
    {
        return
            GameFlowManager.Instance != null &&
            GameFlowManager.Instance.CanGameplayMove;
    }

    private void PlayStepSound()
    {
        if (
            audioSource != null &&
            stepClip != null
        )
        {
            audioSource.PlayOneShot(stepClip);
        }
    }
}