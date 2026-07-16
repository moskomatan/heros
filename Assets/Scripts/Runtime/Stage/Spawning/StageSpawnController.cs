using System.Collections.Generic;
using UnityEngine;

public sealed class StageSpawnController : MonoBehaviour
{
    [SerializeField] private List<CharacterSpawnEntry> _spawnEntries = new List<CharacterSpawnEntry>();

    private readonly List<RegisteredCombatant> _spawnedCombatants = new List<RegisteredCombatant>();
    private CombatantRegistry _registry;
    private CharacterSpawner _spawner;

    private void Start()
    {
        _registry = new CombatantRegistry();
        DifferentTeamRelationshipService relationshipService = new DifferentTeamRelationshipService();
        NearestEnemyTargetSelector targetSelector = new NearestEnemyTargetSelector();
        _spawner = new CharacterSpawner(_registry, targetSelector, relationshipService);

        for (int i = 0; i < _spawnEntries.Count; i++)
        {
            CharacterSpawnEntry entry = _spawnEntries[i];
            if (entry == null)
            {
                Debug.LogError($"{nameof(StageSpawnController)} has a null entry at index {i}.", this);
                continue;
            }

            RegisteredCombatant spawned = _spawner.Spawn(entry);
            if (spawned != null)
            {
                _spawnedCombatants.Add(spawned);
            }
        }
    }

    private void OnDestroy()
    {
        for (int i = 0; i < _spawnedCombatants.Count; i++)
        {
            RegisteredCombatant combatant = _spawnedCombatants[i];
            if (combatant != null)
            {
                Object.Destroy(combatant.gameObject);
            }
        }

        _spawnedCombatants.Clear();
    }
}
