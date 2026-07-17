using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public sealed class BasicAttackRuntimeTests
{
    [Test]
    public void TryBeginAttack_WhenReady_EntersActiveAndLocksMovement()
    {
        FakeMovementLock movementLock = new FakeMovementLock();
        FakeAttackHitbox hitbox = new FakeAttackHitbox();
        BasicAttackRuntime runtime = CreateRuntime(
            activeTime: 0.2f,
            cooldown: 0.5f,
            hitbox: hitbox,
            setAttackMovementLock: movementLock.SetLocked);

        bool began = runtime.TryBeginAttack();

        Assert.That(began, Is.True);
        Assert.That(runtime.Phase, Is.EqualTo(BasicAttackPhase.Active));
        Assert.That(hitbox.IsEnabled, Is.True);
        Assert.That(hitbox.ScanCount, Is.EqualTo(1));
        Assert.That(movementLock.IsLocked, Is.True);
    }

    [Test]
    public void TryBeginAttack_WhileActive_ReturnsFalse()
    {
        BasicAttackRuntime runtime = CreateRuntime(activeTime: 0.2f, cooldown: 0.5f);

        Assert.That(runtime.TryBeginAttack(), Is.True);
        Assert.That(runtime.TryBeginAttack(), Is.False);
    }

    [Test]
    public void Tick_ActiveWindowEnds_EntersCooldownAndUnlocksMovement()
    {
        FakeMovementLock movementLock = new FakeMovementLock();
        FakeAttackHitbox hitbox = new FakeAttackHitbox();
        BasicAttackRuntime runtime = CreateRuntime(
            activeTime: 0.2f,
            cooldown: 0.5f,
            hitbox: hitbox,
            setAttackMovementLock: movementLock.SetLocked);

        runtime.TryBeginAttack();
        runtime.Tick(0.2f);

        Assert.That(runtime.Phase, Is.EqualTo(BasicAttackPhase.Cooldown));
        Assert.That(hitbox.IsEnabled, Is.False);
        Assert.That(movementLock.IsLocked, Is.False);
    }

    [Test]
    public void Tick_CooldownCompletes_ReturnsToReady()
    {
        BasicAttackRuntime runtime = CreateRuntime(activeTime: 0.1f, cooldown: 0.3f);

        runtime.TryBeginAttack();
        runtime.Tick(0.1f);
        Assert.That(runtime.Phase, Is.EqualTo(BasicAttackPhase.Cooldown));

        runtime.Tick(0.3f);
        Assert.That(runtime.Phase, Is.EqualTo(BasicAttackPhase.Ready));
        Assert.That(runtime.TryBeginAttack(), Is.True);
    }

    [Test]
    public void TryBeginAttack_DuringCooldown_ReturnsFalse()
    {
        BasicAttackRuntime runtime = CreateRuntime(activeTime: 0.1f, cooldown: 0.5f);

        runtime.TryBeginAttack();
        runtime.Tick(0.1f);

        Assert.That(runtime.Phase, Is.EqualTo(BasicAttackPhase.Cooldown));
        Assert.That(runtime.TryBeginAttack(), Is.False);
    }

    [Test]
    public void TryBeginAttack_WhenDead_ReturnsFalse()
    {
        bool isAlive = false;
        BasicAttackRuntime runtime = CreateRuntime(
            activeTime: 0.2f,
            cooldown: 0.5f,
            isAlive: () => isAlive);

        Assert.That(runtime.TryBeginAttack(), Is.False);
    }

    [Test]
    public void Cancel_WhileActive_DisablesHitboxAndReturnsToReady()
    {
        FakeAttackHitbox hitbox = new FakeAttackHitbox();
        BasicAttackRuntime runtime = CreateRuntime(
            activeTime: 0.5f,
            cooldown: 0.5f,
            hitbox: hitbox);

        runtime.TryBeginAttack();
        runtime.Cancel();

        Assert.That(runtime.Phase, Is.EqualTo(BasicAttackPhase.Ready));
        Assert.That(hitbox.IsEnabled, Is.False);
    }

    [Test]
    public void HandleHitCandidate_ValidEnemy_AppliesDamageOncePerExecution()
    {
        FakeCombatant attacker = new FakeCombatant(TeamId.TeamOne);
        FakeCombatant defender = new FakeCombatant(TeamId.TeamTwo);
        RecordingDamageReceiver receiver = new RecordingDamageReceiver();
        AttackExecutionTracker tracker = new AttackExecutionTracker();
        BasicAttackRuntime runtime = CreateRuntime(
            activeTime: 0.5f,
            cooldown: 0.5f,
            attacker: attacker,
            tracker: tracker,
            damage: 10,
            criticalChance: 0f);

        runtime.TryBeginAttack();
        runtime.HandleHitCandidate(receiver, defender, Vector3.one);
        runtime.HandleHitCandidate(receiver, defender, Vector3.one);

        Assert.That(receiver.Requests.Count, Is.EqualTo(1));
        Assert.That(receiver.Requests[0].RawDamage, Is.EqualTo(10));
        Assert.That(receiver.Requests[0].IsCritical, Is.False);
        Assert.That(receiver.Requests[0].AttackExecutionId, Is.EqualTo(runtime.CurrentExecutionId));
        Assert.That(receiver.Requests[0].Attacker, Is.SameAs(attacker));
    }

    [Test]
    public void HandleHitCandidate_NewExecution_CanDamageSameTargetAgain()
    {
        FakeCombatant attacker = new FakeCombatant(TeamId.TeamOne);
        FakeCombatant defender = new FakeCombatant(TeamId.TeamTwo);
        RecordingDamageReceiver receiver = new RecordingDamageReceiver();
        BasicAttackRuntime runtime = CreateRuntime(
            activeTime: 0.1f,
            cooldown: 0.1f,
            attacker: attacker,
            damage: 10,
            criticalChance: 0f);

        runtime.TryBeginAttack();
        runtime.HandleHitCandidate(receiver, defender, Vector3.zero);
        runtime.Tick(0.1f);
        runtime.Tick(0.1f);

        runtime.TryBeginAttack();
        runtime.HandleHitCandidate(receiver, defender, Vector3.zero);

        Assert.That(receiver.Requests.Count, Is.EqualTo(2));
        Assert.That(receiver.Requests[0].AttackExecutionId, Is.Not.EqualTo(receiver.Requests[1].AttackExecutionId));
    }

    [Test]
    public void HandleHitCandidate_Ally_IsRejected()
    {
        FakeCombatant attacker = new FakeCombatant(TeamId.TeamOne);
        FakeCombatant ally = new FakeCombatant(TeamId.TeamOne);
        RecordingDamageReceiver receiver = new RecordingDamageReceiver();
        BasicAttackRuntime runtime = CreateRuntime(
            activeTime: 0.5f,
            cooldown: 0.5f,
            attacker: attacker);

        runtime.TryBeginAttack();
        runtime.HandleHitCandidate(receiver, ally, Vector3.zero);

        Assert.That(receiver.Requests, Is.Empty);
    }

    [Test]
    public void HandleHitCandidate_CriticalRoll_UsesFixedRandomSource()
    {
        FakeCombatant attacker = new FakeCombatant(TeamId.TeamOne);
        FakeCombatant defender = new FakeCombatant(TeamId.TeamTwo);
        RecordingDamageReceiver receiver = new RecordingDamageReceiver();
        BasicAttackRuntime runtime = CreateRuntime(
            activeTime: 0.5f,
            cooldown: 0.5f,
            attacker: attacker,
            damage: 10,
            criticalChance: 0.25f,
            criticalDamageMultiplier: 2f,
            random: new FixedRandomSource(0.2f));

        runtime.TryBeginAttack();
        runtime.HandleHitCandidate(receiver, defender, Vector3.zero);

        Assert.That(receiver.Requests.Count, Is.EqualTo(1));
        Assert.That(receiver.Requests[0].IsCritical, Is.True);
        Assert.That(receiver.Requests[0].RawDamage, Is.EqualTo(20));
    }

    [Test]
    public void HandleHitCandidate_NonCriticalRoll_UsesFixedRandomSource()
    {
        FakeCombatant attacker = new FakeCombatant(TeamId.TeamOne);
        FakeCombatant defender = new FakeCombatant(TeamId.TeamTwo);
        RecordingDamageReceiver receiver = new RecordingDamageReceiver();
        BasicAttackRuntime runtime = CreateRuntime(
            activeTime: 0.5f,
            cooldown: 0.5f,
            attacker: attacker,
            damage: 10,
            criticalChance: 0.25f,
            criticalDamageMultiplier: 2f,
            random: new FixedRandomSource(0.25f));

        runtime.TryBeginAttack();
        runtime.HandleHitCandidate(receiver, defender, Vector3.zero);

        Assert.That(receiver.Requests.Count, Is.EqualTo(1));
        Assert.That(receiver.Requests[0].IsCritical, Is.False);
        Assert.That(receiver.Requests[0].RawDamage, Is.EqualTo(10));
    }

    [Test]
    public void HandleHitCandidate_OutsideActive_DoesNotApplyDamage()
    {
        FakeCombatant attacker = new FakeCombatant(TeamId.TeamOne);
        FakeCombatant defender = new FakeCombatant(TeamId.TeamTwo);
        RecordingDamageReceiver receiver = new RecordingDamageReceiver();
        BasicAttackRuntime runtime = CreateRuntime(
            activeTime: 0.1f,
            cooldown: 0.5f,
            attacker: attacker);

        runtime.TryBeginAttack();
        runtime.Tick(0.1f);
        runtime.HandleHitCandidate(receiver, defender, Vector3.zero);

        Assert.That(receiver.Requests, Is.Empty);
    }

    private static BasicAttackRuntime CreateRuntime(
        float activeTime,
        float cooldown,
        ICombatant attacker = null,
        Func<bool> isAlive = null,
        IAttackHitbox hitbox = null,
        IRandomSource random = null,
        AttackExecutionTracker tracker = null,
        int damage = 10,
        float criticalChance = 0f,
        float criticalDamageMultiplier = 1.5f,
        Action<bool> setAttackMovementLock = null)
    {
        attacker ??= new FakeCombatant(TeamId.TeamOne);
        tracker ??= new AttackExecutionTracker();
        AttackHitValidator validator = new AttackHitValidator(
            new DifferentTeamRelationshipService(),
            tracker);

        return new BasicAttackRuntime(
            new CombatConfiguration.BasicAttackSettings(
                damage,
                criticalChance,
                criticalDamageMultiplier,
                cooldown,
                activeTime),
            attacker,
            isAlive ?? (() => true),
            hitbox ?? new FakeAttackHitbox(),
            random ?? new FixedRandomSource(0.99f),
            tracker,
            validator,
            setAttackMovementLock ?? (_ => { }));
    }

    private sealed class FakeAttackHitbox : IAttackHitbox
    {
        public bool IsEnabled { get; private set; }

        public int ScanCount { get; private set; }

        public void SetEnabled(bool enabled)
        {
            IsEnabled = enabled;
        }

        public void ScanInitialOverlaps()
        {
            ScanCount++;
        }
    }

    private sealed class FakeMovementLock
    {
        public bool IsLocked { get; private set; }

        public void SetLocked(bool isLocked)
        {
            IsLocked = isLocked;
        }
    }

    private sealed class FakeCombatant : ICombatant
    {
        public FakeCombatant(TeamId team)
        {
            TeamMember = new FakeTeamMember(team);
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

    private sealed class RecordingDamageReceiver : IDamageReceiver
    {
        public List<DamageRequest> Requests { get; } = new();

        public bool IsAlive => true;

        public DamageResult ReceiveDamage(in DamageRequest request)
        {
            Requests.Add(request);
            return new DamageResult(
                wasApplied: true,
                rawDamage: request.RawDamage,
                normalDefenseApplied: 0,
                criticalDefenseApplied: 0,
                finalDamage: request.RawDamage,
                remainingHealth: 100,
                wasCritical: request.IsCritical,
                wasLethal: false);
        }
    }
}
