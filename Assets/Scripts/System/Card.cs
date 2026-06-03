using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "CardDefinition", menuName = "Scriptable Objects/Card")]
public class Card : ScriptableObject
{
    [Header("Identity")]
    public int CardId;
    public string CardName;

    [TextArea]
    public string CardDescription;

    [Header("Gameplay")]
    public int Damage;
    public int Health;
    public int Cost;

    [Header("Visual")]
    public Sprite Artwork;

    [Header("Prefab")]
    public GameObject CardPrefab;
}
