using UnityEngine;

public enum CardTypeEnumExample { Creature = 1, Land = 2, Instant = 3 }


[CreateAssetMenu(fileName = "CardExample", menuName = "CardExample", order = 0)]
public class CardExample : ScriptableObject
{
    public CardTypeEnumExample type;

    public string cardname;
    public Color color;
    public CardExample someOtherCard;

    [Range(0.0f, 10.0f)]
    public float someFloat;

    [Range(1, 5)]
    public int count = 1;

    public bool someBool;

    public string abilityText;

    [Range(0, 8)]
    public int score;

    [Range(0, 3)]
    public int deck;

    public Sprite immagine;

    public Vector3 vector3;
    public Vector2 vector2;
    public Vector3Int vector3int;
    public Vector2Int vector2int;

}