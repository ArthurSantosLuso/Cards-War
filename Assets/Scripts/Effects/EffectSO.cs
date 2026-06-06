using UnityEngine;

public abstract class EffectSO : ScriptableObject, IEffect
{
    [Header("Identity")]
    public int effectId;

    public int EffectId => effectId;


    public virtual int ModifyOutgoingDamageVsUnit(int baseDamage, UnitController attacker, UnitController defender)
        => baseDamage;

    public virtual int ModifyOutgoingDamageVsPlayer(int baseDamage, UnitController attacker)
        => baseDamage;

    public virtual int ModifyIncomingDamage(int baseDamage, UnitController defender)
        => baseDamage;

    public virtual bool RedirectsPlayerDamage(UnitController self, out UnitController redirectTarget)
    {
        redirectTarget = null;
        return false;
    }

    public virtual bool AttacksAllLanes(UnitController self) => false;

    public virtual bool OnDeath(UnitController self) => false;
}
