using System.Collections.Generic;
using UnityEngine;

public sealed class StageSpawnController : MonoBehaviour
{
    [SerializeField] private List<CharacterSpawnEntry> _spawnEntries = new List<CharacterSpawnEntry>();

    private readonly IList<GameObject> _spawnedInstances = new List<GameObject>();
    private BotTargetRunner _botTargetRunner;

    private void Start()
    {
        CombatantRegistry registry = new CombatantRegistry();
        DifferentTeamRelationshipService relationshipService = new DifferentTeamRelationshipService();
        NearestEnemyTargetSelector targetSelector = new NearestEnemyTargetSelector();
        _botTargetRunner = new BotTargetRunner();
        CharacterSpawner spawner = new CharacterSpawner(registry, targetSelector, relationshipService, _botTargetRunner);

        for (int i = 0; i < _spawnEntries.Count; i++)
        {
            CharacterSpawnEntry entry = _spawnEntries[i];
            if (entry == null)
            {
                Debug.LogError($"{nameof(StageSpawnController)} has a null entry at index {i}.", this);
                continue;
            }

            RegisteredCombatant spawned = spawner.Spawn(entry);
            if (spawned != null)
            {
                _spawnedInstances.Add(spawned.gameObject);
            }
        }
    }

    private void Update()
    {
        if (_botTargetRunner == null)
        {
            return;
        }

        _botTargetRunner.Tick(Time.deltaTime);
    }

    private void OnDestroy()
    {
        if (_botTargetRunner != null)
        {
            _botTargetRunner.StopAll();
            _botTargetRunner = null;
        }

        foreach (GameObject instance in _spawnedInstances)
        {
            if (instance != null)
            {
                Destroy(instance);
            }
        }

        _spawnedInstances.Clear();
    }
}
