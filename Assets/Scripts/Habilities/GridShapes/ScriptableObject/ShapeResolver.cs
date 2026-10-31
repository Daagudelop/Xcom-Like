using System.Collections.Generic;
using UnityEngine;

public static class ShapeResolver
{
    public static List<Vector3> Resolve(
        AttackShape shape,
        Vector3 origin,          // celda desde donde sale la forma
        Vector3 aimPoint,        // hacia donde apunta (puntero)
        float cellSize,
        IDictionary<Vector3, float> grid,
        bool snapTo8Directions = true)
    {
        var result = new List<Vector3>();

        Vector3 dir = aimPoint - origin;
        dir.y = 0;
        if (dir.sqrMagnitude < 0.001f) dir = Vector3.forward;

        float angle = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;   // 0° = +Z
        if (shape.rotatesWithDirection)
        {
            float step = snapTo8Directions ? 45f : 90f;
            angle = Mathf.Round(angle / step) * step;
        }
        else angle = 0;

        Quaternion rot = Quaternion.Euler(0, angle, 0);

        foreach (var o in shape.GetLocalOffsets())
        {
            Vector3 local = new Vector3(o.x, 0, o.y) * cellSize;
            Vector3 world = origin + rot * local;
            world = new Vector3(
                Mathf.Round(world.x / cellSize) * cellSize,
                origin.y,
                Mathf.Round(world.z / cellSize) * cellSize);

            if (TryGetGridPoint(grid, world, cellSize, out Vector3 real))
                result.Add(real);
        }
        return result;
    }

    // Busca la celda XZ y toma la Y más cercana (tolerancia de altura)
    static bool TryGetGridPoint(IDictionary<Vector3, float> grid, Vector3 p, float cell, out Vector3 found)
    {
        var exact = GridPointer.NormalizeKey(p);
        if (grid.ContainsKey(exact)) { found = exact; return true; }

        found = default;
        float best = cell + 0.01f;
        bool ok = false;
        foreach (var k in grid.Keys)
        {
            if (Mathf.Abs(k.x - p.x) > 0.01f || Mathf.Abs(k.z - p.z) > 0.01f) continue;
            float dy = Mathf.Abs(k.y - p.y);
            if (dy < best) { best = dy; found = k; ok = true; }
        }
        return ok;
    }
}