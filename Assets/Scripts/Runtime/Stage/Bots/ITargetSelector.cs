using System.Collections.Generic;

public interface ITargetSelector
{
    ICombatant SelectTarget(
        ICombatant observer,
        IReadOnlyList<ICombatant> candidates,
        ITeamRelationshipService relationshipService);
}
