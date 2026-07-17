using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public sealed class CombatantRegistryTests
{
    private CombatantRegistry _registry;
    private readonly IList<GameObject> _createdObjects = new List<GameObject>();

    [SetUp]
    public void SetUp()
    {
        _registry = new CombatantRegistry();
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
    public void Register_AddsCombatant()
    {
        RegisteredCombatant combatant = CreateCombatant();

        _registry.Register(combatant);

        Assert.That(_registry.Combatants.Count, Is.EqualTo(1));
        Assert.That(_registry.Combatants[0], Is.SameAs(combatant));
    }

    [Test]
    public void Register_Duplicate_DoesNotDuplicate()
    {
        RegisteredCombatant combatant = CreateCombatant();

        _registry.Register(combatant);
        _registry.Register(combatant);

        Assert.That(_registry.Combatants.Count, Is.EqualTo(1));
    }

    [Test]
    public void Unregister_RemovesCombatant()
    {
        RegisteredCombatant combatant = CreateCombatant();
        _registry.Register(combatant);

        _registry.Unregister(combatant);

        Assert.That(_registry.Combatants.Count, Is.EqualTo(0));
    }

    [Test]
    public void Unregister_AbsentCombatant_IsSafe()
    {
        RegisteredCombatant combatant = CreateCombatant();

        Assert.DoesNotThrow(() => _registry.Unregister(combatant));
        Assert.That(_registry.Combatants.Count, Is.EqualTo(0));
    }

    [Test]
    public void Combatants_IsNotMutableList()
    {
        Assert.That(_registry.Combatants, Is.Not.InstanceOf<List<ICombatant>>());
    }

    private RegisteredCombatant CreateCombatant()
    {
        GameObject gameObject = new GameObject("RegistryTestCombatant");
        _createdObjects.Add(gameObject);
        return gameObject.AddComponent<RegisteredCombatant>();
    }
}
