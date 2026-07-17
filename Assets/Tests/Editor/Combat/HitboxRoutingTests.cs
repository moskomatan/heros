using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class HitboxRoutingTests
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
    public void CombatHurtboxResolver_Resolve_ReturnsReceiverFromParentHierarchy()
    {
        GameObject root = CreateInactive("HurtboxRoot");
        CharacterCombat combat = root.AddComponent<CharacterCombat>();
        GameObject body = new GameObject("BodyCollider");
        body.transform.SetParent(root.transform, false);
        _createdObjects.Add(body);
        BoxCollider2D hurtbox = body.AddComponent<BoxCollider2D>();
        root.SetActive(true);

        IDamageReceiver resolved = CombatHurtboxResolver.Resolve(hurtbox);

        Assert.That(resolved, Is.SameAs(combat));
    }

    [Test]
    public void CombatHurtboxResolver_Resolve_NullCollider_ReturnsNull()
    {
        Assert.That(CombatHurtboxResolver.Resolve(null), Is.Null);
    }

    [Test]
    public void ProcessHitboxCollider_CachesReceiverAndRaisesHitCandidateDetected()
    {
        CharacterCombat attacker = CreateCombatWithHitbox("Attacker");
        CharacterCombat defender = CreateCombatWithBody("Defender");
        Collider2D defenderHurtbox = defender.GetComponentInChildren<BoxCollider2D>();
        Assert.That(defenderHurtbox, Is.Not.Null);

        int eventCount = 0;
        IDamageReceiver reportedReceiver = null;
        attacker.HitCandidateDetected += (receiver, _, _) =>
        {
            eventCount++;
            reportedReceiver = receiver;
        };

        attacker.SetAttackHitboxEnabled(true);
        attacker.ProcessHitboxCollider(defenderHurtbox);
        attacker.ProcessHitboxCollider(defenderHurtbox);

        Assert.That(eventCount, Is.EqualTo(2));
        Assert.That(reportedReceiver, Is.SameAs(defender));

        Dictionary<Collider2D, IDamageReceiver> cache = GetHitReceiverCache(attacker);
        Assert.That(cache.ContainsKey(defenderHurtbox), Is.True);
        Assert.That(cache[defenderHurtbox], Is.SameAs(defender));
    }

    [Test]
    public void SetAttackHitboxEnabled_False_ClearsCacheAndDisablesCollider()
    {
        CharacterCombat attacker = CreateCombatWithHitbox("AttackerCacheClear");
        CharacterCombat defender = CreateCombatWithBody("DefenderCacheClear");
        Collider2D defenderHurtbox = defender.GetComponentInChildren<BoxCollider2D>();

        attacker.SetAttackHitboxEnabled(true);
        attacker.ProcessHitboxCollider(defenderHurtbox);
        Assert.That(GetHitReceiverCache(attacker), Is.Not.Empty);

        attacker.SetAttackHitboxEnabled(false);

        Assert.That(attacker.BasicAttackHitbox.enabled, Is.False);
        Assert.That(GetHitReceiverCache(attacker), Is.Empty);
    }

    [Test]
    public void Awake_DisablesReferencedAttackHitbox()
    {
        GameObject root = CreateInactive("HitboxDisabledRoot");
        CharacterCombat combat = root.AddComponent<CharacterCombat>();
        GameObject hitboxObject = new GameObject("BasicAttackHitbox");
        hitboxObject.transform.SetParent(root.transform, false);
        _createdObjects.Add(hitboxObject);
        BoxCollider2D hitbox = hitboxObject.AddComponent<BoxCollider2D>();
        hitbox.isTrigger = true;
        hitbox.enabled = true;

        SerializedObject serializedObject = new SerializedObject(combat);
        serializedObject.FindProperty("_basicAttackHitbox").objectReferenceValue = hitbox;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();

        root.SetActive(true);

        Assert.That(combat.BasicAttackHitbox, Is.SameAs(hitbox));
        Assert.That(combat.BasicAttackHitbox.enabled, Is.False);
    }

    [Test]
    public void PlayerPrefab_BasicAttackHitbox_IsDisabledByDefault()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player.prefab");
        Assert.That(prefab, Is.Not.Null);

        CharacterCombat combat = prefab.GetComponent<CharacterCombat>();
        Assert.That(combat, Is.Not.Null);
        Assert.That(combat.BasicAttackHitbox, Is.Not.Null);
        Assert.That(combat.BasicAttackHitbox.enabled, Is.False);
        Assert.That(combat.BasicAttackHitbox.isTrigger, Is.True);
        Assert.That(prefab.GetComponent<Rigidbody2D>(), Is.Not.Null);
        Assert.That(prefab.GetComponent<Rigidbody2D>().bodyType, Is.EqualTo(RigidbodyType2D.Kinematic));
    }

    [Test]
    public void EnemyPrefab_BasicAttackHitbox_IsDisabledByDefault()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemy.prefab");
        Assert.That(prefab, Is.Not.Null);

        CharacterCombat combat = prefab.GetComponent<CharacterCombat>();
        Assert.That(combat, Is.Not.Null);
        Assert.That(combat.BasicAttackHitbox, Is.Not.Null);
        Assert.That(combat.BasicAttackHitbox.enabled, Is.False);
        Assert.That(combat.BasicAttackHitbox.isTrigger, Is.True);
        Assert.That(prefab.GetComponent<Rigidbody2D>(), Is.Not.Null);
    }

    [Test]
    public void ProcessHitboxCollider_IgnoresOwnedColliders()
    {
        CharacterCombat attacker = CreateCombatWithHitbox("SelfHitAttacker");
        int eventCount = 0;
        attacker.HitCandidateDetected += (_, _, _) => eventCount++;

        attacker.SetAttackHitboxEnabled(true);
        attacker.ProcessHitboxCollider(attacker.BasicAttackHitbox);

        Assert.That(eventCount, Is.Zero);
    }

    private CharacterCombat CreateCombatWithHitbox(string name)
    {
        GameObject root = CreateInactive(name);
        root.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
        CharacterCombat combat = root.AddComponent<CharacterCombat>();

        GameObject hitboxObject = new GameObject("BasicAttackHitbox");
        hitboxObject.transform.SetParent(root.transform, false);
        _createdObjects.Add(hitboxObject);
        BoxCollider2D hitbox = hitboxObject.AddComponent<BoxCollider2D>();
        hitbox.isTrigger = true;
        hitbox.enabled = false;

        SerializedObject serializedObject = new SerializedObject(combat);
        serializedObject.FindProperty("_basicAttackHitbox").objectReferenceValue = hitbox;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();

        root.SetActive(true);
        return combat;
    }

    private CharacterCombat CreateCombatWithBody(string name)
    {
        GameObject root = CreateInactive(name);
        CharacterCombat combat = root.AddComponent<CharacterCombat>();
        GameObject body = new GameObject("BodyCollider");
        body.transform.SetParent(root.transform, false);
        _createdObjects.Add(body);
        body.AddComponent<BoxCollider2D>();
        root.SetActive(true);
        return combat;
    }

    private GameObject CreateInactive(string name)
    {
        GameObject gameObject = new GameObject(name);
        gameObject.SetActive(false);
        _createdObjects.Add(gameObject);
        return gameObject;
    }

    private static Dictionary<Collider2D, IDamageReceiver> GetHitReceiverCache(CharacterCombat combat)
    {
        FieldInfo field = typeof(CharacterCombat).GetField(
            "_hitReceiverCache",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null);
        return (Dictionary<Collider2D, IDamageReceiver>)field.GetValue(combat);
    }
}
