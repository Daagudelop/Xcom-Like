using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;

/// <summary>
/// Point & click con NavMesh para los soldados seleccionados.
///  - Click derecho en el suelo: mover en formación
///  - Doble click derecho (o Shift + click derecho): correr
///  - S: detener a los seleccionados
/// </summary>
public class SquadMovement : MonoBehaviour
{
    [Header("Raycast")]
    public LayerMask groundMask;

    [Header("Formación")]
    public float spacing = 1.8f;
    public float navMeshSampleRadius = 3f;

    [Header("Input")]
    [Tooltip("0 = izquierdo, 1 = derecho. Con el izquierdo choca con la selección por caja.")]
    public int moveMouseButton = 1;
    public float doubleClickTime = 0.3f;

    [Header("Feedback (opcional)")]
    public GameObject moveMarkerPrefab;
    public float markerLifetime = 1.2f;

    private Camera cam;
    private float lastMoveTime;
    private Vector3 lastMovePoint;

    private void Awake() => cam = Camera.main;

    private void Update()
    {
        if (!SquadGameRules.CanSelectAndMove) return;

        var sel = SquadSelection.Instance;
        if (sel == null || sel.Selected.Count == 0) return;

        if (Input.GetKeyDown(KeyCode.S))
        {
            foreach (var s in sel.Selected) s.Stop();
        }

        if (!Input.GetMouseButtonDown(moveMouseButton)) return;
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit, 1000f, groundMask)) return;
        if (!NavMesh.SamplePosition(hit.point, out NavMeshHit navHit, navMeshSampleRadius, NavMesh.AllAreas)) return;

        bool doubleClick = Time.unscaledTime - lastMoveTime < doubleClickTime
                           && Vector3.Distance(lastMovePoint, navHit.position) < 2f;
        bool run = doubleClick || Input.GetKey(KeyCode.LeftShift);

        lastMoveTime = Time.unscaledTime;
        lastMovePoint = navHit.position;

        MoveSquad(sel.Selected, navHit.position, run);
        SpawnMarker(navHit.position);
    }

    public void MoveSquad(List<Soldier> soldiers, Vector3 destination, bool run)
    {
        if (soldiers.Count == 1)
        {
            soldiers[0].MoveTo(destination, run);
            return;
        }

        // Dirección de la formación: del centro del grupo hacia el destino
        Vector3 center = Vector3.zero;
        foreach (var s in soldiers) center += s.transform.position;
        center /= soldiers.Count;

        Vector3 forward = destination - center;
        forward.y = 0;
        forward = forward.sqrMagnitude > 0.01f ? forward.normalized : Vector3.forward;

        List<Vector3> slots = BuildFormation(destination, forward, soldiers.Count);

        // Asignación: cada slot (de adelante hacia atrás) toma al soldado libre más cercano
        var free = new List<Soldier>(soldiers);
        foreach (var slot in slots)
        {
            Soldier best = null;
            float bestDist = float.MaxValue;
            foreach (var s in free)
            {
                float d = (s.transform.position - slot).sqrMagnitude;
                if (d < bestDist) { bestDist = d; best = s; }
            }
            if (best == null) break;
            free.Remove(best);
            best.MoveTo(slot, run);
        }
    }

    private List<Vector3> BuildFormation(Vector3 center, Vector3 forward, int count)
    {
        var result = new List<Vector3>(count);
        int cols = Mathf.CeilToInt(Mathf.Sqrt(count));
        Quaternion rot = Quaternion.LookRotation(forward, Vector3.up);

        for (int i = 0; i < count; i++)
        {
            int row = i / cols;
            int col = i % cols;
            float x = (col - (cols - 1) * 0.5f) * spacing;
            float z = -row * spacing;

            Vector3 world = center + rot * new Vector3(x, 0, z);

            // Ajustar a NavMesh; si falla, usar el centro
            if (NavMesh.SamplePosition(world, out NavMeshHit h, spacing, NavMesh.AllAreas))
                world = h.position;
            else
                world = center;

            result.Add(world);
        }
        return result;
    }

    private void SpawnMarker(Vector3 pos)
    {
        if (moveMarkerPrefab == null) return;
        var m = Instantiate(moveMarkerPrefab, pos + Vector3.up * 0.05f, Quaternion.identity);
        Destroy(m, markerLifetime);
    }
}
