using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public sealed class DamageReceiverBatchTests
{
    [Test]
    public void ResolvePendingBatch_TwoHitsInOneStep_SumsFinalDamage()
    {
        HealthPool healthPool = new HealthPool(30);
        DefenseResolver mitigation = new DefenseResolver(defense: 3, criticalDefense: 0);
        DamageReceiver receiver = new DamageReceiver(healthPool, mitigation);
        FakeCombatant attackerA = new FakeCombatant();
        FakeCombatant attackerB = new FakeCombatant();

        DamageResult queuedA = receiver.ReceiveDamage(new DamageRequest(attackerA, 1u, 10, false, Vector3.zero));
        DamageResult queuedB = receiver.ReceiveDamage(new DamageRequest(attackerB, 2u, 10, false, Vector3.zero));

        Assert.That(queuedA.WasApplied, Is.True);
        Assert.That(queuedB.WasApplied, Is.True);
        Assert.That(healthPool.CurrentHealth, Is.EqualTo(30));

        IReadOnlyList<DamageResult> results = receiver.ResolvePendingBatch();

        Assert.That(results.Count, Is.EqualTo(2));
        Assert.That(results[0].FinalDamage, Is.EqualTo(7));
        Assert.That(results[1].FinalDamage, Is.EqualTo(7));
        Assert.That(healthPool.CurrentHealth, Is.EqualTo(16));
        Assert.That(receiver.IsAlive, Is.True);
    }

    [Test]
    public void ResolvePendingBatch_ReversedQueueOrder_ProducesSameFinalHealth()
    {
        HealthPool healthPool = new HealthPool(30);
        DefenseResolver mitigation = new DefenseResolver(defense: 3, criticalDefense: 0);
        DamageReceiver receiver = new DamageReceiver(healthPool, mitigation);
        FakeCombatant attackerA = new FakeCombatant();
        FakeCombatant attackerB = new FakeCombatant();

        receiver.ReceiveDamage(new DamageRequest(attackerB, 2u, 12, false, Vector3.zero));
        receiver.ReceiveDamage(new DamageRequest(attackerA, 1u, 10, false, Vector3.zero));

        IReadOnlyList<DamageResult> results = receiver.ResolvePendingBatch();

        Assert.That(healthPool.CurrentHealth, Is.EqualTo(14));
        Assert.That(results[0].RawDamage, Is.EqualTo(10));
        Assert.That(results[0].FinalDamage, Is.EqualTo(7));
        Assert.That(results[1].RawDamage, Is.EqualTo(12));
        Assert.That(results[1].FinalDamage, Is.EqualTo(9));
    }

    [Test]
    public void ResolvePendingBatch_LethalBatch_ReportsEveryHitAndEmitsDeathOnce()
    {
        HealthPool healthPool = new HealthPool(10);
        DefenseResolver mitigation = new DefenseResolver(defense: 0, criticalDefense: 0);
        DamageReceiver receiver = new DamageReceiver(healthPool, mitigation);
        FakeCombatant attackerA = new FakeCombatant();
        FakeCombatant attackerB = new FakeCombatant();
        int healthChangedCount = 0;
        int diedCount = 0;
        healthPool.HealthChanged += (_, _) => healthChangedCount++;
        healthPool.Died += () => diedCount++;

        receiver.ReceiveDamage(new DamageRequest(attackerA, 1u, 6, false, Vector3.zero));
        receiver.ReceiveDamage(new DamageRequest(attackerB, 2u, 6, false, Vector3.zero));

        IReadOnlyList<DamageResult> results = receiver.ResolvePendingBatch();

        Assert.That(results.Count, Is.EqualTo(2));
        Assert.That(results[0].WasApplied, Is.True);
        Assert.That(results[1].WasApplied, Is.True);
        Assert.That(results[0].FinalDamage, Is.EqualTo(6));
        Assert.That(results[1].FinalDamage, Is.EqualTo(6));
        Assert.That(results[1].WasLethal, Is.True);
        Assert.That(healthPool.CurrentHealth, Is.Zero);
        Assert.That(receiver.IsAlive, Is.False);
        Assert.That(healthChangedCount, Is.EqualTo(1));
        Assert.That(diedCount, Is.EqualTo(1));
    }

    [Test]
    public void ResolvePendingBatch_RecursiveDamageFromHealthChanged_IsDeferredToNextBatch()
    {
        HealthPool healthPool = new HealthPool(20);
        DefenseResolver mitigation = new DefenseResolver(defense: 0, criticalDefense: 0);
        DamageReceiver receiver = new DamageReceiver(healthPool, mitigation);
        FakeCombatant primaryAttacker = new FakeCombatant();
        FakeCombatant recursiveAttacker = new FakeCombatant();
        DamageRequest recursiveRequest = new DamageRequest(recursiveAttacker, 99u, 5, false, Vector3.zero);

        healthPool.HealthChanged += (_, _) => receiver.ReceiveDamage(in recursiveRequest);

        receiver.ReceiveDamage(new DamageRequest(primaryAttacker, 1u, 7, false, Vector3.zero));

        IReadOnlyList<DamageResult> firstBatch = receiver.ResolvePendingBatch();
        IReadOnlyList<DamageResult> secondBatch = receiver.ResolvePendingBatch();

        Assert.That(firstBatch.Count, Is.EqualTo(1));
        Assert.That(firstBatch[0].FinalDamage, Is.EqualTo(7));
        Assert.That(healthPool.CurrentHealth, Is.EqualTo(8));

        Assert.That(secondBatch.Count, Is.EqualTo(1));
        Assert.That(secondBatch[0].FinalDamage, Is.EqualTo(5));
        Assert.That(healthPool.CurrentHealth, Is.EqualTo(3));
    }

    [Test]
    public void ReceiveDamage_AfterDeath_IsRejected()
    {
        HealthPool healthPool = new HealthPool(10);
        DefenseResolver mitigation = new DefenseResolver(defense: 0, criticalDefense: 0);
        DamageReceiver receiver = new DamageReceiver(healthPool, mitigation);
        FakeCombatant attacker = new FakeCombatant();

        receiver.ReceiveDamage(new DamageRequest(attacker, 1u, 10, false, Vector3.zero));
        receiver.ResolvePendingBatch();

        DamageResult rejected = receiver.ReceiveDamage(new DamageRequest(attacker, 2u, 5, false, Vector3.zero));

        Assert.That(rejected.WasApplied, Is.False);
        Assert.That(healthPool.CurrentHealth, Is.Zero);
        Assert.That(receiver.ResolvePendingBatch().Count, Is.Zero);
    }

    [Test]
    public void ResolvePendingBatch_RecursiveDamageFromDied_IsRejectedWhenDefenderDead()
    {
        HealthPool healthPool = new HealthPool(10);
        DefenseResolver mitigation = new DefenseResolver(defense: 0, criticalDefense: 0);
        DamageReceiver receiver = new DamageReceiver(healthPool, mitigation);
        FakeCombatant lethalAttacker = new FakeCombatant();
        FakeCombatant recursiveAttacker = new FakeCombatant();
        DamageRequest recursiveRequest = new DamageRequest(recursiveAttacker, 99u, 5, false, Vector3.zero);

        healthPool.Died += () => receiver.ReceiveDamage(in recursiveRequest);

        receiver.ReceiveDamage(new DamageRequest(lethalAttacker, 1u, 10, false, Vector3.zero));
        IReadOnlyList<DamageResult> firstBatch = receiver.ResolvePendingBatch();

        Assert.That(firstBatch.Count, Is.EqualTo(1));
        Assert.That(firstBatch[0].WasLethal, Is.True);
        Assert.That(receiver.IsAlive, Is.False);
        Assert.That(receiver.ResolvePendingBatch().Count, Is.Zero);
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
