using UnityEngine;

public enum ItemColorEnumExample { Black = 0, Yellow = 1, Green = 2, Blue = 3, Red = 4 }



[CreateAssetMenu(fileName = "ItemExample", menuName = "ItemExample", order = 1)]
public class ItemExample : ScriptableObject
{
    public string itemName;
    public ItemColorEnumExample color;
    public ItemColorEnumExample color2;
    [Range(1, 5)]
    public int count = 1;
    public CardExample card;
}
