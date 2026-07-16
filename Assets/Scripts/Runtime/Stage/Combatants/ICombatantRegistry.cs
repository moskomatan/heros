using System.Collections.Generic;

public interface ICombatantRegistry
{
    IReadOnlyList<RegisteredCombatant> Combatants { get; }

    void Register(RegisteredCombatant combatant);

    void Unregister(RegisteredCombatant combatant);
}
