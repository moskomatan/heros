using System;

public sealed class HealthPool
{
    public HealthPool(int maxHealth)
        : this(maxHealth, maxHealth)
    {
    }

    public HealthPool(int maxHealth, int currentHealth)
    {
        MaxHealth = ClampNonNegative(maxHealth);
        CurrentHealth = Clamp(currentHealth, 0, MaxHealth);
    }

    public int MaxHealth { get; }

    public int CurrentHealth { get; private set; }

    public bool IsAlive => CurrentHealth > 0;

    public event Action<int, int> Damaged;

    public event Action<int, int> HealthChanged;

    public event Action Died;

    public void ApplyDamage(int finalDamage)
    {
        if (!IsAlive || finalDamage <= 0)
        {
            return;
        }

        int previousHealth = CurrentHealth;
        CurrentHealth = Math.Max(0, CurrentHealth - finalDamage);

        Damaged?.Invoke(finalDamage, CurrentHealth);
        HealthChanged?.Invoke(CurrentHealth, MaxHealth);

        if (previousHealth > 0 && CurrentHealth == 0)
        {
            Died?.Invoke();
        }
    }

    private static int ClampNonNegative(int value)
    {
        return value < 0 ? 0 : value;
    }

    private static int Clamp(int value, int min, int max)
    {
        if (value < min)
        {
            return min;
        }

        if (value > max)
        {
            return max;
        }

        return value;
    }
}
