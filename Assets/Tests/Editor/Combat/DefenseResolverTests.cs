using NUnit.Framework;
using UnityEngine;

public sealed class DefenseResolverTests
{
    [Test]
    public void Mitigate_NormalHit_SubtractsNormalDefenseOnly()
    {
        DefenseResolver resolver = new DefenseResolver(defense: 3, criticalDefense: 4);
        DamageRequest request = CreateRequest(rawDamage: 10, isCritical: false);

        MitigationResult result = resolver.Mitigate(in request);

        Assert.That(result.NormalDefenseApplied, Is.EqualTo(3));
        Assert.That(result.CriticalDefenseApplied, Is.Zero);
        Assert.That(result.FinalDamage, Is.EqualTo(7));
    }

    [Test]
    public void Mitigate_CriticalHit_AppliesNormalAndCriticalDefense()
    {
        DefenseResolver resolver = new DefenseResolver(defense: 3, criticalDefense: 4);
        DamageRequest request = CreateRequest(rawDamage: 20, isCritical: true);

        MitigationResult result = resolver.Mitigate(in request);

        Assert.That(result.NormalDefenseApplied, Is.EqualTo(3));
        Assert.That(result.CriticalDefenseApplied, Is.EqualTo(4));
        Assert.That(result.FinalDamage, Is.EqualTo(13));
    }

    [Test]
    public void Mitigate_CriticalDefenseIgnoredForNormalHit()
    {
        DefenseResolver resolver = new DefenseResolver(defense: 3, criticalDefense: 4);
        DamageRequest request = CreateRequest(rawDamage: 10, isCritical: false);

        MitigationResult result = resolver.Mitigate(in request);

        Assert.That(result.CriticalDefenseApplied, Is.Zero);
        Assert.That(result.FinalDamage, Is.EqualTo(7));
    }

    [Test]
    public void Mitigate_FinalDamageNeverBelowOne()
    {
        DefenseResolver resolver = new DefenseResolver(defense: 50, criticalDefense: 50);
        DamageRequest request = CreateRequest(rawDamage: 10, isCritical: true);

        MitigationResult result = resolver.Mitigate(in request);

        Assert.That(result.FinalDamage, Is.EqualTo(1));
    }

    [Test]
    public void Constructor_NegativeDefenseValues_ClampsToZero()
    {
        DefenseResolver resolver = new DefenseResolver(defense: -3, criticalDefense: -4);
        DamageRequest request = CreateRequest(rawDamage: 10, isCritical: true);

        MitigationResult result = resolver.Mitigate(in request);

        Assert.That(result.NormalDefenseApplied, Is.Zero);
        Assert.That(result.CriticalDefenseApplied, Is.Zero);
        Assert.That(result.FinalDamage, Is.EqualTo(10));
    }

    private static DamageRequest CreateRequest(int rawDamage, bool isCritical)
    {
        return new DamageRequest(new FakeCombatant(), 1u, rawDamage, isCritical, Vector3.zero);
    }

    private sealed class FakeCombatant : ICombatant
    {
        public FakeCombatant()
        {
            TeamMember = new FakeTeamMember(TeamId.TeamOne);
            IsTargetable = true;
        }

        public ITeamMember TeamMember { get; }

        public Transform TargetTransform => null;

        public bool IsTargetable { get; }
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
