using System.Collections.Generic;
using UnityEngine;

public enum ShapeType { Circle, Diamond, Square, Cone, Line, Cross, Ring, Custom }

[CreateAssetMenu(menuName = "Combat/Attack Shape")]
public class AttackShape : ScriptableObject
{
    public ShapeType type;
    [Min(1)] public int size = 2;        // radio, largo o alcance según el tipo
    [Min(0)] public int width = 0;       // media anchura (Line)
    public bool includeOrigin = false;
    public bool rotatesWithDirection = true;
    public List<Vector2Int> customOffsets;   // para formas dibujadas a mano

    // Offsets locales: x = lateral, y = adelante (Z)
    public List<Vector2Int> GetLocalOffsets()
    {
        var list = new List<Vector2Int>();

        switch (type)
        {
            case ShapeType.Circle:   // distancia euclidiana
                for (int x = -size; x <= size; x++)
                    for (int z = -size; z <= size; z++)
                        if (x * x + z * z <= size * size) list.Add(new Vector2Int(x, z));
                break;

            case ShapeType.Diamond:  // distancia Manhattan
                for (int x = -size; x <= size; x++)
                    for (int z = -size; z <= size; z++)
                        if (Mathf.Abs(x) + Mathf.Abs(z) <= size) list.Add(new Vector2Int(x, z));
                break;

            case ShapeType.Square:
                for (int x = -size; x <= size; x++)
                    for (int z = -size; z <= size; z++)
                        list.Add(new Vector2Int(x, z));
                break;

            case ShapeType.Cone:     // pirámide: se ensancha con la distancia
                for (int d = 1; d <= size; d++)
                    for (int x = -(d - 1); x <= d - 1; x++)
                        list.Add(new Vector2Int(x, d));
                break;

            case ShapeType.Line:
                for (int d = 1; d <= size; d++)
                    for (int x = -width; x <= width; x++)
                        list.Add(new Vector2Int(x, d));
                break;

            case ShapeType.Cross:
                for (int i = -size; i <= size; i++)
                {
                    list.Add(new Vector2Int(i, 0));
                    list.Add(new Vector2Int(0, i));
                }
                break;

            case ShapeType.Ring:     // círculo hueco
                for (int x = -size; x <= size; x++)
                    for (int z = -size; z <= size; z++)
                    {
                        int d2 = x * x + z * z;
                        if (d2 <= size * size && d2 > (size - 1) * (size - 1))
                            list.Add(new Vector2Int(x, z));
                    }
                break;

            case ShapeType.Custom:
                list.AddRange(customOffsets);
                break;
        }

        if (!includeOrigin) list.RemoveAll(o => o == Vector2Int.zero);
        else if (!list.Contains(Vector2Int.zero)) list.Add(Vector2Int.zero);

        return list;
    }
}
