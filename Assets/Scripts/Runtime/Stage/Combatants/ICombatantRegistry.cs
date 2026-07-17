using System.Collections.Generic;

public interface ICombatantRegistry
{
    IReadOnlyList<ICombatant> Combatants { get; }

    void Register(ICombatant combatant);

    void Unregister(ICombatant combatant);
}
