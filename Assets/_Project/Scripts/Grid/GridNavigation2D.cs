using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class GridNavigation2D : MonoBehaviour
{
    private static readonly Vector3Int[] CardinalDirections =
    {
        Vector3Int.up,
        Vector3Int.right,
        Vector3Int.down,
        Vector3Int.left
    };

    [Header("Grid")]
    [SerializeField] private Grid grid;

    [Header("Tilemaps")]
    [SerializeField] private Tilemap floorTilemap;
    [SerializeField] private Tilemap wallTilemap;

    public Grid Grid => grid;
    public Tilemap FloorTilemap => floorTilemap;
    public Tilemap WallTilemap => wallTilemap;

    public Vector3Int WorldToCell(Vector2 worldPosition)
    {
        return grid.WorldToCell(worldPosition);
    }

    public Vector2 CellToWorldCenter(Vector3Int cell)
    {
        return grid.GetCellCenterWorld(cell);
    }

    public bool IsWalkable(Vector3Int cell)
    {
        if (floorTilemap == null || wallTilemap == null)
            return false;

        return floorTilemap.HasTile(cell) && !wallTilemap.HasTile(cell);
    }

    public IEnumerable<Vector3Int> GetWalkableNeighbors(Vector3Int cell)
    {
        for (int i = 0; i < CardinalDirections.Length; i++)
        {
            Vector3Int neighbor = cell + CardinalDirections[i];

            if (IsWalkable(neighbor))
                yield return neighbor;
        }
    }

    public bool AreCardinalNeighbors(Vector3Int a, Vector3Int b)
    {
        int distance = Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
        return distance == 1;
    }

    private void OnValidate()
    {
        if (grid == null)
            grid = GetComponent<Grid>();
    }
}
