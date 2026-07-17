using System;

public sealed class DefenseResolver : IDamageMitigation
{
    public DefenseResolver(int defense, int criticalDefense)
    {
        _defense = ClampNonNegative(defense);
        _criticalDefense = ClampNonNegative(criticalDefense);
    }

    private readonly int _defense;
    private readonly int _criticalDefense;

    public MitigationResult Mitigate(in DamageRequest request)
    {
        int normalDefenseApplied = _defense;
        int criticalDefenseApplied = request.IsCritical ? _criticalDefense : 0;
        int totalDefense = normalDefenseApplied + criticalDefenseApplied;
        int finalDamage = Math.Max(1, request.RawDamage - totalDefense);

        return new MitigationResult(
            normalDefenseApplied,
            criticalDefenseApplied,
            finalDamage);
    }

    private static int ClampNonNegative(int value)
    {
        return value < 0 ? 0 : value;
    }
}
