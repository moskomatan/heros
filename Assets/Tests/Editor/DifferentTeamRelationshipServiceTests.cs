using NUnit.Framework;

public sealed class DifferentTeamRelationshipServiceTests
{
    private readonly DifferentTeamRelationshipService _service = new DifferentTeamRelationshipService();

    [Test]
    public void AreEnemies_SameTeam_ReturnsFalse()
    {
        FakeTeamMember observer = new FakeTeamMember(TeamId.TeamOne);
        FakeTeamMember candidate = new FakeTeamMember(TeamId.TeamOne);

        Assert.That(_service.AreEnemies(observer, candidate), Is.False);
    }

    [Test]
    public void AreEnemies_DifferentNonNeutralTeams_ReturnsTrue()
    {
        FakeTeamMember observer = new FakeTeamMember(TeamId.TeamOne);
        FakeTeamMember candidate = new FakeTeamMember(TeamId.TeamTwo);

        Assert.That(_service.AreEnemies(observer, candidate), Is.True);
    }

    [Test]
    public void AreEnemies_NeutralObserver_ReturnsFalse()
    {
        FakeTeamMember observer = new FakeTeamMember(TeamId.Neutral);
        FakeTeamMember candidate = new FakeTeamMember(TeamId.TeamTwo);

        Assert.That(_service.AreEnemies(observer, candidate), Is.False);
    }

    [Test]
    public void AreEnemies_NeutralCandidate_ReturnsFalse()
    {
        FakeTeamMember observer = new FakeTeamMember(TeamId.TeamOne);
        FakeTeamMember candidate = new FakeTeamMember(TeamId.Neutral);

        Assert.That(_service.AreEnemies(observer, candidate), Is.False);
    }

    [Test]
    public void AreEnemies_NullObserver_ReturnsFalse()
    {
        FakeTeamMember candidate = new FakeTeamMember(TeamId.TeamTwo);

        Assert.That(_service.AreEnemies(null, candidate), Is.False);
    }

    [Test]
    public void AreEnemies_NullCandidate_ReturnsFalse()
    {
        FakeTeamMember observer = new FakeTeamMember(TeamId.TeamOne);

        Assert.That(_service.AreEnemies(observer, null), Is.False);
    }

    private sealed class FakeTeamMember : ITeamMember
    {
        public FakeTeamMember(TeamId team)
        {
            Team = team;
        }

        public TeamId Team { get; }
    }
}
