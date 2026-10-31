using UnityEngine;

public enum ItemType { Weapon, Armor, Consumable, Ammo, Misc }

[CreateAssetMenu(menuName = "Squad/Item")]
public class ItemData : ScriptableObject
{
    public string displayName = "Item";
    public Sprite icon;
    public ItemType type;
    [Min(1)] public int maxStack = 1;
    [Min(0)] public float weight = 0.5f;
    [TextArea] public string description;
}
