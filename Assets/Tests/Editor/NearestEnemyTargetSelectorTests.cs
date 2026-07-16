using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class NearestEnemyTargetSelectorTests
{
    private NearestEnemyTargetSelector _selector;
    private DifferentTeamRelationshipService _relationshipService;
    private readonly List<GameObject> _createdObjects = new List<GameObject>();

    [SetUp]
    public void SetUp()
    {
        _selector = new NearestEnemyTargetSelector();
        _relationshipService = new DifferentTeamRelationshipService();
    }

    [TearDown]
    public void TearDown()
    {
        for (int i = 0; i < _createdObjects.Count; i++)
        {
            if (_createdObjects[i] != null)
            {
                Object.DestroyImmediate(_createdObjects[i]);
            }
        }

        _createdObjects.Clear();
    }

    [Test]
    public void SelectTarget_ExcludesSelf()
    {
        RegisteredCombatant observer = CreateCombatant(TeamId.TeamOne, Vector3.zero);
        List<RegisteredCombatant> candidates = new List<RegisteredCombatant> { observer };

        RegisteredCombatant selected = _selector.SelectTarget(observer, candidates, _relationshipService);

        Assert.That(selected, Is.Null);
    }

    [Test]
    public void SelectTarget_ExcludesAllies()
    {
        RegisteredCombatant observer = CreateCombatant(TeamId.TeamOne, Vector3.zero);
        RegisteredCombatant ally = CreateCombatant(TeamId.TeamOne, new Vector3(1f, 0f, 0f));
        List<RegisteredCombatant> candidates = new List<RegisteredCombatant> { observer, ally };

        RegisteredCombatant selected = _selector.SelectTarget(observer, candidates, _relationshipService);

        Assert.That(selected, Is.Null);
    }

    [Test]
    public void SelectTarget_ExcludesNeutralCombatants()
    {
        RegisteredCombatant observer = CreateCombatant(TeamId.TeamOne, Vector3.zero);
        RegisteredCombatant neutral = CreateCombatant(TeamId.Neutral, new Vector3(1f, 0f, 0f));
        List<RegisteredCombatant> candidates = new List<RegisteredCombatant> { observer, neutral };

        RegisteredCombatant selected = _selector.SelectTarget(observer, candidates, _relationshipService);

        Assert.That(selected, Is.Null);
    }

    [Test]
    public void SelectTarget_ExcludesInactiveTargets()
    {
        RegisteredCombatant observer = CreateCombatant(TeamId.TeamOne, Vector3.zero);
        RegisteredCombatant enemy = CreateCombatant(TeamId.TeamTwo, new Vector3(1f, 0f, 0f));
        enemy.enabled = false;
        List<RegisteredCombatant> candidates = new List<RegisteredCombatant> { observer, enemy };

        RegisteredCombatant selected = _selector.SelectTarget(observer, candidates, _relationshipService);

        Assert.That(selected, Is.Null);
    }

    [Test]
    public void SelectTarget_ChoosesNearestEnemy()
    {
        RegisteredCombatant observer = CreateCombatant(TeamId.TeamOne, Vector3.zero);
        RegisteredCombatant farEnemy = CreateCombatant(TeamId.TeamTwo, new Vector3(10f, 0f, 0f));
        RegisteredCombatant nearEnemy = CreateCombatant(TeamId.TeamTwo, new Vector3(2f, 0f, 0f));
        List<RegisteredCombatant> candidates = new List<RegisteredCombatant> { observer, farEnemy, nearEnemy };

        RegisteredCombatant selected = _selector.SelectTarget(observer, candidates, _relationshipService);

        Assert.That(selected, Is.SameAs(nearEnemy));
    }

    [Test]
    public void SelectTarget_NoEnemyAvailable_ReturnsNull()
    {
        RegisteredCombatant observer = CreateCombatant(TeamId.TeamOne, Vector3.zero);
        List<RegisteredCombatant> candidates = new List<RegisteredCombatant> { observer };

        RegisteredCombatant selected = _selector.SelectTarget(observer, candidates, _relationshipService);

        Assert.That(selected, Is.Null);
    }

    [Test]
    public void SelectTarget_EqualDistance_KeepsFirstRegisteredEnemy()
    {
        RegisteredCombatant observer = CreateCombatant(TeamId.TeamOne, Vector3.zero);
        RegisteredCombatant firstEnemy = CreateCombatant(TeamId.TeamTwo, new Vector3(3f, 0f, 0f));
        RegisteredCombatant secondEnemy = CreateCombatant(TeamId.TeamTwo, new Vector3(3f, 0f, 0f));
        List<RegisteredCombatant> candidates = new List<RegisteredCombatant>
        {
            observer,
            firstEnemy,
            secondEnemy
        };

        RegisteredCombatant selected = _selector.SelectTarget(observer, candidates, _relationshipService);

        Assert.That(selected, Is.SameAs(firstEnemy));
    }

    private RegisteredCombatant CreateCombatant(TeamId team, Vector3 position)
    {
        GameObject gameObject = new GameObject($"SelectorTest_{team}");
        gameObject.SetActive(false);
        _createdObjects.Add(gameObject);
        gameObject.transform.position = position;

        KeyboardMovementInputSource input = gameObject.AddComponent<KeyboardMovementInputSource>();
        CombatCharacter character = gameObject.AddComponent<CombatCharacter>();
        TeamMember teamMember = gameObject.AddComponent<TeamMember>();
        RegisteredCombatant registeredCombatant = gameObject.AddComponent<RegisteredCombatant>();

        SerializedObject characterSerializedObject = new SerializedObject(character);
        characterSerializedObject.FindProperty("_inputSourceBehaviour").objectReferenceValue = input;
        characterSerializedObject.ApplyModifiedPropertiesWithoutUndo();

        teamMember.SetTeam(team);
        gameObject.SetActive(true);

        Assert.That(registeredCombatant.IsTargetable, Is.True);
        return registeredCombatant;
    }
}
