using System;

public sealed class AttackHitValidator
{
    public AttackHitValidator(
        ITeamRelationshipService relationshipService,
        AttackExecutionTracker executionTracker)
    {
        _relationshipService = relationshipService
            ?? throw new ArgumentNullException(nameof(relationshipService));
        _executionTracker = executionTracker
            ?? throw new ArgumentNullException(nameof(executionTracker));
    }

    private readonly ITeamRelationshipService _relationshipService;
    private readonly AttackExecutionTracker _executionTracker;

    public bool IsValidHit(
        ICombatant attacker,
        ICombatant defender,
        IDamageReceiver defenderReceiver)
    {
        if (attacker == null || defender == null || defenderReceiver == null)
        {
            return false;
        }

        if (ReferenceEquals(attacker, defender))
        {
            return false;
        }

        if (!_relationshipService.AreEnemies(attacker.TeamMember, defender.TeamMember))
        {
            return false;
        }

        if (!defenderReceiver.IsAlive)
        {
            return false;
        }

        if (!defender.IsTargetable)
        {
            return false;
        }

        if (_executionTracker.HasHit(defender))
        {
            return false;
        }

        return true;
    }
}
