using System;
using UnityEngine;

[Serializable]
public sealed class CharacterMovementSettings
{
    [SerializeField] private float _moveSpeed = 5f;
    [SerializeField] private float _deadZone = 0.1f;
    [SerializeField] private bool _normalizeDiagonals = true;

    public float MoveSpeed => _moveSpeed;
    public float DeadZone => _deadZone;
    public bool NormalizeDiagonals => _normalizeDiagonals;
}
