using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class CharacterCombatCompositionTests
{
    private readonly List<GameObject> _createdObjects = new();

    [TearDown]
    public void TearDown()
    {
        foreach (GameObject createdObject in _createdObjects)
        {
            if (createdObject != null)
            {
                UnityEngine.Object.DestroyImmediate(createdObject);
            }
        }

        _createdObjects.Clear();
    }

    [Test]
    public void CombatConfiguration_Clamp_NegativeAndOutOfRangeStats_BecomeSafe()
    {
        CombatConfiguration configuration = new CombatConfiguration(
            new CombatConfiguration.VitalitySettings(-10),
            new CombatConfiguration.DefenseSettings(-3, -4),
            new CombatConfiguration.BasicAttackSettings(
                damage: -5,
                criticalChance: 1.5f,
                criticalDamageMultiplier: -2f,
                cooldown: -1f,
                activeTime: -0.5f));

        Assert.That(configuration.Vitality.MaxHealth, Is.Zero);
        Assert.That(configuration.Defense.Defense, Is.Zero);
        Assert.That(configuration.Defense.CriticalDefense, Is.Zero);
        Assert.That(configuration.BasicAttack.Damage, Is.Zero);
        Assert.That(configuration.BasicAttack.CriticalChance, Is.EqualTo(1f));
        Assert.That(configuration.BasicAttack.CriticalDamageMultiplier, Is.Zero);
        Assert.That(configuration.BasicAttack.Cooldown, Is.Zero);
        Assert.That(configuration.BasicAttack.ActiveTime, Is.Zero);
    }

    [Test]
    public void CombatConfiguration_Clamp_CriticalChanceBelowZero_BecomesZero()
    {
        CombatConfiguration configuration = new CombatConfiguration(
            new CombatConfiguration.VitalitySettings(30),
            new CombatConfiguration.DefenseSettings(0, 0),
            new CombatConfiguration.BasicAttackSettings(
                damage: 10,
                criticalChance: -0.2f,
                criticalDamageMultiplier: 1.5f,
                cooldown: 0.5f,
                activeTime: 0.2f));

        Assert.That(configuration.BasicAttack.CriticalChance, Is.Zero);
    }

    [Test]
    public void Awake_ConstructsRuntimeObjectsFromConfiguration()
    {
        CharacterCombat combat = CreateCombat(
            maxHealth: 40,
            defense: 2,
            criticalDefense: 1);

        Assert.That(combat, Is.InstanceOf<IDamageReceiver>());
        Assert.That(combat, Is.InstanceOf<ICombatVitality>());
        Assert.That(combat, Is.InstanceOf<IBasicAttackRequester>());
        Assert.That(combat.IsAlive, Is.True);
        Assert.That(((ICombatVitality)combat).IsAlive, Is.True);
        Assert.That(((IDamageReceiver)combat).IsAlive, Is.True);
    }

    [Test]
    public void Awake_NegativeMaxHealth_IsAliveIsFalse()
    {
        CharacterCombat combat = CreateCombat(
            maxHealth: -8,
            defense: 0,
            criticalDefense: 0);

        Assert.That(combat.IsAlive, Is.False);
    }

    [Test]
    public void TryBasicAttack_IsStubbedFalseUntilTask7()
    {
        CharacterCombat combat = CreateCombat(
            maxHealth: 30,
            defense: 0,
            criticalDefense: 0);

        Assert.That(combat.TryBasicAttack(), Is.False);
        Assert.That(((IBasicAttackRequester)combat).TryBasicAttack(), Is.False);
    }

    [Test]
    public void ReceiveDamage_QueuesThenResolvesThroughFacadeOnFixedUpdate()
    {
        CharacterCombat combat = CreateCombat(
            maxHealth: 30,
            defense: 3,
            criticalDefense: 0);
        FakeCombatant attacker = new FakeCombatant();
        int healthChangedCurrent = -1;
        int healthChangedMax = -1;
        int diedCount = 0;
        int damageAppliedCount = 0;
        DamageResult appliedResult = default;
        combat.HealthChanged += (current, max) =>
        {
            healthChangedCurrent = current;
            healthChangedMax = max;
        };
        combat.Died += () => diedCount++;
        combat.DamageApplied += result =>
        {
            damageAppliedCount++;
            appliedResult = result;
        };

        DamageResult queued = combat.ReceiveDamage(
            new DamageRequest(attacker, 1u, 10, false, Vector3.zero));

        Assert.That(queued.WasApplied, Is.True);
        Assert.That(queued.FinalDamage, Is.Zero);
        Assert.That(combat.IsAlive, Is.True);

        SimulateFixedUpdate(combat);

        Assert.That(healthChangedCurrent, Is.EqualTo(23));
        Assert.That(healthChangedMax, Is.EqualTo(30));
        Assert.That(diedCount, Is.Zero);
        Assert.That(damageAppliedCount, Is.EqualTo(1));
        Assert.That(appliedResult.WasApplied, Is.True);
        Assert.That(appliedResult.FinalDamage, Is.EqualTo(7));
        Assert.That(appliedResult.RemainingHealth, Is.EqualTo(23));
        Assert.That(combat.IsAlive, Is.True);
    }

    [Test]
    public void ReceiveDamage_LethalBatch_ForwardsDiedEvent()
    {
        CharacterCombat combat = CreateCombat(
            maxHealth: 10,
            defense: 0,
            criticalDefense: 0);
        FakeCombatant attacker = new FakeCombatant();
        int diedCount = 0;
        combat.Died += () => diedCount++;

        combat.ReceiveDamage(new DamageRequest(attacker, 1u, 10, false, Vector3.zero));
        SimulateFixedUpdate(combat);

        Assert.That(diedCount, Is.EqualTo(1));
        Assert.That(combat.IsAlive, Is.False);
    }

    private CharacterCombat CreateCombat(int maxHealth, int defense, int criticalDefense)
    {
        GameObject gameObject = new GameObject("CharacterCombatCompositionTest");
        gameObject.SetActive(false);
        _createdObjects.Add(gameObject);

        CharacterCombat combat = gameObject.AddComponent<CharacterCombat>();

        SerializedObject serializedObject = new SerializedObject(combat);
        SerializedProperty configuration = serializedObject.FindProperty("_configuration");
        configuration.FindPropertyRelative("_vitality").FindPropertyRelative("_maxHealth").intValue = maxHealth;
        configuration.FindPropertyRelative("_defense").FindPropertyRelative("_defense").intValue = defense;
        configuration.FindPropertyRelative("_defense").FindPropertyRelative("_criticalDefense").intValue = criticalDefense;
        configuration.FindPropertyRelative("_basicAttack").FindPropertyRelative("_damage").intValue = 10;
        configuration.FindPropertyRelative("_basicAttack").FindPropertyRelative("_criticalChance").floatValue = 0.1f;
        configuration.FindPropertyRelative("_basicAttack").FindPropertyRelative("_criticalDamageMultiplier").floatValue = 1.5f;
        configuration.FindPropertyRelative("_basicAttack").FindPropertyRelative("_cooldown").floatValue = 0.5f;
        configuration.FindPropertyRelative("_basicAttack").FindPropertyRelative("_activeTime").floatValue = 0.2f;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();

        gameObject.SetActive(true);
        return combat;
    }

    private static void SimulateFixedUpdate(CharacterCombat combat)
    {
        MethodInfo fixedUpdate = typeof(CharacterCombat).GetMethod(
            "FixedUpdate",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(fixedUpdate, Is.Not.Null);
        fixedUpdate.Invoke(combat, null);
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
