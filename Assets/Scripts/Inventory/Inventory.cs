using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ItemStack
{
    public ItemData item;
    public int amount;

    public ItemStack(ItemData item, int amount)
    {
        this.item = item;
        this.amount = amount;
    }
}

public class Inventory : MonoBehaviour
{
    [Min(1)] public int capacity = 20;          // número máximo de slots
    [Min(0)] public float maxWeight = 50f;      // 0 = sin límite de peso
    public List<ItemStack> slots = new List<ItemStack>();

    public event Action OnChanged;

    public float CurrentWeight
    {
        get
        {
            float w = 0;
            foreach (var s in slots) w += s.item.weight * s.amount;
            return w;
        }
    }

    public int Count(ItemData item)
    {
        int total = 0;
        foreach (var s in slots) if (s.item == item) total += s.amount;
        return total;
    }

    public bool Has(ItemData item, int amount = 1) => Count(item) >= amount;

    /// <summary>Intenta agregar. Devuelve la cantidad que NO cupo.</summary>
    public int Add(ItemData item, int amount)
    {
        if (item == null || amount <= 0) return amount;

        // Límite por peso
        if (maxWeight > 0 && item.weight > 0)
        {
            int byWeight = Mathf.FloorToInt((maxWeight - CurrentWeight) / item.weight);
            amount = Mathf.Min(amount, Mathf.Max(0, byWeight)) + 0;
        }
        int requested = amount;
        int leftover = 0;

        // 1) completar stacks existentes
        foreach (var s in slots)
        {
            if (amount <= 0) break;
            if (s.item != item || s.amount >= item.maxStack) continue;
            int add = Mathf.Min(item.maxStack - s.amount, amount);
            s.amount += add;
            amount -= add;
        }

        // 2) crear stacks nuevos
        while (amount > 0 && slots.Count < capacity)
        {
            int add = Mathf.Min(item.maxStack, amount);
            slots.Add(new ItemStack(item, add));
            amount -= add;
        }

        leftover = amount;
        if (requested != leftover) OnChanged?.Invoke();
        return leftover;
    }

    public bool Remove(ItemData item, int amount = 1)
    {
        if (!Has(item, amount)) return false;

        for (int i = slots.Count - 1; i >= 0 && amount > 0; i--)
        {
            if (slots[i].item != item) continue;
            int take = Mathf.Min(slots[i].amount, amount);
            slots[i].amount -= take;
            amount -= take;
            if (slots[i].amount <= 0) slots.RemoveAt(i);
        }
        OnChanged?.Invoke();
        return true;
    }

    /// <summary>Pasa items a otro inventario (intercambio entre soldados).</summary>
    public int Transfer(Inventory target, ItemData item, int amount)
    {
        amount = Mathf.Min(amount, Count(item));
        if (amount <= 0 || target == null) return 0;

        int leftover = target.Add(item, amount);
        int moved = amount - leftover;
        if (moved > 0) Remove(item, moved);
        return moved;
    }
}
