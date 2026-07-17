using System;
using UnityEngine;

public readonly struct DamageRequest
{
    public DamageRequest(
        ICombatant attacker,
        uint attackExecutionId,
        int rawDamage,
        bool isCritical,
        Vector3 hitPoint)
    {
        if (rawDamage < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(rawDamage), "Raw damage must be non-negative.");
        }

        Attacker = attacker;
        AttackExecutionId = attackExecutionId;
        RawDamage = rawDamage;
        IsCritical = isCritical;
        HitPoint = hitPoint;
    }

    public ICombatant Attacker { get; }

    public uint AttackExecutionId { get; }

    public int RawDamage { get; }

    public bool IsCritical { get; }

    public Vector3 HitPoint { get; }
}
