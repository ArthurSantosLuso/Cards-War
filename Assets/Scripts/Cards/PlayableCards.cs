using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "PlayableCards", menuName = "Scriptable Objects/PlayableCards")]
public class PlayableCards : ScriptableObject
{
    public List<Card> Cards;

    private Dictionary<int, Card> lookup;

    public void Initialize()
    {
        lookup = new Dictionary<int, Card>();

        foreach (var card in Cards)
        {
            lookup[card.CardId] = card;
        }
    }

    public Card GetCard(int id)
    {
        if (lookup == null)
            Initialize();

        if (lookup.TryGetValue(id, out Card card))
            return card;

        return null;
    }
}
