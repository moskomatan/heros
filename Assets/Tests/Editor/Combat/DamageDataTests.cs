using System;
using NUnit.Framework;
using UnityEngine;

public sealed class DamageDataTests
{
    [Test]
    public void DamageRequest_StoresProvidedValues()
    {
        FakeCombatant attacker = new FakeCombatant();
        Vector3 hitPoint = new Vector3(1f, 2f, 3f);

        DamageRequest request = new DamageRequest(attacker, 42u, 10, true, hitPoint);

        Assert.That(request.Attacker, Is.SameAs(attacker));
        Assert.That(request.AttackExecutionId, Is.EqualTo(42u));
        Assert.That(request.RawDamage, Is.EqualTo(10));
        Assert.That(request.IsCritical, Is.True);
        Assert.That(request.HitPoint, Is.EqualTo(hitPoint));
    }

    [Test]
    public void DamageRequest_ZeroRawDamage_IsAllowed()
    {
        FakeCombatant attacker = new FakeCombatant();

        DamageRequest request = new DamageRequest(attacker, 1u, 0, false, Vector3.zero);

        Assert.That(request.RawDamage, Is.Zero);
    }

    [Test]
    public void DamageRequest_NegativeRawDamage_Throws()
    {
        FakeCombatant attacker = new FakeCombatant();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new DamageRequest(attacker, 1u, -1, false, Vector3.zero));
    }

    [Test]
    public void DamageResult_StoresProvidedValues()
    {
        DamageResult result = new DamageResult(
            wasApplied: true,
            rawDamage: 20,
            normalDefenseApplied: 3,
            criticalDefenseApplied: 4,
            finalDamage: 13,
            remainingHealth: 17,
            wasCritical: true,
            wasLethal: false);

        Assert.That(result.WasApplied, Is.True);
        Assert.That(result.RawDamage, Is.EqualTo(20));
        Assert.That(result.NormalDefenseApplied, Is.EqualTo(3));
        Assert.That(result.CriticalDefenseApplied, Is.EqualTo(4));
        Assert.That(result.FinalDamage, Is.EqualTo(13));
        Assert.That(result.RemainingHealth, Is.EqualTo(17));
        Assert.That(result.WasCritical, Is.True);
        Assert.That(result.WasLethal, Is.False);
    }

    [Test]
    public void MitigationResult_StoresProvidedValues()
    {
        MitigationResult result = new MitigationResult(
            normalDefenseApplied: 3,
            criticalDefenseApplied: 4,
            finalDamage: 13);

        Assert.That(result.NormalDefenseApplied, Is.EqualTo(3));
        Assert.That(result.CriticalDefenseApplied, Is.EqualTo(4));
        Assert.That(result.FinalDamage, Is.EqualTo(13));
    }

    [Test]
    public void IDamageReceiver_ReceiveDamage_ReturnsConfiguredResult()
    {
        FakeDamageReceiver receiver = new FakeDamageReceiver(isAlive: true);
        DamageRequest request = new DamageRequest(new FakeCombatant(), 1u, 10, false, Vector3.zero);

        DamageResult result = receiver.ReceiveDamage(in request);

        Assert.That(result.WasApplied, Is.True);
        Assert.That(result.FinalDamage, Is.EqualTo(7));
        Assert.That(receiver.IsAlive, Is.True);
    }

    [Test]
    public void IDamageMitigation_Mitigate_ReturnsConfiguredResult()
    {
        FakeDamageMitigation mitigation = new FakeDamageMitigation();
        DamageRequest request = new DamageRequest(new FakeCombatant(), 1u, 10, true, Vector3.zero);

        MitigationResult result = mitigation.Mitigate(in request);

        Assert.That(result.NormalDefenseApplied, Is.EqualTo(3));
        Assert.That(result.CriticalDefenseApplied, Is.EqualTo(4));
        Assert.That(result.FinalDamage, Is.EqualTo(13));
    }

    [Test]
    public void ICombatVitality_ExposesAliveState()
    {
        FakeCombatVitality vitality = new FakeCombatVitality(isAlive: false);

        Assert.That(vitality.IsAlive, Is.False);
    }

    [Test]
    public void IBasicAttackRequester_TryBasicAttack_ReturnsConfiguredResult()
    {
        FakeBasicAttackRequester requester = new FakeBasicAttackRequester(canAttack: true);

        Assert.That(requester.TryBasicAttack(), Is.True);
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

    private sealed class FakeDamageReceiver : IDamageReceiver
    {
        public FakeDamageReceiver(bool isAlive)
        {
            IsAlive = isAlive;
        }

        public bool IsAlive { get; }

        public DamageResult ReceiveDamage(in DamageRequest request)
        {
            return new DamageResult(
                wasApplied: true,
                rawDamage: request.RawDamage,
                normalDefenseApplied: 3,
                criticalDefenseApplied: 0,
                finalDamage: 7,
                remainingHealth: 23,
                wasCritical: request.IsCritical,
                wasLethal: false);
        }
    }

    private sealed class FakeDamageMitigation : IDamageMitigation
    {
        public MitigationResult Mitigate(in DamageRequest request)
        {
            return new MitigationResult(
                normalDefenseApplied: 3,
                criticalDefenseApplied: request.IsCritical ? 4 : 0,
                finalDamage: request.IsCritical ? 13 : 7);
        }
    }

    private sealed class FakeCombatVitality : ICombatVitality
    {
        public FakeCombatVitality(bool isAlive)
        {
            IsAlive = isAlive;
        }

        public bool IsAlive { get; }
    }

    private sealed class FakeBasicAttackRequester : IBasicAttackRequester
    {
        public FakeBasicAttackRequester(bool canAttack)
        {
            _canAttack = canAttack;
        }

        private readonly bool _canAttack;

        public bool TryBasicAttack()
        {
            return _canAttack;
        }
    }
}
