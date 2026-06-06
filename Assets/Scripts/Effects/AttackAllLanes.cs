using UnityEngine;

[CreateAssetMenu(fileName = "AttackAllLanes", menuName = "Effects/AttackAllLanes")]
public class AttackAllLanes : EffectSO
{
    public override bool AttacksAllLanes(UnitController self) => true;
}
