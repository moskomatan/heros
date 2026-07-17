using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class BotTargetLifecycleTests
{
    private BotTargetRunner _runner;
    private CombatantRegistry _registry;
    private NearestEnemyTargetSelector _targetSelector;
    private DifferentTeamRelationshipService _relationshipService;
    private readonly IList<GameObject> _createdObjects = new List<GameObject>();

    [SetUp]
    public void SetUp()
    {
        _runner = new BotTargetRunner();
        _registry = new CombatantRegistry();
        _targetSelector = new NearestEnemyTargetSelector();
        _relationshipService = new DifferentTeamRelationshipService();
    }

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
    public void Unregister_StopsControllerAndSkipsFurtherTicks()
    {
        BotMovementInputSource chaseSource = CreateController(TeamId.TeamOne, out BotTargetController controller);
        CreateCombatant(TeamId.TeamTwo, new Vector3(2f, 0f, 0f));

        _runner.Register(controller);
        _runner.Tick(0.3f);

        Assert.That(chaseSource.Target, Is.Not.Null);

        _runner.Unregister(controller);
        Transform targetBeforeSecondTick = chaseSource.Target;
        _runner.Tick(0.3f);

        Assert.That(chaseSource.Target, Is.EqualTo(targetBeforeSecondTick));
    }

    [Test]
    public void Unregister_AbsentController_IsSafe()
    {
        CreateController(TeamId.TeamOne, out BotTargetController controller);

        Assert.DoesNotThrow(() => _runner.Unregister(controller));
    }

    [Test]
    public void BotTargetBinding_Disable_UnregistersController()
    {
        BotMovementInputSource chaseSource = CreateController(TeamId.TeamOne, out BotTargetController controller);
        CreateCombatant(TeamId.TeamTwo, new Vector3(2f, 0f, 0f));
        BotTargetBinding binding = CreateBinding(controller);

        _runner.Tick(0.3f);
        Assert.That(chaseSource.Target, Is.Not.Null);

        binding.gameObject.SetActive(false);
        Transform targetAfterDisable = chaseSource.Target;
        _runner.Tick(0.3f);

        Assert.That(chaseSource.Target, Is.EqualTo(targetAfterDisable));
    }

    [Test]
    public void BotTargetBinding_ReEnable_RegistersControllerAgain()
    {
        BotMovementInputSource chaseSource = CreateController(TeamId.TeamOne, out BotTargetController controller);
        CreateCombatant(TeamId.TeamTwo, new Vector3(2f, 0f, 0f));
        BotTargetBinding binding = CreateBinding(controller);

        binding.gameObject.SetActive(false);
        _runner.Tick(0.3f);
        Assert.That(chaseSource.Target, Is.Null);

        binding.gameObject.SetActive(true);
        _runner.Tick(0.3f);

        Assert.That(chaseSource.Target, Is.Not.Null);
    }

    [Test]
    public void BotTargetBinding_Destroy_UnregistersController()
    {
        BotMovementInputSource chaseSource = CreateController(TeamId.TeamOne, out BotTargetController controller);
        CreateCombatant(TeamId.TeamTwo, new Vector3(2f, 0f, 0f));
        BotTargetBinding binding = CreateBinding(controller);

        _runner.Tick(0.3f);
        Assert.That(chaseSource.Target, Is.Not.Null);

        Object.DestroyImmediate(binding.gameObject);
        Transform targetAfterDestroy = chaseSource.Target;
        _runner.Tick(0.3f);

        Assert.That(chaseSource.Target, Is.EqualTo(targetAfterDestroy));
    }

    [Test]
    public void Tick_DestroyedObserver_DoesNotThrow()
    {
        GameObject observerObject = CreateObserverObject(TeamId.TeamOne, out BotMovementInputSource chaseSource, out RegisteredCombatant observer);
        BotTargetController controller = new BotTargetController(
            observer,
            chaseSource,
            _registry,
            _targetSelector,
            _relationshipService);

        CreateCombatant(TeamId.TeamTwo, new Vector3(2f, 0f, 0f));
        _runner.Register(controller);
        _runner.Tick(0.3f);

        Object.DestroyImmediate(observerObject);
        _createdObjects.Remove(observerObject);

        Assert.DoesNotThrow(() => _runner.Tick(0.3f));
    }

    private BotTargetBinding CreateBinding(BotTargetController controller)
    {
        GameObject bindingObject = new GameObject("BotTargetBindingTest");
        _createdObjects.Add(bindingObject);
        BotTargetBinding binding = bindingObject.AddComponent<BotTargetBinding>();
        binding.Initialize(_runner, controller);
        return binding;
    }

    private BotMovementInputSource CreateController(TeamId team, out BotTargetController controller)
    {
        GameObject observerObject = CreateObserverObject(team, out BotMovementInputSource chaseSource, out RegisteredCombatant observer);
        controller = new BotTargetController(
            observer,
            chaseSource,
            _registry,
            _targetSelector,
            _relationshipService);

        return chaseSource;
    }

    private GameObject CreateObserverObject(
        TeamId team,
        out BotMovementInputSource chaseSource,
        out RegisteredCombatant observer)
    {
        GameObject observerObject = new GameObject("Observer");
        observerObject.SetActive(false);
        _createdObjects.Add(observerObject);

        chaseSource = observerObject.AddComponent<BotMovementInputSource>();
        CombatCharacter character = observerObject.AddComponent<CombatCharacter>();
        observer = observerObject.AddComponent<RegisteredCombatant>();

        SerializedObject characterSerializedObject = new SerializedObject(character);
        characterSerializedObject.FindProperty("_inputSourceBehaviour").objectReferenceValue = chaseSource;
        characterSerializedObject.ApplyModifiedPropertiesWithoutUndo();

        observer.Initialize(_registry, team);
        observerObject.SetActive(true);

        return observerObject;
    }

    private RegisteredCombatant CreateCombatant(TeamId team, Vector3 position)
    {
        GameObject gameObject = new GameObject($"Combatant_{team}");
        gameObject.SetActive(false);
        _createdObjects.Add(gameObject);
        gameObject.transform.position = position;

        KeyboardMovementInputSource input = gameObject.AddComponent<KeyboardMovementInputSource>();
        CombatCharacter character = gameObject.AddComponent<CombatCharacter>();
        RegisteredCombatant registeredCombatant = gameObject.AddComponent<RegisteredCombatant>();

        SerializedObject characterSerializedObject = new SerializedObject(character);
        characterSerializedObject.FindProperty("_inputSourceBehaviour").objectReferenceValue = input;
        characterSerializedObject.ApplyModifiedPropertiesWithoutUndo();

        registeredCombatant.Initialize(_registry, team);
        gameObject.SetActive(true);

        return registeredCombatant;
    }
}
