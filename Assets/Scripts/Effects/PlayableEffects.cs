using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PlayableEffects", menuName = "Scriptable Objects/PlayableEffects")]
public class PlayableEffects : ScriptableObject
{
    public List<EffectSO> Effects;

    private Dictionary<int, EffectSO> lookup;

    public void Initialize()
    {
        lookup = new Dictionary<int, EffectSO>();
        foreach (var effect in Effects)
        {
            if (effect != null)
                lookup[effect.EffectId] = effect;
        }
    }

    public EffectSO GetEffect(int id)
    {
        if (lookup == null) Initialize();
        return lookup.TryGetValue(id, out EffectSO effect) ? effect : null;
    }
}
