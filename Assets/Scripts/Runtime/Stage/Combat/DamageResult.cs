public readonly struct DamageResult
{
    public DamageResult(
        bool wasApplied,
        int rawDamage,
        int normalDefenseApplied,
        int criticalDefenseApplied,
        int finalDamage,
        int remainingHealth,
        bool wasCritical,
        bool wasLethal)
    {
        WasApplied = wasApplied;
        RawDamage = rawDamage;
        NormalDefenseApplied = normalDefenseApplied;
        CriticalDefenseApplied = criticalDefenseApplied;
        FinalDamage = finalDamage;
        RemainingHealth = remainingHealth;
        WasCritical = wasCritical;
        WasLethal = wasLethal;
    }

    public bool WasApplied { get; }

    public int RawDamage { get; }

    public int NormalDefenseApplied { get; }

    public int CriticalDefenseApplied { get; }

    public int FinalDamage { get; }

    public int RemainingHealth { get; }

    public bool WasCritical { get; }

    public bool WasLethal { get; }
}
