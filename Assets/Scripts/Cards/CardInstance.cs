[System.Serializable]
public class CardInstance
{
    public int InstanceId;

    public int CardId;

    public int OwnerClientId;

    public int CurrentCost;

    public CardInstance(int instanceId, int cardId, int owner)
    {
        InstanceId = instanceId;
        CardId = cardId;
        OwnerClientId = owner;
    }
}
