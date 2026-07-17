using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Builds combatants in code for PlayMode tests without scene or prefab dependencies.
/// </summary>
internal sealed class CombatPlayModeTestFixture
{
    private readonly List<GameObject> _createdObjects = new();
    private readonly CombatantRegistry _registry = new();
    private readonly DifferentTeamRelationshipService _relationshipService = new();

    public ICombatantRegistry Registry => _registry;

    public ITeamRelationshipService RelationshipService => _relationshipService;

    public void TearDown()
    {
        foreach (GameObject createdObject in _createdObjects)
        {
            if (createdObject != null)
            {
                UnityEngine.Object.Destroy(createdObject);
            }
        }

        _createdObjects.Clear();
    }

    public CharacterCombat CreateCombatant(
        string name,
        TeamId team,
        Vector2 position,
        int maxHealth = 30,
        int defense = 0,
        int damage = 10,
        float activeTime = 0.2f,
        float cooldown = 0.5f,
        float criticalChance = 0f,
        Vector2? hitboxLocalOffset = null)
    {
        int attackHitboxLayer = LayerMask.NameToLayer("AttackHitbox");
        int hurtboxLayer = LayerMask.NameToLayer("Hurtbox");
        Assert.That(attackHitboxLayer, Is.GreaterThanOrEqualTo(0), "AttackHitbox layer must exist.");
        Assert.That(hurtboxLayer, Is.GreaterThanOrEqualTo(0), "Hurtbox layer must exist.");

        Vector2 resolvedHitboxOffset = hitboxLocalOffset ?? new Vector2(0.75f, 0f);

        GameObject root = new GameObject(name);
        root.SetActive(false);
        _createdObjects.Add(root);
        root.transform.position = position;

        Rigidbody2D rigidbody = root.AddComponent<Rigidbody2D>();
        rigidbody.bodyType = RigidbodyType2D.Kinematic;
        rigidbody.gravityScale = 0f;
        rigidbody.useFullKinematicContacts = true;
        rigidbody.simulated = true;

        AlwaysZeroMovementInput movementInput = root.AddComponent<AlwaysZeroMovementInput>();
        FakeAttackInput attackInput = root.AddComponent<FakeAttackInput>();
        CombatCharacter character = root.AddComponent<CombatCharacter>();
        RegisteredCombatant registeredCombatant = root.AddComponent<RegisteredCombatant>();
        CharacterCombat combat = root.AddComponent<CharacterCombat>();

        GameObject hurtboxObject = new GameObject("BodyCollider");
        hurtboxObject.transform.SetParent(root.transform, false);
        hurtboxObject.layer = hurtboxLayer;
        _createdObjects.Add(hurtboxObject);
        BoxCollider2D hurtbox = hurtboxObject.AddComponent<BoxCollider2D>();
        hurtbox.isTrigger = false;
        hurtbox.size = new Vector2(1f, 1f);

        GameObject hitboxObject = new GameObject("BasicAttackHitbox");
        hitboxObject.transform.SetParent(root.transform, false);
        hitboxObject.transform.localPosition = new Vector3(resolvedHitboxOffset.x, resolvedHitboxOffset.y, 0f);
        hitboxObject.layer = attackHitboxLayer;
        _createdObjects.Add(hitboxObject);
        BoxCollider2D hitbox = hitboxObject.AddComponent<BoxCollider2D>();
        hitbox.isTrigger = true;
        hitbox.enabled = false;
        hitbox.size = new Vector2(0.8f, 1f);

        SetPrivateField(character, "_inputSourceBehaviour", movementInput);
        SetPrivateField(character, "_motorType", CombatCharacter.MovementMotorType.Rigidbody2D);
        SetPrivateField(character, "_rigidbody", rigidbody);

        CombatConfiguration configuration = new CombatConfiguration(
            new CombatConfiguration.VitalitySettings(maxHealth),
            new CombatConfiguration.DefenseSettings(defense, 0),
            new CombatConfiguration.BasicAttackSettings(
                damage,
                criticalChance,
                criticalDamageMultiplier: 1.5f,
                cooldown,
                activeTime));

        SetPrivateField(combat, "_configuration", configuration);
        SetPrivateField(combat, "_registeredCombatant", registeredCombatant);
        SetPrivateField(combat, "_basicAttackHitbox", hitbox);
        SetPrivateField(combat, "_attackInputSourceBehaviour", attackInput);

        SetPrivateField(registeredCombatant, "_character", character);

        root.SetActive(true);

        registeredCombatant.Initialize(_registry, team);
        combat.Initialize(_relationshipService);

        return combat;
    }

    public static void FaceDirection(CharacterCombat combatant, Vector2 direction)
    {
        AlwaysZeroMovementInput movementInput = combatant.GetComponent<AlwaysZeroMovementInput>();
        Assert.That(movementInput, Is.Not.Null);

        CombatCharacter character = combatant.GetComponent<CombatCharacter>();
        Assert.That(character, Is.Not.Null);
        Assert.That(character.MovementState, Is.InstanceOf<CharacterMovementController>());

        Vector3 positionBefore = combatant.transform.position;
        movementInput.Direction = direction;
        ((CharacterMovementController)character.MovementState).Tick(0.016f);
        movementInput.Direction = Vector2.zero;
        combatant.transform.position = positionBefore;
        Physics2D.SyncTransforms();
    }

    public static void PlaceOverlapping(CharacterCombat attacker, CharacterCombat defender)
    {
        attacker.transform.position = Vector3.zero;
        defender.transform.position = new Vector3(0.75f, 0f, 0f);
        Physics2D.SyncTransforms();
    }

    public static void PlaceOverlappingLeft(CharacterCombat attacker, CharacterCombat defender)
    {
        attacker.transform.position = Vector3.zero;
        defender.transform.position = new Vector3(-0.75f, 0f, 0f);
        Physics2D.SyncTransforms();
    }

    public static void PlaceSeparated(CharacterCombat attacker, CharacterCombat defender)
    {
        attacker.transform.position = Vector3.zero;
        defender.transform.position = new Vector3(5f, 0f, 0f);
        Physics2D.SyncTransforms();
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, $"Missing field {fieldName} on {target.GetType().Name}.");
        field.SetValue(target, value);
    }

    private sealed class AlwaysZeroMovementInput : MonoBehaviour, IMovementInputSource
    {
        public Vector2 Direction { get; set; }

        public Vector2 GetDirection()
        {
            return Direction;
        }
    }

    private sealed class FakeAttackInput : MonoBehaviour, IAttackInputSource
    {
        public bool ConsumeBasicAttackPressed()
        {
            return false;
        }
    }
}
