using System.Collections.Generic;
using System.Collections.ObjectModel;

public sealed class CombatantRegistry : ICombatantRegistry
{
    private readonly List<RegisteredCombatant> _combatants = new List<RegisteredCombatant>();
    private readonly ReadOnlyCollection<RegisteredCombatant> _readOnlyCombatants;

    public CombatantRegistry()
    {
        _readOnlyCombatants = _combatants.AsReadOnly();
    }

    public IReadOnlyList<RegisteredCombatant> Combatants => _readOnlyCombatants;

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
