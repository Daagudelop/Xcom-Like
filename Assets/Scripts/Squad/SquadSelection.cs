using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Selección estilo Wasteland 3 / RTS:
///  - Click: selecciona uno
///  - Shift o Ctrl + click: agrega / quita de la selección
///  - Arrastrar: selección por caja
///  - Doble click sobre soldado: selecciona todos los visibles en pantalla
///  - Espacio: selecciona a todo el escuadrón
///  - Ctrl + 1..9: guarda grupo | 1..9: recupera grupo
/// </summary>
public class SquadSelection : MonoBehaviour
{
    public static SquadSelection Instance { get; private set; }

    [Header("Config")]
    public LayerMask soldierMask;
    public float dragThreshold = 8f;       // píxeles para considerar arrastre
    public float doubleClickTime = 0.3f;
    public Color boxFill = new Color(0.2f, 0.9f, 0.4f, 0.15f);
    public Color boxBorder = new Color(0.2f, 0.9f, 0.4f, 0.9f);

    [Tooltip("Si es true, al salir de exploración se limpia la selección. " +
             "Déjalo en false si quieres seguir usando el inventario del soldado seleccionado en combate.")]
    public bool clearSelectionOnExit = false;

    public readonly List<Soldier> Selected = new List<Soldier>();
    public event Action OnSelectionChanged;

    private Camera cam;
    private Vector2 dragStart;
    private bool pressing;
    private bool dragging;
    private float lastClickTime;
    private readonly List<Soldier>[] groups = new List<Soldier>[9];

    private void Awake()
    {
        Instance = this;
        cam = Camera.main;
    }

    private void OnEnable() => GameManager.OnStateExit += HandleStateExit;
    private void OnDisable() => GameManager.OnStateExit -= HandleStateExit;

    private void HandleStateExit(GameState state)
    {
        if (state != GameState.InExplorationMode) return;

        CancelDrag();
        if (clearSelectionOnExit) ClearSelection();
    }

    private void CancelDrag()
    {
        pressing = false;
        dragging = false;
    }

    private void Update()
    {
        if (!SquadGameRules.CanSelectAndMove)
        {
            if (pressing || dragging) CancelDrag();
            return;
        }

        HandleMouse();
        HandleKeyboard();
    }

    // ---------------- MOUSE ----------------

    private void HandleMouse()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
            pressing = true;
            dragging = false;
            dragStart = Input.mousePosition;
        }

        if (pressing && Input.GetMouseButton(0))
        {
            if (!dragging && Vector2.Distance(dragStart, Input.mousePosition) > dragThreshold)
                dragging = true;
        }

        if (pressing && Input.GetMouseButtonUp(0))
        {
            bool additive = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.LeftControl);

            if (dragging) BoxSelect(additive);
            else ClickSelect(additive);

            pressing = false;
            dragging = false;
        }
    }

    private void ClickSelect(bool additive)
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        Soldier hitSoldier = null;

        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, soldierMask))
            hitSoldier = hit.collider.GetComponentInParent<Soldier>();

        bool isDouble = Time.unscaledTime - lastClickTime < doubleClickTime;
        lastClickTime = Time.unscaledTime;

        if (hitSoldier == null)
        {
            if (!additive) ClearSelection();
            return;
        }

        if (isDouble)
        {
            SelectAllOnScreen();
            return;
        }

        if (additive)
        {
            if (hitSoldier.IsSelected) Deselect(hitSoldier);
            else Select(hitSoldier);
        }
        else
        {
            SelectOnly(hitSoldier);
        }
    }

    private void BoxSelect(bool additive)
    {
        Vector2 end = Input.mousePosition;
        Rect rect = Rect.MinMaxRect(
            Mathf.Min(dragStart.x, end.x), Mathf.Min(dragStart.y, end.y),
            Mathf.Max(dragStart.x, end.x), Mathf.Max(dragStart.y, end.y));

        if (!additive) ClearSelection(false);

        foreach (var s in Soldier.All)
        {
            Vector3 sp = cam.WorldToScreenPoint(s.transform.position + Vector3.up);
            if (sp.z > 0 && rect.Contains(sp)) Select(s, false);
        }
        OnSelectionChanged?.Invoke();
    }

    private void SelectAllOnScreen()
    {
        ClearSelection(false);
        foreach (var s in Soldier.All)
        {
            Vector3 sp = cam.WorldToScreenPoint(s.transform.position);
            if (sp.z > 0 && sp.x >= 0 && sp.x <= Screen.width && sp.y >= 0 && sp.y <= Screen.height)
                Select(s, false);
        }
        OnSelectionChanged?.Invoke();
    }

    // ---------------- TECLADO ----------------

    private void HandleKeyboard()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            ClearSelection(false);
            foreach (var s in Soldier.All) Select(s, false);
            OnSelectionChanged?.Invoke();
        }

        bool ctrl = Input.GetKey(KeyCode.LeftControl);
        for (int i = 0; i < 9; i++)
        {
            if (!Input.GetKeyDown(KeyCode.Alpha1 + i)) continue;

            if (ctrl)
            {
                groups[i] = new List<Soldier>(Selected);
            }
            else if (groups[i] != null && groups[i].Count > 0)
            {
                ClearSelection(false);
                foreach (var s in groups[i]) if (s != null) Select(s, false);
                OnSelectionChanged?.Invoke();
            }
        }
    }

    // ---------------- API ----------------

    public void SelectOnly(Soldier s)
    {
        ClearSelection(false);
        Select(s, false);
        OnSelectionChanged?.Invoke();
    }

    public void Select(Soldier s, bool notify = true)
    {
        if (s == null || s.IsSelected) return;
        s.SetSelected(true);
        Selected.Add(s);
        if (notify) OnSelectionChanged?.Invoke();
    }

    public void Deselect(Soldier s)
    {
        if (s == null || !s.IsSelected) return;
        s.SetSelected(false);
        Selected.Remove(s);
        OnSelectionChanged?.Invoke();
    }

    public void ClearSelection(bool notify = true)
    {
        foreach (var s in Selected) if (s != null) s.SetSelected(false);
        Selected.Clear();
        if (notify) OnSelectionChanged?.Invoke();
    }

    // ---------------- CAJA DE SELECCIÓN ----------------

    private void OnGUI()
    {
        if (!dragging) return;

        Vector2 end = Input.mousePosition;
        float x = Mathf.Min(dragStart.x, end.x);
        float yTop = Screen.height - Mathf.Max(dragStart.y, end.y);
        float w = Mathf.Abs(end.x - dragStart.x);
        float h = Mathf.Abs(end.y - dragStart.y);
        Rect r = new Rect(x, yTop, w, h);

        DrawRect(r, boxFill);
        DrawRect(new Rect(r.x, r.y, r.width, 2), boxBorder);
        DrawRect(new Rect(r.x, r.yMax - 2, r.width, 2), boxBorder);
        DrawRect(new Rect(r.x, r.y, 2, r.height), boxBorder);
        DrawRect(new Rect(r.xMax - 2, r.y, 2, r.height), boxBorder);
    }

    private static void DrawRect(Rect r, Color c)
    {
        Color prev = GUI.color;
        GUI.color = c;
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.color = prev;
    }
}
