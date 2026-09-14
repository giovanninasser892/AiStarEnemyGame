using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class AIMemoryManager : MonoBehaviour
{
    public static AIMemoryManager Instance { get; private set; }

    [Header("Arquivo")]
    [SerializeField] private string fileName = "maze_ai_memory.json";

    [Header("Histórico")]
    [Min(1)]
    [SerializeField] private int maxStoredSuccessfulRoutes = 100;

    private AIMemoryData data;
    private List<Vector3Int> pendingSuccessfulRoute;
    private readonly Dictionary<Vector2Int, DirectionStatsData> transitionLookup =
        new Dictionary<Vector2Int, DirectionStatsData>();

    public AIMemoryData Data => data;
    public int CurrentLevel => data != null ? Mathf.Max(1, data.currentLevel) : 1;
    public int SuccessfulRuns => data != null ? Mathf.Max(0, data.successfulRuns) : 0;
    public string SavePath => Path.Combine(Application.persistentDataPath, fileName);

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        Load();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void StageSuccessfulAttempt(List<Vector3Int> route)
    {
        if (route == null || route.Count < 2)
        {
            pendingSuccessfulRoute = null;
            return;
        }

        pendingSuccessfulRoute = new List<Vector3Int>(route);
    }

    public void DiscardPendingAttempt()
    {
        pendingSuccessfulRoute = null;
    }

    public bool CommitPendingWin()
    {
        if (pendingSuccessfulRoute == null || pendingSuccessfulRoute.Count < 2)
            return false;

        LearnFromRoute(pendingSuccessfulRoute);
        StoreRoute(pendingSuccessfulRoute);

        data.successfulRuns++;
        data.currentLevel = Mathf.Max(1, data.currentLevel) + 1;

        pendingSuccessfulRoute = null;

        Save();
        RebuildLookup();

        return true;
    }

    public Vector3Int PredictFutureCell(
        Vector3Int currentPlayerCell,
        int predictionSteps,
        GridNavigation2D navigation)
    {
        if (predictionSteps <= 0 || navigation == null || transitionLookup.Count == 0)
            return currentPlayerCell;

        Vector3Int predicted = currentPlayerCell;
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

        for (int i = 0; i < predictionSteps; i++)
        {
            Vector2Int key = new Vector2Int(predicted.x, predicted.y);

            if (!visited.Add(key))
                break;

            if (!transitionLookup.TryGetValue(key, out DirectionStatsData stats))
                break;

            Vector3Int direction = GetMostLikelyDirection(stats);

            if (direction == Vector3Int.zero)
                break;

            Vector3Int next = predicted + direction;

            if (!navigation.IsWalkable(next))
                break;

            predicted = next;
        }

        return predicted;
    }

    public float GetPredictionConfidence(Vector3Int cell)
    {
        Vector2Int key = new Vector2Int(cell.x, cell.y);

        if (!transitionLookup.TryGetValue(key, out DirectionStatsData stats))
            return 0f;

        int total = stats.north + stats.south + stats.east + stats.west;

        if (total <= 0)
            return 0f;

        int best = Mathf.Max(
            Mathf.Max(stats.north, stats.south),
            Mathf.Max(stats.east, stats.west)
        );

        return (float)best / total;
    }

    private void LearnFromRoute(List<Vector3Int> route)
    {
        for (int i = 0; i < route.Count - 1; i++)
        {
            Vector3Int from = route[i];
            Vector3Int to = route[i + 1];

            Vector3Int direction = to - from;

            if (Mathf.Abs(direction.x) + Mathf.Abs(direction.y) != 1)
                continue;

            DirectionStatsData stats = GetOrCreateStats(from);

            if (direction == Vector3Int.up)
                stats.north++;
            else if (direction == Vector3Int.down)
                stats.south++;
            else if (direction == Vector3Int.right)
                stats.east++;
            else if (direction == Vector3Int.left)
                stats.west++;
        }
    }

    private void StoreRoute(List<Vector3Int> route)
    {
        SuccessfulRouteData savedRoute = new SuccessfulRouteData();

        for (int i = 0; i < route.Count; i++)
            savedRoute.cells.Add(new GridCellData(route[i].x, route[i].y));

        data.successfulRoutes.Add(savedRoute);

        while (data.successfulRoutes.Count > maxStoredSuccessfulRoutes)
            data.successfulRoutes.RemoveAt(0);
    }

    private DirectionStatsData GetOrCreateStats(Vector3Int cell)
    {
        Vector2Int key = new Vector2Int(cell.x, cell.y);

        if (transitionLookup.TryGetValue(key, out DirectionStatsData existing))
            return existing;

        DirectionStatsData created = new DirectionStatsData
        {
            x = cell.x,
            y = cell.y
        };

        data.transitions.Add(created);
        transitionLookup.Add(key, created);
        return created;
    }

    private Vector3Int GetMostLikelyDirection(DirectionStatsData stats)
    {
        int bestCount = 0;
        Vector3Int bestDirection = Vector3Int.zero;

        // Ordem fixa usada apenas para desempate.
        if (stats.north > bestCount)
        {
            bestCount = stats.north;
            bestDirection = Vector3Int.up;
        }

        if (stats.east > bestCount)
        {
            bestCount = stats.east;
            bestDirection = Vector3Int.right;
        }

        if (stats.south > bestCount)
        {
            bestCount = stats.south;
            bestDirection = Vector3Int.down;
        }

        if (stats.west > bestCount)
        {
            bestCount = stats.west;
            bestDirection = Vector3Int.left;
        }

        return bestDirection;
    }

    private void Load()
    {
        try
        {
            if (!File.Exists(SavePath))
            {
                data = CreateDefaultData();
                RebuildLookup();
                return;
            }

            string json = File.ReadAllText(SavePath);
            data = JsonUtility.FromJson<AIMemoryData>(json);

            EnsureDataIntegrity();
            RebuildLookup();
        }
        catch (Exception exception)
        {
            Debug.LogError($"AIMemoryManager: falha ao carregar '{SavePath}'. Novo save será usado.\n{exception}");
            data = CreateDefaultData();
            RebuildLookup();
        }
    }

    private void Save()
    {
        try
        {
            EnsureDataIntegrity();

            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(SavePath, json);
        }
        catch (Exception exception)
        {
            Debug.LogError($"AIMemoryManager: falha ao salvar '{SavePath}'.\n{exception}");
        }
    }

    private void RebuildLookup()
    {
        transitionLookup.Clear();

        EnsureDataIntegrity();

        for (int i = 0; i < data.transitions.Count; i++)
        {
            DirectionStatsData stats = data.transitions[i];

            if (stats == null)
                continue;

            Vector2Int key = new Vector2Int(stats.x, stats.y);
            transitionLookup[key] = stats;
        }
    }

    private void EnsureDataIntegrity()
    {
        if (data == null)
            data = CreateDefaultData();

        data.currentLevel = Mathf.Max(1, data.currentLevel);
        data.successfulRuns = Mathf.Max(0, data.successfulRuns);

        if (data.transitions == null)
            data.transitions = new List<DirectionStatsData>();

        if (data.successfulRoutes == null)
            data.successfulRoutes = new List<SuccessfulRouteData>();
    }

    private AIMemoryData CreateDefaultData()
    {
        return new AIMemoryData
        {
            version = 1,
            currentLevel = 1,
            successfulRuns = 0,
            transitions = new List<DirectionStatsData>(),
            successfulRoutes = new List<SuccessfulRouteData>()
        };
    }

    [ContextMenu("DEBUG - Apagar memória da IA")]
    public void DebugDeleteSave()
    {
        pendingSuccessfulRoute = null;
        data = CreateDefaultData();
        RebuildLookup();

        if (File.Exists(SavePath))
            File.Delete(SavePath);

        Debug.Log($"Memória da IA apagada: {SavePath}");
    }

    [ContextMenu("DEBUG - Mostrar caminho do save")]
    public void DebugLogSavePath()
    {
        Debug.Log(SavePath);
    }

    public void ResetMemory()
    {
        pendingSuccessfulRoute = null;

        data = CreateDefaultData();

        RebuildLookup();

        if (File.Exists(SavePath))
            File.Delete(SavePath);

        Debug.Log(
            $"Memória da IA resetada.\n" +
            $"Nível atual: {data.currentLevel}\n" +
            $"Vitórias: {data.successfulRuns}\n" +
            $"Save: {SavePath}"
        );
    }
}
