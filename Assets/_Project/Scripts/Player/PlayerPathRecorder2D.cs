using System.Collections.Generic;
using UnityEngine;

public class PlayerPathRecorder2D : MonoBehaviour
{
    [SerializeField] private GridNavigation2D navigation;

    private readonly List<Vector3Int> currentPath = new List<Vector3Int>();
    private bool initialized;

    public IReadOnlyList<Vector3Int> CurrentPath => currentPath;

    private void Start()
    {
        if (!initialized && navigation != null)
            ResetCurrentAttempt(navigation.WorldToCell(transform.position));
    }

    public void ResetCurrentAttempt(Vector3Int initialCell)
    {
        currentPath.Clear();
        currentPath.Add(initialCell);
        initialized = true;
    }

    public void RecordCell(Vector3Int cell)
    {
        if (!initialized)
        {
            ResetCurrentAttempt(cell);
            return;
        }

        if (currentPath.Count == 0 || currentPath[currentPath.Count - 1] != cell)
            currentPath.Add(cell);
    }

    public List<Vector3Int> GetCurrentPathCopy()
    {
        return new List<Vector3Int>(currentPath);
    }

    public void DiscardCurrentAttempt()
    {
        currentPath.Clear();
        initialized = false;
    }
}
