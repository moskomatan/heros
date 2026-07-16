public sealed class DifferentTeamRelationshipService : ITeamRelationshipService
{
    public bool AreEnemies(ITeamMember observer, ITeamMember candidate)
    {
        if (observer == null || candidate == null)
        {
            return false;
        }

        if (observer.Team == TeamId.Neutral || candidate.Team == TeamId.Neutral)
        {
            return false;
        }

        return observer.Team != candidate.Team;
    }
}
