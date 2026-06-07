using System.Collections.Generic;

public class PlayerDeckState
{
    public List<CardInstance> Hand = new List<CardInstance>();
    public Queue<CardInstance> Deck = new Queue<CardInstance>();

    public int TotalDraw = 0;

    public const int MAX_HAND_SIZE = 4;
    public const int MAX_TOTAL_DRAWS = 20;
}
