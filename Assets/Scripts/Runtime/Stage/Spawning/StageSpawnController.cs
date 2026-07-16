using System.Collections.Generic;
using UnityEngine;

public sealed class StageSpawnController : MonoBehaviour
{
    [SerializeField] private List<CharacterSpawnEntry> _spawnEntries = new List<CharacterSpawnEntry>();

    private void Start()
    {
        CombatantRegistry registry = new CombatantRegistry();
        DifferentTeamRelationshipService relationshipService = new DifferentTeamRelationshipService();
        NearestEnemyTargetSelector targetSelector = new NearestEnemyTargetSelector();
        CharacterSpawner spawner = new CharacterSpawner(registry, targetSelector, relationshipService);

        for (int i = 0; i < _spawnEntries.Count; i++)
        {
            CharacterSpawnEntry entry = _spawnEntries[i];
            if (entry == null)
            {
                Debug.LogError($"{nameof(StageSpawnController)} has a null entry at index {i}.", this);
                continue;
            }

            spawner.Spawn(entry);
        }
    }
}
