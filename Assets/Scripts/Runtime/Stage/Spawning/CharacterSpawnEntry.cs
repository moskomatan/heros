using System;
using UnityEngine;

[Serializable]
public sealed class CharacterSpawnEntry
{
    [SerializeField] private GameObject _prefab;
    [SerializeField] private Transform _spawnPoint;
    [SerializeField] private TeamId _team;

    public GameObject Prefab => _prefab;
    public Transform SpawnPoint => _spawnPoint;
    public TeamId Team => _team;
}
