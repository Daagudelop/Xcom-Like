using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Utilities : MonoBehaviour
{
    #region snap on navmesh
    
    public static void SnapOnNavmesh(GameObject SourcePos)
    {
        NavMeshHit hit;
        NavMesh.SamplePosition(SourcePos.transform.position, out hit, float.MaxValue, NavMesh.AllAreas);
        SourcePos.transform.position = new Vector3(hit.position.x, hit.position.y, hit.position.z);
    }
    public static void SnapOnNavmesh(ref Vector3 SourcePos)
    {
        NavMeshHit hit;
        NavMesh.SamplePosition(SourcePos, out hit, float.MaxValue, NavMesh.AllAreas);
        SourcePos = new Vector3(hit.position.x, hit.position.y, hit.position.z);
    }
    public static NavMeshHit GetHitSnapOnNavmesh(ref Vector3 SourcePos)
    {
        NavMeshHit hit;
        NavMesh.SamplePosition(SourcePos, out hit, float.MaxValue, NavMesh.AllAreas);
        return hit;
    }

    public static Vector3 RoundToGrid(Vector3 pos)
    {
        return new Vector3(
            Mathf.Round(pos.x / 2f) * 2f,
            Mathf.Round(pos.y / 2f) * 2f,
            Mathf.Round(pos.z / 2f) * 2f
        );
    }

    #endregion

    #region inside triangle

    /* A utility function to calculate area of triangle 
    formed by (x1, z1, z1) (x2, z2, z2) and (x3, z3, z3) */
    public static double area(
        float x1, float z1,
        float x2, float z2,
        float x3, float z3)
    {
        return Math.Abs((x1 * (z2 - z3) +
                         x2 * (z3 - z1) +
                         x3 * (z1 - z2))
                         / 2.0);
    }

    /* A function to check whether point P(x, y) lies
    inside the triangle formed by A(x1, z1),
    B(x2, z2) and C(x3, z3) */
    public static bool isInside(float x1, float y1, float x2,
                         float y2, float x3, float y3,
                         float x, float y)
    {
        /* Calculate area of triangle ABC */
        double A = area(x1, y1, x2, y2, x3, y3);

        /* Calculate area of triangle PBC */
        double A1 = area(x, y, x2, y2, x3, y3);

        /* Calculate area of triangle PAC */
        double A2 = area(x1, y1, x, y, x3, y3);

        /* Calculate area of triangle PAB */
        double A3 = area(x1, y1, x2, y2, x, y);

        /* Check if sum of A1, A2 and A3 is same as A */
        return (A == A1 + A2 + A3);
    }

    #endregion

    #region cell pos
    public static float? CenterOnCell(float number, float cellSize)
    {
        if (number % cellSize != 0)
        {
            float cellsInN = number / cellSize;
            if (Mathf.Abs(cellsInN - Mathf.Floor(cellsInN)) < 0.5f)
            {
                //debug.Log("floor" + number);
                return cellSize * Mathf.Floor(cellsInN);
            }
            else
            {
                //debug.Log("ceil" +number);
                return cellSize * Mathf.Ceil(cellsInN);
            }
        }
        else
        {
            return number;
        }
    }

    public static Vector3? CenterPosOnCell(Vector3 pos, float cellSize)
    {
        return new Vector3(
            (float)CenterOnCell(pos.x, cellSize),
            (float)CenterOnCell(pos.y, cellSize),
            (float)CenterOnCell(pos.z, cellSize)
            );
    }
    public static Vector3? CenterPosOnCellYNull(Vector3 pos, float cellSize)
    {
        return new Vector3(
            (float)CenterOnCell(pos.x, cellSize),
            pos.y,
            (float)CenterOnCell(pos.z, cellSize)
            );
    }

    #endregion
}
