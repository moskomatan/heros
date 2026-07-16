using System.Collections.Generic;

public interface ITargetSelector
{
    RegisteredCombatant SelectTarget(
        RegisteredCombatant observer,
        IReadOnlyList<RegisteredCombatant> candidates,
        ITeamRelationshipService relationshipService);
}
