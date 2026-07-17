public readonly struct MitigationResult
{
    public MitigationResult(
        int normalDefenseApplied,
        int criticalDefenseApplied,
        int finalDamage)
    {
        NormalDefenseApplied = normalDefenseApplied;
        CriticalDefenseApplied = criticalDefenseApplied;
        FinalDamage = finalDamage;
    }

    public int NormalDefenseApplied { get; }

    public int CriticalDefenseApplied { get; }

    public int FinalDamage { get; }
}
