using NUnit.Framework;
using UnityEngine;

public sealed class AttackHitRuleTests
{
    private DifferentTeamRelationshipService _relationshipService;
    private AttackExecutionTracker _tracker;
    private AttackHitValidator _validator;

    [SetUp]
    public void SetUp()
    {
        _relationshipService = new DifferentTeamRelationshipService();
        _tracker = new AttackExecutionTracker();
        _validator = new AttackHitValidator(_relationshipService, _tracker);
        _tracker.BeginExecution(1u);
    }

    [Test]
    public void IsValidHit_Allies_ReturnsFalse()
    {
        FakeCombatant attacker = new FakeCombatant(TeamId.TeamOne);
        FakeCombatant ally = new FakeCombatant(TeamId.TeamOne);
        FakeDamageReceiver receiver = new FakeDamageReceiver(isAlive: true);

        Assert.That(_validator.IsValidHit(attacker, ally, receiver), Is.False);
    }

    [Test]
    public void IsValidHit_Neutral_ReturnsFalse()
    {
        FakeCombatant attacker = new FakeCombatant(TeamId.TeamOne);
        FakeCombatant neutral = new FakeCombatant(TeamId.Neutral);
        FakeDamageReceiver receiver = new FakeDamageReceiver(isAlive: true);

        Assert.That(_validator.IsValidHit(attacker, neutral, receiver), Is.False);
    }

    [Test]
    public void IsValidHit_Self_ReturnsFalse()
    {
        FakeCombatant attacker = new FakeCombatant(TeamId.TeamOne);
        FakeDamageReceiver receiver = new FakeDamageReceiver(isAlive: true);

        Assert.That(_validator.IsValidHit(attacker, attacker, receiver), Is.False);
    }

    [Test]
    public void IsValidHit_DeadDefender_ReturnsFalse()
    {
        FakeCombatant attacker = new FakeCombatant(TeamId.TeamOne);
        FakeCombatant enemy = new FakeCombatant(TeamId.TeamTwo);
        FakeDamageReceiver receiver = new FakeDamageReceiver(isAlive: false);

        Assert.That(_validator.IsValidHit(attacker, enemy, receiver), Is.False);
    }

    [Test]
    public void IsValidHit_UntargetableDefender_ReturnsFalse()
    {
        FakeCombatant attacker = new FakeCombatant(TeamId.TeamOne);
        FakeCombatant enemy = new FakeCombatant(TeamId.TeamTwo, isTargetable: false);
        FakeDamageReceiver receiver = new FakeDamageReceiver(isAlive: true);

        Assert.That(_validator.IsValidHit(attacker, enemy, receiver), Is.False);
    }

    [Test]
    public void IsValidHit_EnemyAliveTargetable_ReturnsTrue()
    {
        FakeCombatant attacker = new FakeCombatant(TeamId.TeamOne);
        FakeCombatant enemy = new FakeCombatant(TeamId.TeamTwo);
        FakeDamageReceiver receiver = new FakeDamageReceiver(isAlive: true);

        Assert.That(_validator.IsValidHit(attacker, enemy, receiver), Is.True);
    }

    [Test]
    public void IsValidHit_SameTargetTwiceInExecution_SecondRejected()
    {
        FakeCombatant attacker = new FakeCombatant(TeamId.TeamOne);
        FakeCombatant enemy = new FakeCombatant(TeamId.TeamTwo);
        FakeDamageReceiver receiver = new FakeDamageReceiver(isAlive: true);

        Assert.That(_validator.IsValidHit(attacker, enemy, receiver), Is.True);
        _tracker.RecordHit(enemy);

        Assert.That(_validator.IsValidHit(attacker, enemy, receiver), Is.False);
    }

    [Test]
    public void IsValidHit_NewExecution_AllowsSameTargetAgain()
    {
        FakeCombatant attacker = new FakeCombatant(TeamId.TeamOne);
        FakeCombatant enemy = new FakeCombatant(TeamId.TeamTwo);
        FakeDamageReceiver receiver = new FakeDamageReceiver(isAlive: true);

        _tracker.RecordHit(enemy);
        Assert.That(_validator.IsValidHit(attacker, enemy, receiver), Is.False);

        _tracker.BeginExecution(2u);

        Assert.That(_validator.IsValidHit(attacker, enemy, receiver), Is.True);
    }

    [Test]
    public void IsValidHit_NullReferences_ReturnsFalse()
    {
        FakeCombatant attacker = new FakeCombatant(TeamId.TeamOne);
        FakeCombatant enemy = new FakeCombatant(TeamId.TeamTwo);
        FakeDamageReceiver receiver = new FakeDamageReceiver(isAlive: true);

        Assert.That(_validator.IsValidHit(null, enemy, receiver), Is.False);
        Assert.That(_validator.IsValidHit(attacker, null, receiver), Is.False);
        Assert.That(_validator.IsValidHit(attacker, enemy, null), Is.False);
    }

    private sealed class FakeCombatant : ICombatant
    {
        public FakeCombatant(TeamId team, bool isTargetable = true)
        {
            TeamMember = new FakeTeamMember(team);
            IsTargetable = isTargetable;
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

    private sealed class FakeDamageReceiver : IDamageReceiver
    {
        public FakeDamageReceiver(bool isAlive)
        {
            IsAlive = isAlive;
        }

        public bool IsAlive { get; }

        public DamageResult ReceiveDamage(in DamageRequest request)
        {
            return default;
        }
    }
}
