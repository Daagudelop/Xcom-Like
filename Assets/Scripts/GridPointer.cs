
using AYellowpaper.SerializedCollections;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class GridPointer : MonoBehaviour
{
    #region Variables
    public static GridPointer instance;
    public LayerMask walkableLayer;

    [HideInInspector] public int entityCellSize;
    [HideInInspector] public Vector3 LastPos = Vector3.zero;
    [HideInInspector] public Camera mainC;
    [HideInInspector] public Vector3 pos;
    public Entity currentEntityCore;
    public SerializedDictionary<Vector3, float> allGridPoints;

    public event Action<Vector3> OnGridPointConfirmed;

    

    #endregion

    private void Awake()
    {
        mainC = Camera.main;
        if (instance == null) instance = this;
    }

    private void Update() => MovePointerToMouse();

    public void MoveToPoint()
    {
        currentEntityCore.GetComponent<NavMeshAgent>().SetDestination(transform.position);
    }

    // Quita el -0 y errores de precisión. Úsalo TAMBIÉN al llenar allGridPoints.
    public static Vector3 NormalizeKey(Vector3 v)
    {
        return new Vector3(
            Mathf.Round(v.x * 100f) / 100f + 0f,   // "+ 0f" convierte -0 en +0
            Mathf.Round(v.y * 100f) / 100f + 0f,
            Mathf.Round(v.z * 100f) / 100f + 0f);
    }

    public void MovePointerToMouse()
    {
        if (currentEntityCore == null || allGridPoints == null || allGridPoints.Count == 0) return;

        entityCellSize = currentEntityCore.CellSize;
        Ray ray = mainC.ScreenPointToRay(Input.mousePosition);

        if (!Physics.Raycast(ray, out RaycastHit hit, float.MaxValue, walkableLayer)) return;

        Vector3 cell = (Vector3)Utilities.CenterPosOnCellYNull(hit.point, entityCellSize);

        if (!TryFindGridPoint(cell, hit.point.y, out Vector3 found)) return;

        pos = found;
        transform.position = found;
        LastPos = found;

        // El click ya no depende de condiciones raras: si hay punto válido, confirma
        if (Input.GetMouseButtonDown(1))
            OnGridPointConfirmed?.Invoke(pos);
    }

    private bool TryFindGridPoint(Vector3 cell, float hitY, out Vector3 found)
    {
        // 1) Búsqueda exacta con llave normalizada
        Vector3 exact = NormalizeKey(new Vector3(cell.x, Mathf.Round(hitY / 2f) * 2f, cell.z));
        if (allGridPoints.ContainsKey(exact))
        {
            found = exact;
            return true;
        }

        // 2) Fallback: misma celda XZ, la Y más cercana al hit
        //    (solo corre cuando la exacta falla)
        found = default;
        float bestDy = entityCellSize + 0.01f;   // tolerancia máxima en Y
        bool ok = false;

        foreach (var kv in allGridPoints)
        {
            Vector3 k = kv.Key;
            if (Mathf.Abs(k.x - cell.x) > 0.01f || Mathf.Abs(k.z - cell.z) > 0.01f) continue;

            float dy = Mathf.Abs(k.y - hitY);
            if (dy < bestDy)
            {
                bestDy = dy;
                found = k;       // usa la llave original del diccionario
                ok = true;
            }
        }
        return ok;
    }
}