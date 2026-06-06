using UnityEngine;

[CreateAssetMenu(fileName = "Revive", menuName = "Effects/Revive")]
public class Revive : EffectSO
{
    // Per unit revive state. Since ScriptableObjects are shared assets, we need to track
    // which UnitControllers have already consumed their revive via a HashSet.
    private System.Collections.Generic.HashSet<UnitController> _revived = new();

    public override bool OnDeath(UnitController self)
    {
        if (_revived.Contains(self))
        {
            // Already revived once — let it die normally
            _revived.Remove(self);
            return false;
        }

        _revived.Add(self);

        // Revive with half max HP. SetHealth is a server-authoritative method on UnitController.
        int reviveHp = Mathf.Max(1, self.MaxHealth / 2);
        self.SetHealth(reviveHp);

        Debug.Log($"[Combat] Unit revived with {reviveHp} HP!");
        return true; // cancel despawn
    }
}
