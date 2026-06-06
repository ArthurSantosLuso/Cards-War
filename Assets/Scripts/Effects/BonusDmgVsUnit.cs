using UnityEngine;

[CreateAssetMenu(fileName = "BonusDmgVsUnit", menuName = "Effects/BonusDmgVsUnit")]
public class BonusDmgVsUnit : EffectSO
{
    [SerializeField] private int bonusDamage = 2;

    public override int ModifyOutgoingDamageVsUnit(int baseDamage, UnitController attacker, UnitController defender)
        => baseDamage + bonusDamage;
}
