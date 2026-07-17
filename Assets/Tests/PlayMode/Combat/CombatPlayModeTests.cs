using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class CombatPlayModeTests
{
    private CombatPlayModeTestFixture _fixture;

    [SetUp]
    public void SetUp()
    {
        _fixture = new CombatPlayModeTestFixture();
    }

    [TearDown]
    public void TearDown()
    {
        _fixture?.TearDown();
        _fixture = null;
    }

    [UnityTest]
    public IEnumerator ActivatingHitbox_DamagesOverlappingEnemy()
    {
        CharacterCombat attacker = _fixture.CreateCombatant("Attacker", TeamId.TeamOne, Vector2.zero);
        CharacterCombat defender = _fixture.CreateCombatant("Defender", TeamId.TeamTwo, new Vector2(5f, 0f));
        CombatPlayModeTestFixture.PlaceSeparated(attacker, defender);

        Assert.That(attacker.TryBasicAttack(), Is.True);
        Assert.That(attacker.BasicAttackHitbox.enabled, Is.True);
        Assert.That(defender.CurrentHealth, Is.EqualTo(defender.MaxHealth));

        defender.transform.position = new Vector3(0.75f, 0f, 0f);
        Physics2D.SyncTransforms();

        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();

        Assert.That(defender.CurrentHealth, Is.LessThan(defender.MaxHealth));
        Assert.That(defender.IsAlive, Is.True);
    }

    [UnityTest]
    public IEnumerator AlreadyOverlappingTarget_IsDetectedWhenHitboxActivates()
    {
        CharacterCombat attacker = _fixture.CreateCombatant("AttackerOverlap", TeamId.TeamOne, Vector2.zero);
        CharacterCombat defender = _fixture.CreateCombatant(
            "DefenderOverlap",
            TeamId.TeamTwo,
            new Vector2(0.75f, 0f));
        CombatPlayModeTestFixture.PlaceOverlapping(attacker, defender);

        int damageAppliedCount = 0;
        defender.DamageApplied += _ => damageAppliedCount++;

        Assert.That(attacker.TryBasicAttack(), Is.True);

        yield return new WaitForFixedUpdate();

        Assert.That(damageAppliedCount, Is.EqualTo(1));
        Assert.That(defender.CurrentHealth, Is.EqualTo(defender.MaxHealth - 10));
    }

    [UnityTest]
    public IEnumerator FacingLeft_DamagesOverlappingEnemyOnLeft()
    {
        CharacterCombat attacker = _fixture.CreateCombatant("LeftFacingAttacker", TeamId.TeamOne, Vector2.zero);
        CharacterCombat defender = _fixture.CreateCombatant(
            "LeftSideDefender",
            TeamId.TeamTwo,
            new Vector2(-0.75f, 0f));
        CombatPlayModeTestFixture.PlaceOverlappingLeft(attacker, defender);
        CombatPlayModeTestFixture.FaceDirection(attacker, Vector2.left);

        int damageAppliedCount = 0;
        defender.DamageApplied += _ => damageAppliedCount++;

        Assert.That(attacker.TryBasicAttack(), Is.True);
        Assert.That(attacker.BasicAttackHitbox.transform.localPosition.x, Is.LessThan(0f));

        yield return new WaitForFixedUpdate();

        Assert.That(damageAppliedCount, Is.EqualTo(1));
        Assert.That(defender.CurrentHealth, Is.EqualTo(defender.MaxHealth - 10));
    }

    [UnityTest]
    public IEnumerator FacingRight_DamagesOverlappingEnemyOnRight()
    {
        CharacterCombat attacker = _fixture.CreateCombatant("RightFacingAttacker", TeamId.TeamOne, Vector2.zero);
        CharacterCombat defender = _fixture.CreateCombatant(
            "RightSideDefender",
            TeamId.TeamTwo,
            new Vector2(0.75f, 0f));
        CombatPlayModeTestFixture.PlaceOverlapping(attacker, defender);
        CombatPlayModeTestFixture.FaceDirection(attacker, Vector2.right);

        int damageAppliedCount = 0;
        defender.DamageApplied += _ => damageAppliedCount++;

        Assert.That(attacker.TryBasicAttack(), Is.True);
        Assert.That(attacker.BasicAttackHitbox.transform.localPosition.x, Is.GreaterThan(0f));

        yield return new WaitForFixedUpdate();

        Assert.That(damageAppliedCount, Is.EqualTo(1));
        Assert.That(defender.CurrentHealth, Is.EqualTo(defender.MaxHealth - 10));
    }

    [UnityTest]
    public IEnumerator FacingLeft_DoesNotDamageEnemyOnRight()
    {
        CharacterCombat attacker = _fixture.CreateCombatant("MissRightAttacker", TeamId.TeamOne, Vector2.zero);
        CharacterCombat defender = _fixture.CreateCombatant(
            "RightSideMissDefender",
            TeamId.TeamTwo,
            new Vector2(0.75f, 0f));
        CombatPlayModeTestFixture.PlaceOverlapping(attacker, defender);
        CombatPlayModeTestFixture.FaceDirection(attacker, Vector2.left);

        int damageAppliedCount = 0;
        defender.DamageApplied += _ => damageAppliedCount++;

        Assert.That(attacker.TryBasicAttack(), Is.True);
        Assert.That(attacker.BasicAttackHitbox.transform.localPosition.x, Is.LessThan(0f));

        yield return new WaitForFixedUpdate();

        Assert.That(damageAppliedCount, Is.Zero);
        Assert.That(defender.CurrentHealth, Is.EqualTo(defender.MaxHealth));
    }

    [UnityTest]
    public IEnumerator AlliedHurtbox_IsIgnored()
    {
        CharacterCombat attacker = _fixture.CreateCombatant("AllyAttacker", TeamId.TeamOne, Vector2.zero);
        CharacterCombat ally = _fixture.CreateCombatant("AllyDefender", TeamId.TeamOne, new Vector2(0.75f, 0f));
        CombatPlayModeTestFixture.PlaceOverlapping(attacker, ally);

        int damageAppliedCount = 0;
        ally.DamageApplied += _ => damageAppliedCount++;

        Assert.That(attacker.TryBasicAttack(), Is.True);

        yield return new WaitForFixedUpdate();

        Assert.That(damageAppliedCount, Is.Zero);
        Assert.That(ally.CurrentHealth, Is.EqualTo(ally.MaxHealth));
    }

    [UnityTest]
    public IEnumerator Hitbox_TurnsOffAfterActiveWindow()
    {
        CharacterCombat attacker = _fixture.CreateCombatant(
            "TimedAttacker",
            TeamId.TeamOne,
            Vector2.zero,
            activeTime: 0.1f,
            cooldown: 0.5f);

        Assert.That(attacker.TryBasicAttack(), Is.True);
        Assert.That(attacker.BasicAttackHitbox.enabled, Is.True);
        Assert.That(attacker.AttackPhase, Is.EqualTo(BasicAttackPhase.Active));

        yield return new WaitForSeconds(0.15f);

        Assert.That(attacker.BasicAttackHitbox.enabled, Is.False);
        Assert.That(attacker.AttackPhase, Is.EqualTo(BasicAttackPhase.Cooldown));
    }

    [UnityTest]
    public IEnumerator Cooldown_PreventsAttackSpam()
    {
        CharacterCombat attacker = _fixture.CreateCombatant(
            "CooldownAttacker",
            TeamId.TeamOne,
            Vector2.zero,
            activeTime: 0.05f,
            cooldown: 0.2f);

        Assert.That(attacker.TryBasicAttack(), Is.True);
        Assert.That(attacker.TryBasicAttack(), Is.False);

        yield return new WaitForSeconds(0.08f);

        Assert.That(attacker.AttackPhase, Is.EqualTo(BasicAttackPhase.Cooldown));
        Assert.That(attacker.TryBasicAttack(), Is.False);

        yield return new WaitForSeconds(0.2f);

        Assert.That(attacker.AttackPhase, Is.EqualTo(BasicAttackPhase.Ready));
        Assert.That(attacker.TryBasicAttack(), Is.True);
    }

    [UnityTest]
    public IEnumerator Death_CancelsActiveHitboxAndDisablesMovementAndTargeting()
    {
        CharacterCombat attacker = _fixture.CreateCombatant(
            "DyingAttacker",
            TeamId.TeamOne,
            Vector2.zero,
            maxHealth: 5,
            activeTime: 1f,
            cooldown: 1f);
        RegisteredCombatant registeredCombatant = attacker.RegisteredCombatant;
        ConfigurableMovementGate movementGate = attacker.GetComponent<CombatCharacter>().MovementGate;

        Assert.That(attacker.TryBasicAttack(), Is.True);
        Assert.That(attacker.BasicAttackHitbox.enabled, Is.True);
        Assert.That(registeredCombatant.IsTargetable, Is.True);
        Assert.That(movementGate.CanMove, Is.False);

        attacker.ReceiveDamage(new DamageRequest(
            attacker.RegisteredCombatant,
            99u,
            rawDamage: 5,
            isCritical: false,
            hitPoint: Vector3.zero));

        yield return new WaitForFixedUpdate();

        Assert.That(attacker.IsAlive, Is.False);
        Assert.That(attacker.BasicAttackHitbox.enabled, Is.False);
        Assert.That(attacker.AttackPhase, Is.EqualTo(BasicAttackPhase.Ready));
        Assert.That(registeredCombatant.IsTargetable, Is.False);
        Assert.That(movementGate.CanMove, Is.False);
    }

    [UnityTest]
    public IEnumerator TwoAttackers_SamePhysicsStep_BothDamageApplied()
    {
        CharacterCombat attackerA = _fixture.CreateCombatant(
            "AttackerA",
            TeamId.TeamOne,
            new Vector2(-0.75f, 0f),
            damage: 10,
            criticalChance: 0f,
            hitboxLocalOffset: new Vector2(0.75f, 0f));
        CharacterCombat attackerB = _fixture.CreateCombatant(
            "AttackerB",
            TeamId.TeamOne,
            new Vector2(0.75f, 0f),
            damage: 10,
            criticalChance: 0f,
            hitboxLocalOffset: new Vector2(-0.75f, 0f));
        CharacterCombat defender = _fixture.CreateCombatant(
            "SharedDefender",
            TeamId.TeamTwo,
            Vector2.zero,
            maxHealth: 30,
            defense: 0);

        attackerA.transform.position = new Vector3(-0.75f, 0f, 0f);
        attackerB.transform.position = new Vector3(0.75f, 0f, 0f);
        defender.transform.position = Vector3.zero;
        Physics2D.SyncTransforms();

        CombatPlayModeTestFixture.FaceDirection(attackerA, Vector2.right);
        CombatPlayModeTestFixture.FaceDirection(attackerB, Vector2.left);

        List<DamageResult> appliedResults = new();
        defender.DamageApplied += result => appliedResults.Add(result);

        Assert.That(attackerA.TryBasicAttack(), Is.True);
        Assert.That(attackerB.TryBasicAttack(), Is.True);

        yield return new WaitForFixedUpdate();

        Assert.That(appliedResults.Count, Is.EqualTo(2));
        foreach (DamageResult result in appliedResults)
        {
            Assert.That(result.WasApplied, Is.True);
            Assert.That(result.FinalDamage, Is.EqualTo(10));
        }

        Assert.That(defender.CurrentHealth, Is.EqualTo(10));
        Assert.That(defender.IsAlive, Is.True);
    }
}
