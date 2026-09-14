using System;
using System.Collections.Generic;

[Serializable]
public class GridCellData
{
    public int x;
    public int y;

    public GridCellData()
    {
    }

    public GridCellData(int x, int y)
    {
        this.x = x;
        this.y = y;
    }
}

[Serializable]
public class DirectionStatsData
{
    public int x;
    public int y;

    public int north;
    public int south;
    public int east;
    public int west;
}

[Serializable]
public class SuccessfulRouteData
{
    public List<GridCellData> cells = new List<GridCellData>();
}

[Serializable]
public class AIMemoryData
{
    public int version = 1;
    public int currentLevel = 1;
    public int successfulRuns = 0;

    public List<DirectionStatsData> transitions = new List<DirectionStatsData>();
    public List<SuccessfulRouteData> successfulRoutes = new List<SuccessfulRouteData>();
}
