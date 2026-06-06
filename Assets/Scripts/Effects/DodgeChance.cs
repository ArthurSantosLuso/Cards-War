using UnityEngine;

[CreateAssetMenu(fileName = "DodgeChance", menuName = "Effects/DodgeChance")]
public class DodgeChance : EffectSO
{
    [SerializeField][Range(0f, 1f)] private float dodgeChance = 0.4f;

    public override int ModifyIncomingDamage(int baseDamage, UnitController defender)
    {
        if (Random.value < dodgeChance)
        {
            Debug.Log($"[Combat] Unit on tile dodged {baseDamage} damage!");
            return 0;
        }
        return baseDamage;
    }

}
