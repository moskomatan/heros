using System.Collections.Generic;
using System.Collections.ObjectModel;

public sealed class CombatantRegistry : ICombatantRegistry
{
    private readonly IList<ICombatant> _combatants = new List<ICombatant>();
    private readonly ReadOnlyCollection<ICombatant> _readOnlyCombatants;

    public CombatantRegistry()
    {
        _readOnlyCombatants = new ReadOnlyCollection<ICombatant>(_combatants);
    }

    public IReadOnlyList<ICombatant> Combatants => _readOnlyCombatants;

    public void Register(ICombatant combatant)
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

    public void Unregister(ICombatant combatant)
    {
        if (combatant == null)
        {
            return;
        }

        _combatants.Remove(combatant);
    }
}
