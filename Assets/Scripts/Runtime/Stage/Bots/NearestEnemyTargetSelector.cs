using System.Collections.Generic;
using UnityEngine;

public sealed class NearestEnemyTargetSelector : ITargetSelector
{
    public ICombatant SelectTarget(
        ICombatant observer,
        IReadOnlyList<ICombatant> candidates,
        ITeamRelationshipService relationshipService)
    {
        if (observer == null || candidates == null || relationshipService == null)
        {
            return null;
        }

        ICombatant nearest = null;
        float nearestSqrDistance = float.MaxValue;
        Vector3 observerPosition = observer.TargetTransform.position;

        foreach (ICombatant candidate in candidates)
        {

            if (candidate == null || candidate == observer || !candidate.IsTargetable)
            {
                continue;
            }

            if (!relationshipService.AreEnemies(observer.TeamMember, candidate.TeamMember))
            {
                continue;
            }

            Vector3 candidatePosition = candidate.TargetTransform.position;
            float deltaX = candidatePosition.x - observerPosition.x;
            float deltaY = candidatePosition.y - observerPosition.y;
            float sqrDistance = deltaX * deltaX + deltaY * deltaY;

            if (sqrDistance < nearestSqrDistance)
            {
                nearestSqrDistance = sqrDistance;
                nearest = candidate;
            }
        }

        return nearest;
    }
}
