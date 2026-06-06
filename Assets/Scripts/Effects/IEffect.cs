public interface IEffect
{
    /// <summary>
    /// Unique ID used to sync this effect over the network.
    /// </summary>
    int EffectId { get; }

    /// <summary>
    /// Called before this unit deals damage to an enemy unit.
    /// </summary>
    /// <returns>Return the modified damage value.</returns>
    int ModifyOutgoingDamageVsUnit(int baseDamage, UnitController attacker, UnitController defender);


    /// <summary>
    /// Called before this unit deals damage to enemy player.
    /// </summary>
    /// <returns>Return the modified damage value.</returns>
    int ModifyOutgoingDamageVsPlayer(int baseDamage, UnitController attacker);

    /// <summary>
    /// Called before this unit receives damage.
    /// </summary>
    /// <returns>Return the modified damage value.</returns>
    int ModifyIncomingDamage(int baseDamage, UnitController defender);

    /// <summary>
    /// If true, this unit intercepts all direct player damage dealt to its owner this round.
    /// The out parameter receives the redirected damage instead of the player.
    /// </summary>
    bool RedirectsPlayerDamage(UnitController self, out UnitController redirectTarget);

    /// <summary>
    /// If true, the unit will attack all enemy lanes instead of just the opposing one.
    /// </summary>
    bool AttacksAllLanes(UnitController self);

    /// <summary>
    /// Called server side when this unit's HP reaches 0, before Despawn.
    /// Return true to cancel the despawn. The effect is responsible
    /// for setting the unit's HP to the revived value via TakeDamage or SetHealth.
    /// </summary>
    bool OnDeath(UnitController self);
}