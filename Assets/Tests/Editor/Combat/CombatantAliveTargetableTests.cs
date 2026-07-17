using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class CombatantAliveTargetableTests
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
    public void IsTargetable_UnboundVitality_RemainsTargetableWhenActive()
    {
        RegisteredCombatant combatant = CreateCombatant();

        Assert.That(combatant.IsTargetable, Is.True);
    }

    [Test]
    public void IsTargetable_BoundAliveVitality_RemainsTargetable()
    {
        RegisteredCombatant combatant = CreateCombatant();
        FakeVitality vitality = new FakeVitality { IsAlive = true };

        combatant.BindVitality(vitality);

        Assert.That(combatant.IsTargetable, Is.True);
    }

    [Test]
    public void IsTargetable_BoundDeadVitality_IsNotTargetable()
    {
        RegisteredCombatant combatant = CreateCombatant();
        FakeVitality vitality = new FakeVitality { IsAlive = false };

        combatant.BindVitality(vitality);

        Assert.That(combatant.IsTargetable, Is.False);
    }

    [Test]
    public void IsTargetable_BoundVitalityBecomesDead_IsNotTargetable()
    {
        RegisteredCombatant combatant = CreateCombatant();
        FakeVitality vitality = new FakeVitality { IsAlive = true };
        combatant.BindVitality(vitality);

        Assert.That(combatant.IsTargetable, Is.True);

        vitality.IsAlive = false;

        Assert.That(combatant.IsTargetable, Is.False);
    }

    [Test]
    public void RegisteredCombatant_DoesNotReferenceCharacterCombat()
    {
        Type registeredCombatantType = typeof(RegisteredCombatant);
        Type characterCombatType = typeof(CharacterCombat);

        IEnumerable<Type> referencedTypes = registeredCombatantType
            .GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            .Select(field => field.FieldType)
            .Concat(
                registeredCombatantType
                    .GetProperties(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                    .Select(property => property.PropertyType))
            .Concat(
                registeredCombatantType
                    .GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                    .SelectMany(method => method.GetParameters().Select(parameter => parameter.ParameterType)
                        .Append(method.ReturnType)));

        Assert.That(referencedTypes, Does.Not.Contain(characterCombatType));

        MethodInfo bindVitality = registeredCombatantType.GetMethod(
            "BindVitality",
            BindingFlags.Instance | BindingFlags.Public);
        Assert.That(bindVitality, Is.Not.Null);
        Assert.That(bindVitality.GetParameters().Length, Is.EqualTo(1));
        Assert.That(bindVitality.GetParameters()[0].ParameterType, Is.EqualTo(typeof(ICombatVitality)));
    }

    [Test]
    public void CharacterCombat_OnDeath_DisablesMovementGateAndUnbindsTargetable()
    {
        CharacterCombat combat = CreateCombatWithMovement(maxHealth: 5);
        RegisteredCombatant registeredCombatant = combat.RegisteredCombatant;
        ConfigurableMovementGate movementGate = combat.GetComponent<CombatCharacter>().MovementGate;

        Assert.That(registeredCombatant.IsTargetable, Is.True);
        Assert.That(movementGate.CanMove, Is.True);

        combat.ReceiveDamage(new DamageRequest(new FakeCombatant(), 1u, 5, false, Vector3.zero));
        SimulateFixedUpdate(combat);

        Assert.That(combat.IsAlive, Is.False);
        Assert.That(registeredCombatant.IsTargetable, Is.False);
        Assert.That(movementGate.CanMove, Is.False);
    }

    private RegisteredCombatant CreateCombatant()
    {
        GameObject gameObject = new GameObject("AliveTargetableCombatant");
        gameObject.SetActive(false);
        _createdObjects.Add(gameObject);

        KeyboardMovementInputSource input = gameObject.AddComponent<KeyboardMovementInputSource>();
        CombatCharacter character = gameObject.AddComponent<CombatCharacter>();
        RegisteredCombatant registeredCombatant = gameObject.AddComponent<RegisteredCombatant>();

        SerializedObject characterSerializedObject = new SerializedObject(character);
        characterSerializedObject.FindProperty("_inputSourceBehaviour").objectReferenceValue = input;
        characterSerializedObject.ApplyModifiedPropertiesWithoutUndo();

        registeredCombatant.Initialize(new CombatantRegistry(), TeamId.TeamOne);
        gameObject.SetActive(true);

        return registeredCombatant;
    }

    private CharacterCombat CreateCombatWithMovement(int maxHealth)
    {
        GameObject gameObject = new GameObject("AliveTargetableCombatSetup");
        gameObject.SetActive(false);
        _createdObjects.Add(gameObject);

        KeyboardMovementInputSource input = gameObject.AddComponent<KeyboardMovementInputSource>();
        CombatCharacter character = gameObject.AddComponent<CombatCharacter>();
        RegisteredCombatant registeredCombatant = gameObject.AddComponent<RegisteredCombatant>();
        CharacterCombat combat = gameObject.AddComponent<CharacterCombat>();

        SerializedObject characterSerializedObject = new SerializedObject(character);
        characterSerializedObject.FindProperty("_inputSourceBehaviour").objectReferenceValue = input;
        characterSerializedObject.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject combatSerializedObject = new SerializedObject(combat);
        SerializedProperty configuration = combatSerializedObject.FindProperty("_configuration");
        configuration.FindPropertyRelative("_vitality").FindPropertyRelative("_maxHealth").intValue = maxHealth;
        configuration.FindPropertyRelative("_defense").FindPropertyRelative("_defense").intValue = 0;
        configuration.FindPropertyRelative("_defense").FindPropertyRelative("_criticalDefense").intValue = 0;
        configuration.FindPropertyRelative("_basicAttack").FindPropertyRelative("_damage").intValue = 10;
        configuration.FindPropertyRelative("_basicAttack").FindPropertyRelative("_criticalChance").floatValue = 0.1f;
        configuration.FindPropertyRelative("_basicAttack").FindPropertyRelative("_criticalDamageMultiplier").floatValue = 1.5f;
        configuration.FindPropertyRelative("_basicAttack").FindPropertyRelative("_cooldown").floatValue = 0.5f;
        configuration.FindPropertyRelative("_basicAttack").FindPropertyRelative("_activeTime").floatValue = 0.2f;
        combatSerializedObject.FindProperty("_registeredCombatant").objectReferenceValue = registeredCombatant;
        combatSerializedObject.ApplyModifiedPropertiesWithoutUndo();

        registeredCombatant.Initialize(new CombatantRegistry(), TeamId.TeamOne);
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

    private sealed class FakeVitality : ICombatVitality
    {
        public bool IsAlive { get; set; }
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
