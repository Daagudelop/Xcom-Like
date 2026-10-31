using UnityEngine;

/// <summary>
/// HUD mínimo con OnGUI para probar el sistema sin armar UI todavía.
/// Muestra los soldados seleccionados y el inventario del primero.
/// Reemplázalo luego por tu UI real escuchando SquadSelection.OnSelectionChanged
/// y Inventory.OnChanged.
/// </summary>
public class SquadDebugHUD : MonoBehaviour
{
    public ItemData testItem;   // opcional: arrastra un item para probar con T

    private void Update()
    {
        if (!SquadGameRules.CanUseInventory) return;

        var sel = SquadSelection.Instance;
        if (sel == null || sel.Selected.Count == 0) return;

        // T = agrega un item de prueba al primer soldado seleccionado
        if (Input.GetKeyDown(KeyCode.T) && testItem != null)
            sel.Selected[0].Inventory.Add(testItem, 1);

        // Y = pasa 1 item del primero al segundo seleccionado
        if (Input.GetKeyDown(KeyCode.Y) && testItem != null && sel.Selected.Count > 1)
            sel.Selected[0].Inventory.Transfer(sel.Selected[1].Inventory, testItem, 1);
    }

    private void OnGUI()
    {
        if (!SquadGameRules.CanUseInventory) return;

        var sel = SquadSelection.Instance;
        if (sel == null) return;

        GUILayout.BeginArea(new Rect(10, 10, 280, 400), GUI.skin.box);
        GUILayout.Label($"Seleccionados: {sel.Selected.Count}");

        foreach (var s in sel.Selected)
            GUILayout.Label($"• {s.soldierName} {(s.IsMoving ? "(moviendo)" : "")}");

        if (sel.Selected.Count > 0)
        {
            var inv = sel.Selected[0].Inventory;
            GUILayout.Space(8);
            GUILayout.Label($"Inventario de {sel.Selected[0].soldierName} " +
                            $"({inv.slots.Count}/{inv.capacity}) - {inv.CurrentWeight:0.0}/{inv.maxWeight} kg");
            foreach (var slot in inv.slots)
                GUILayout.Label($"   {slot.item.displayName} x{slot.amount}");
        }
        GUILayout.EndArea();
    }
}
