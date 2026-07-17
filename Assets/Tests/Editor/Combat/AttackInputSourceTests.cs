using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class AttackInputSourceTests
{
    private readonly List<GameObject> _createdObjects = new();

    [TearDown]
    public void TearDown()
    {
        foreach (GameObject createdObject in _createdObjects)
        {
            if (createdObject != null)
            {
                Object.DestroyImmediate(createdObject);
            }
        }

        _createdObjects.Clear();
    }

    [Test]
    public void UiButtonAttackInputSource_Consume_ReturnsTrueOncePerNotify()
    {
        UiButtonAttackInputSource source = CreateObject("UiAttack").AddComponent<UiButtonAttackInputSource>();

        Assert.That(source.ConsumeBasicAttackPressed(), Is.False);

        source.NotifyBasicAttackPressed();
        Assert.That(source.ConsumeBasicAttackPressed(), Is.True);
        Assert.That(source.ConsumeBasicAttackPressed(), Is.False);
    }

    [Test]
    public void UiButtonAttackInputSource_MultipleNotifies_ConsumeClearsOnce()
    {
        UiButtonAttackInputSource source = CreateObject("UiAttackMulti").AddComponent<UiButtonAttackInputSource>();

        source.NotifyBasicAttackPressed();
        source.NotifyBasicAttackPressed();

        Assert.That(source.ConsumeBasicAttackPressed(), Is.True);
        Assert.That(source.ConsumeBasicAttackPressed(), Is.False);
    }

    [Test]
    public void KeyboardAttackInputSource_WithoutKeyboard_ReturnsFalse()
    {
        KeyboardAttackInputSource source = CreateObject("KeyboardAttack").AddComponent<KeyboardAttackInputSource>();

        Assert.That(source.ConsumeBasicAttackPressed(), Is.False);
    }

    [Test]
    public void BotAttackInputSource_TargetWithinRange_ReturnsTrueEachConsume()
    {
        GameObject botObject = CreateObject("BotAttacker");
        BotMovementInputSource chase = botObject.AddComponent<BotMovementInputSource>();
        BotAttackInputSource source = botObject.AddComponent<BotAttackInputSource>();

        GameObject targetObject = CreateObject("ChaseTarget");
        targetObject.transform.position = new Vector3(1f, 0f, 0f);
        chase.Target = targetObject.transform;

        SerializedObject serializedSource = new SerializedObject(source);
        serializedSource.FindProperty("_attackRange").floatValue = 1.5f;
        serializedSource.FindProperty("_chaseTargetSource").objectReferenceValue = chase;
        serializedSource.ApplyModifiedPropertiesWithoutUndo();

        Assert.That(source.ConsumeBasicAttackPressed(), Is.True);
        Assert.That(source.ConsumeBasicAttackPressed(), Is.True);
    }

    [Test]
    public void BotAttackInputSource_TargetOutsideRange_ReturnsFalse()
    {
        GameObject botObject = CreateObject("BotAttackerFar");
        BotMovementInputSource chase = botObject.AddComponent<BotMovementInputSource>();
        BotAttackInputSource source = botObject.AddComponent<BotAttackInputSource>();

        GameObject targetObject = CreateObject("FarTarget");
        targetObject.transform.position = new Vector3(5f, 0f, 0f);
        chase.Target = targetObject.transform;

        SerializedObject serializedSource = new SerializedObject(source);
        serializedSource.FindProperty("_attackRange").floatValue = 1.5f;
        serializedSource.FindProperty("_chaseTargetSource").objectReferenceValue = chase;
        serializedSource.ApplyModifiedPropertiesWithoutUndo();

        Assert.That(source.ConsumeBasicAttackPressed(), Is.False);
    }

    [Test]
    public void BotAttackInputSource_NullTarget_ReturnsFalse()
    {
        GameObject botObject = CreateObject("BotAttackerNoTarget");
        BotMovementInputSource chase = botObject.AddComponent<BotMovementInputSource>();
        BotAttackInputSource source = botObject.AddComponent<BotAttackInputSource>();

        SerializedObject serializedSource = new SerializedObject(source);
        serializedSource.FindProperty("_chaseTargetSource").objectReferenceValue = chase;
        serializedSource.ApplyModifiedPropertiesWithoutUndo();

        Assert.That(source.ConsumeBasicAttackPressed(), Is.False);
    }

    [Test]
    public void BotAttackInputSource_Awake_ResolvesChaseSourceFromSameGameObject()
    {
        GameObject botObject = CreateInactive("BotAttackerResolve");
        BotMovementInputSource chase = botObject.AddComponent<BotMovementInputSource>();
        BotAttackInputSource source = botObject.AddComponent<BotAttackInputSource>();

        GameObject targetObject = CreateObject("ResolveTarget");
        targetObject.transform.position = new Vector3(0.5f, 0f, 0f);
        chase.Target = targetObject.transform;

        botObject.SetActive(true);

        Assert.That(source.ConsumeBasicAttackPressed(), Is.True);
    }

    [Test]
    public void PlayerPrefab_WiresKeyboardAttackInputSource()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player.prefab");
        Assert.That(prefab, Is.Not.Null);

        KeyboardAttackInputSource keyboardAttack = prefab.GetComponent<KeyboardAttackInputSource>();
        CharacterCombat combat = prefab.GetComponent<CharacterCombat>();
        Assert.That(keyboardAttack, Is.Not.Null);
        Assert.That(combat, Is.Not.Null);

        SerializedObject serializedCombat = new SerializedObject(combat);
        Assert.That(
            serializedCombat.FindProperty("_attackInputSourceBehaviour").objectReferenceValue,
            Is.SameAs(keyboardAttack));
    }

    [Test]
    public void EnemyPrefab_WiresBotAttackInputSource()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemy.prefab");
        Assert.That(prefab, Is.Not.Null);

        BotAttackInputSource botAttack = prefab.GetComponent<BotAttackInputSource>();
        BotMovementInputSource botMovement = prefab.GetComponent<BotMovementInputSource>();
        CharacterCombat combat = prefab.GetComponent<CharacterCombat>();
        Assert.That(botAttack, Is.Not.Null);
        Assert.That(botMovement, Is.Not.Null);
        Assert.That(combat, Is.Not.Null);
        Assert.That(botAttack.AttackRange, Is.EqualTo(1.5f).Within(0.001f));

        SerializedObject serializedCombat = new SerializedObject(combat);
        Assert.That(
            serializedCombat.FindProperty("_attackInputSourceBehaviour").objectReferenceValue,
            Is.SameAs(botAttack));

        SerializedObject serializedBotAttack = new SerializedObject(botAttack);
        Assert.That(
            serializedBotAttack.FindProperty("_chaseTargetSource").objectReferenceValue,
            Is.SameAs(botMovement));
    }

    [Test]
    public void CharacterCombat_Update_ConsumesAttackInputAndRequestsBasicAttack()
    {
        GameObject root = CreateInactive("CombatWithAttackInput");
        KeyboardMovementInputSource movementInput = root.AddComponent<KeyboardMovementInputSource>();
        CombatCharacter character = root.AddComponent<CombatCharacter>();
        RegisteredCombatant combatant = root.AddComponent<RegisteredCombatant>();
        CharacterCombat combat = root.AddComponent<CharacterCombat>();
        FakeAttackInputSource inputSource = root.AddComponent<AttackInputSourceTestsFakeAttackInput>();

        SerializedObject serializedCharacter = new SerializedObject(character);
        serializedCharacter.FindProperty("_inputSourceBehaviour").objectReferenceValue = movementInput;
        serializedCharacter.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject serializedCombat = new SerializedObject(combat);
        serializedCombat.FindProperty("_registeredCombatant").objectReferenceValue = combatant;
        serializedCombat.FindProperty("_attackInputSourceBehaviour").objectReferenceValue = inputSource;
        SerializedProperty configuration = serializedCombat.FindProperty("_configuration");
        configuration.FindPropertyRelative("_vitality").FindPropertyRelative("_maxHealth").intValue = 30;
        configuration.FindPropertyRelative("_basicAttack").FindPropertyRelative("_damage").intValue = 10;
        configuration.FindPropertyRelative("_basicAttack").FindPropertyRelative("_cooldown").floatValue = 0.5f;
        configuration.FindPropertyRelative("_basicAttack").FindPropertyRelative("_activeTime").floatValue = 0.2f;
        serializedCombat.ApplyModifiedPropertiesWithoutUndo();

        root.SetActive(true);
        combat.Initialize(new DifferentTeamRelationshipService());

        inputSource.NextPressed = true;
        SimulateUpdate(combat);

        Assert.That(inputSource.ConsumeCount, Is.EqualTo(1));
        Assert.That(combat.TryBasicAttack(), Is.False);
    }

    private static void SimulateUpdate(CharacterCombat combat)
    {
        MethodInfo update = typeof(CharacterCombat).GetMethod(
            "Update",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(update, Is.Not.Null);
        update.Invoke(combat, null);
    }

    private GameObject CreateObject(string name)
    {
        GameObject created = new GameObject(name);
        _createdObjects.Add(created);
        return created;
    }

    private GameObject CreateInactive(string name)
    {
        GameObject created = CreateObject(name);
        created.SetActive(false);
        return created;
    }
}

internal sealed class AttackInputSourceTestsFakeAttackInput : MonoBehaviour, IAttackInputSource
{
    public bool NextPressed;
    public int ConsumeCount;

    public bool ConsumeBasicAttackPressed()
    {
        ConsumeCount++;
        if (!NextPressed)
        {
            return false;
        }

        NextPressed = false;
        return true;
    }
}
