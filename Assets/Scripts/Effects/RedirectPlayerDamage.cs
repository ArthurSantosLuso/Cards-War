using UnityEngine;

[CreateAssetMenu(fileName = "RedirectPlayerDamage", menuName = "Effects/RedirectPlayerDamage")]
public class RedirectPlayerDamage : EffectSO
{
    public override bool RedirectsPlayerDamage(UnitController self, out UnitController redirectTarget)
    {
        redirectTarget = self;
        return true;
    }
}
