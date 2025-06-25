using Godot;
using System.Collections.Generic;

public static class Pathfinder
{
    public static List<Vector2I> FindPath(Vector2I start, Vector2I goal, System.Func<Vector2I, bool> isPassable)
    {
        Queue<Vector2I> frontier = new();
        Dictionary<Vector2I, Vector2I> cameFrom = new();
        HashSet<Vector2I> visited = new();

        frontier.Enqueue(start);
        visited.Add(start);

        while (frontier.Count > 0)
        {
            var current = frontier.Dequeue();
            if (current == goal)
                break;

            foreach (var neighbor in HexUtils.GetNeighbors(current))
            {
                if (visited.Contains(neighbor))
                    continue;
                if (!isPassable(neighbor))
                    continue;

                frontier.Enqueue(neighbor);
                visited.Add(neighbor);
                cameFrom[neighbor] = current;
            }
        }

        if (!visited.Contains(goal))
            return new List<Vector2I>();

        List<Vector2I> path = new();
        var step = goal;
        path.Add(step);
        while (step != start)
        {
            step = cameFrom[step];
            path.Add(step);
        }
        path.Reverse();
        return path;
    }
}
