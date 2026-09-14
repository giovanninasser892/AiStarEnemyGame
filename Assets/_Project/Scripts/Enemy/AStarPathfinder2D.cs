using System.Collections.Generic;
using UnityEngine;

public class AStarPathfinder2D : MonoBehaviour
{
    [SerializeField] private GridNavigation2D navigation;

    private class NodeRecord
    {
        public Vector3Int cell;
        public int g = int.MaxValue;
        public int h;
        public NodeRecord parent;

        public int F => g + h;
    }

    public List<Vector3Int> FindPath(Vector3Int start, Vector3Int goal)
    {
        List<Vector3Int> emptyPath = new List<Vector3Int>();

        if (navigation == null)
            return emptyPath;

        if (!navigation.IsWalkable(start) || !navigation.IsWalkable(goal))
            return emptyPath;

        if (start == goal)
        {
            emptyPath.Add(start);
            return emptyPath;
        }

        Dictionary<Vector3Int, NodeRecord> nodes = new Dictionary<Vector3Int, NodeRecord>();
        List<NodeRecord> open = new List<NodeRecord>();
        HashSet<Vector3Int> closed = new HashSet<Vector3Int>();

        NodeRecord startNode = GetOrCreateNode(nodes, start, goal);
        startNode.g = 0;
        open.Add(startNode);

        while (open.Count > 0)
        {
            int bestIndex = GetBestOpenIndex(open);
            NodeRecord current = open[bestIndex];
            open.RemoveAt(bestIndex);

            if (current.cell == goal)
                return ReconstructPath(current);

            closed.Add(current.cell);

            foreach (Vector3Int neighborCell in navigation.GetWalkableNeighbors(current.cell))
            {
                int tentativeG = current.g + 1;
                NodeRecord neighbor = GetOrCreateNode(nodes, neighborCell, goal);

                if (closed.Contains(neighborCell) && tentativeG >= neighbor.g)
                    continue;

                if (tentativeG < neighbor.g)
                {
                    neighbor.g = tentativeG;
                    neighbor.parent = current;

                    if (closed.Contains(neighborCell))
                        closed.Remove(neighborCell);

                    if (!open.Contains(neighbor))
                        open.Add(neighbor);
                }
            }
        }

        return emptyPath;
    }

    private NodeRecord GetOrCreateNode(
        Dictionary<Vector3Int, NodeRecord> nodes,
        Vector3Int cell,
        Vector3Int goal)
    {
        if (nodes.TryGetValue(cell, out NodeRecord existing))
            return existing;

        NodeRecord node = new NodeRecord
        {
            cell = cell,
            h = Manhattan(cell, goal)
        };

        nodes.Add(cell, node);
        return node;
    }

    private int GetBestOpenIndex(List<NodeRecord> open)
    {
        int bestIndex = 0;

        for (int i = 1; i < open.Count; i++)
        {
            NodeRecord candidate = open[i];
            NodeRecord best = open[bestIndex];

            if (candidate.F < best.F ||
                (candidate.F == best.F && candidate.h < best.h))
            {
                bestIndex = i;
            }
        }

        return bestIndex;
    }

    private List<Vector3Int> ReconstructPath(NodeRecord goal)
    {
        List<Vector3Int> path = new List<Vector3Int>();
        NodeRecord current = goal;

        while (current != null)
        {
            path.Add(current.cell);
            current = current.parent;
        }

        path.Reverse();
        return path;
    }

    private int Manhattan(Vector3Int a, Vector3Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }
}
