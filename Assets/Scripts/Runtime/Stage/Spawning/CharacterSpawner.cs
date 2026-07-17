using System;
using UnityEngine;

public sealed class CharacterSpawner
{
    private const float DefaultBotReevaluateInterval = 0.25f;

    private readonly ICombatantRegistry _registry;
    private readonly ITargetSelector _targetSelector;
    private readonly ITeamRelationshipService _relationshipService;
    private readonly BotTargetRunner _botTargetRunner;

    public CharacterSpawner(
        ICombatantRegistry registry,
        ITargetSelector targetSelector,
        ITeamRelationshipService relationshipService,
        BotTargetRunner botTargetRunner)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _targetSelector = targetSelector ?? throw new ArgumentNullException(nameof(targetSelector));
        _relationshipService = relationshipService ?? throw new ArgumentNullException(nameof(relationshipService));
        _botTargetRunner = botTargetRunner ?? throw new ArgumentNullException(nameof(botTargetRunner));
    }

    public RegisteredCombatant Spawn(CharacterSpawnEntry entry)
    {
        if (entry == null)
        {
            Debug.LogError($"{nameof(CharacterSpawner)} received a null {nameof(CharacterSpawnEntry)}.");
            return null;
        }

        if (entry.Prefab == null)
        {
            Debug.LogError($"{nameof(CharacterSpawner)} spawn entry has a null prefab.");
            return null;
        }

        if (entry.SpawnPoint == null)
        {
            Debug.LogError($"{nameof(CharacterSpawner)} spawn entry has a null spawn point.");
            return null;
        }

        if (entry.Team == TeamId.Neutral)
        {
            Debug.LogWarning(
                $"{nameof(CharacterSpawner)} is spawning neutral combatant from prefab '{entry.Prefab.name}'. " +
                "Neutral combatants are not treated as enemies by bots.");
        }

        GameObject instance = UnityEngine.Object.Instantiate(
            entry.Prefab,
            entry.SpawnPoint.position,
            entry.SpawnPoint.rotation);

        RegisteredCombatant registeredCombatant = instance.GetComponent<RegisteredCombatant>();

        if (registeredCombatant == null)
        {
            Debug.LogError(
                $"{nameof(CharacterSpawner)} failed to spawn {entry.Prefab.name}: missing {nameof(RegisteredCombatant)}.",
                instance);
            UnityEngine.Object.Destroy(instance);
            return null;
        }

        CombatCharacter character = instance.GetComponent<CombatCharacter>();
        if (character == null)
        {
            Debug.LogError(
                $"{nameof(CharacterSpawner)} failed to spawn {entry.Prefab.name}: missing {nameof(CombatCharacter)}.",
                instance);
            UnityEngine.Object.Destroy(instance);
            return null;
        }

        registeredCombatant.Initialize(_registry, entry.Team);

        BotMovementInputSource botMovementInputSource = instance.GetComponent<BotMovementInputSource>();
        if (botMovementInputSource != null)
        {
            BotTargetController botTargetController = new BotTargetController(
                registeredCombatant,
                botMovementInputSource,
                _registry,
                _targetSelector,
                _relationshipService,
                DefaultBotReevaluateInterval);

            BotTargetBinding binding = instance.GetComponent<BotTargetBinding>();
            if (binding == null)
            {
                binding = instance.AddComponent<BotTargetBinding>();
            }

            binding.Initialize(_botTargetRunner, botTargetController);
        }

        return registeredCombatant;
    }
}
