using System.Collections.Generic;

public sealed class CombatantRegistry : ICombatantRegistry
{
    private readonly List<RegisteredCombatant> _combatants = new List<RegisteredCombatant>();

    public IReadOnlyList<RegisteredCombatant> Combatants => _combatants;

    public void Register(RegisteredCombatant combatant)
    {
        if (combatant == null)
        {
            return;
        }

        if (_combatants.Contains(combatant))
        {
            return;
        }

        _combatants.Add(combatant);
    }

    public void Unregister(RegisteredCombatant combatant)
    {
        if (combatant == null)
        {
            return;
        }

        _combatants.Remove(combatant);
    }
}
