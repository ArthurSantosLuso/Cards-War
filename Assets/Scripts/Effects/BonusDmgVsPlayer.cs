using UnityEngine;

[CreateAssetMenu(fileName = "BonusDmgVsPlayer", menuName = "Effects/BonusDmgVsPlayer")]
public class BonusDmgVsPlayer : EffectSO
{
    [SerializeField] private int bonusDamage = 2;

    public override int ModifyOutgoingDamageVsPlayer(int baseDamage, UnitController attacker)
        => baseDamage + bonusDamage;
}
