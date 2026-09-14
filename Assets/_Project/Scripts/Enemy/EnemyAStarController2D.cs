using System.Collections.Generic;
using UnityEngine;

public class EnemyAStarController2D : MonoBehaviour
{
    [Header("Referências")]
    [SerializeField] private EnemyGridMovement2D movement;
    [SerializeField] private PlayerGridMovement2D player;
    [SerializeField] private AStarPathfinder2D pathfinder;
    [SerializeField] private GridNavigation2D navigation;
    [SerializeField] private AIMemoryManager aiMemory;

    [Header("Aprendizado / Predição")]
    [Tooltip("Quantidade máxima de células futuras que a IA pode prever.")]
    [Min(0)]
    [SerializeField] private int maxPredictionDepth = 12;

    [Tooltip("Se a confiança for menor que isto, o inimigo persegue a posição atual do player.")]
    [Range(0f, 1f)]
    [SerializeField] private float minimumPredictionConfidence = 0.5f;

    [Tooltip("Quantas vitórias consolidadas são necessárias para ganhar +1 célula de previsão.")]
    [Min(1)]
    [SerializeField] private int successfulRunsPerPredictionStep = 1;

    private void Start()
    {
        if (aiMemory == null)
            aiMemory = AIMemoryManager.Instance;
    }

    private void Update()
    {
        if (!CanThink())
            return;

        if (movement.IsMoving)
            return;

        ThinkAndMove();
    }

    private void ThinkAndMove()
    {
        Vector3Int playerCell = player.CurrentOrTargetCell;
        Vector3Int targetCell = playerCell;

        int predictionDepth = GetPredictionDepth();

        if (predictionDepth > 0 &&
            aiMemory != null &&
            aiMemory.GetPredictionConfidence(playerCell) >= minimumPredictionConfidence)
        {
            Vector3Int predicted = aiMemory.PredictFutureCell(
                playerCell,
                predictionDepth,
                navigation
            );

            if (navigation.IsWalkable(predicted))
                targetCell = predicted;
        }

        List<Vector3Int> path = pathfinder.FindPath(movement.CurrentCell, targetCell);

        // Se a previsão não produziu caminho, volta ao comportamento A* normal.
        if (path.Count < 2 && targetCell != playerCell)
            path = pathfinder.FindPath(movement.CurrentCell, playerCell);

        if (path.Count >= 2)
            movement.BeginMoveToCell(path[1]);
    }

    private int GetPredictionDepth()
    {
        if (aiMemory == null)
            return 0;

        int depth = aiMemory.SuccessfulRuns / successfulRunsPerPredictionStep;
        return Mathf.Clamp(depth, 0, maxPredictionDepth);
    }

    private bool CanThink()
    {
        return GameFlowManager.Instance != null &&
               GameFlowManager.Instance.CanGameplayMove &&
               movement != null &&
               player != null &&
               pathfinder != null &&
               navigation != null;
    }
}
