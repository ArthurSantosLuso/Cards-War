using UnityEditor.AdaptivePerformance.Editor;
using UnityEngine;

[System.Serializable]
public class CardInstance
{
    public int InstanceId;

    public int CardId;

    public ulong OwnerClientId;

    public int CurrentCost;

    public CardInstance(int instanceId, int cardId, ulong owner)
    {
        InstanceId = instanceId;
        CardId = cardId;
        OwnerClientId = owner;
    }
}
