using UnityEngine;

public sealed class CharacterSpawner
{
    private readonly ICombatantRegistry _registry;
    private readonly ITargetSelector _targetSelector;
    private readonly ITeamRelationshipService _relationshipService;

    public CharacterSpawner(
        ICombatantRegistry registry,
        ITargetSelector targetSelector,
        ITeamRelationshipService relationshipService)
    {
        _registry = registry;
        _targetSelector = targetSelector;
        _relationshipService = relationshipService;
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

        GameObject instance = Object.Instantiate(
            entry.Prefab,
            entry.SpawnPoint.position,
            entry.SpawnPoint.rotation);

        TeamMember teamMember = instance.GetComponent<TeamMember>();
        RegisteredCombatant registeredCombatant = instance.GetComponent<RegisteredCombatant>();
        BotTargetController botTargetController = instance.GetComponent<BotTargetController>();

        if (teamMember == null)
        {
            Debug.LogError(
                $"{nameof(CharacterSpawner)} failed to spawn {entry.Prefab.name}: missing {nameof(TeamMember)}.",
                instance);
            Object.Destroy(instance);
            return null;
        }

        if (registeredCombatant == null)
        {
            Debug.LogError(
                $"{nameof(CharacterSpawner)} failed to spawn {entry.Prefab.name}: missing {nameof(RegisteredCombatant)}.",
                instance);
            Object.Destroy(instance);
            return null;
        }

        teamMember.SetTeam(entry.Team);
        registeredCombatant.Initialize(_registry);

        if (botTargetController != null)
        {
            botTargetController.Initialize(_registry, _targetSelector, _relationshipService);
        }

        return registeredCombatant;
    }
}
