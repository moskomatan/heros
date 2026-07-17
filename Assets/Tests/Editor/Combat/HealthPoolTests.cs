using System;
using NUnit.Framework;

public sealed class HealthPoolTests
{
    [Test]
    public void Constructor_SetsMaxAndCurrentHealth()
    {
        HealthPool healthPool = new HealthPool(30);

        Assert.That(healthPool.MaxHealth, Is.EqualTo(30));
        Assert.That(healthPool.CurrentHealth, Is.EqualTo(30));
        Assert.That(healthPool.IsAlive, Is.True);
    }

    [Test]
    public void Constructor_NegativeMaxHealth_ClampsToZero()
    {
        HealthPool healthPool = new HealthPool(-5);

        Assert.That(healthPool.MaxHealth, Is.Zero);
        Assert.That(healthPool.CurrentHealth, Is.Zero);
        Assert.That(healthPool.IsAlive, Is.False);
    }

    [Test]
    public void Constructor_CurrentHealthAboveMax_ClampsToMax()
    {
        HealthPool healthPool = new HealthPool(10, 25);

        Assert.That(healthPool.CurrentHealth, Is.EqualTo(10));
    }

    [Test]
    public void Constructor_NegativeCurrentHealth_ClampsToZero()
    {
        HealthPool healthPool = new HealthPool(10, -3);

        Assert.That(healthPool.CurrentHealth, Is.Zero);
        Assert.That(healthPool.IsAlive, Is.False);
    }

    [Test]
    public void ApplyDamage_SubtractsFromCurrentHealth()
    {
        HealthPool healthPool = new HealthPool(30);

        healthPool.ApplyDamage(7);

        Assert.That(healthPool.CurrentHealth, Is.EqualTo(23));
        Assert.That(healthPool.IsAlive, Is.True);
    }

    [Test]
    public void ApplyDamage_NeverGoesBelowZero()
    {
        HealthPool healthPool = new HealthPool(5);

        healthPool.ApplyDamage(20);

        Assert.That(healthPool.CurrentHealth, Is.Zero);
        Assert.That(healthPool.IsAlive, Is.False);
    }

    [Test]
    public void ApplyDamage_WhenLethal_FiresDiedOnce()
    {
        HealthPool healthPool = new HealthPool(10);
        int diedCount = 0;
        healthPool.Died += () => diedCount++;

        healthPool.ApplyDamage(10);

        Assert.That(diedCount, Is.EqualTo(1));
        Assert.That(healthPool.CurrentHealth, Is.Zero);
    }

    [Test]
    public void ApplyDamage_AfterDeath_IsIgnored()
    {
        HealthPool healthPool = new HealthPool(10);
        healthPool.ApplyDamage(10);

        healthPool.ApplyDamage(5);

        Assert.That(healthPool.CurrentHealth, Is.Zero);
    }

    [Test]
    public void ApplyDamage_AfterDeath_DoesNotFireEvents()
    {
        HealthPool healthPool = new HealthPool(10);
        int damagedCount = 0;
        int healthChangedCount = 0;
        int diedCount = 0;
        healthPool.Damaged += (_, _) => damagedCount++;
        healthPool.HealthChanged += (_, _) => healthChangedCount++;
        healthPool.Died += () => diedCount++;
        healthPool.ApplyDamage(10);

        healthPool.ApplyDamage(3);

        Assert.That(damagedCount, Is.EqualTo(1));
        Assert.That(healthChangedCount, Is.EqualTo(1));
        Assert.That(diedCount, Is.EqualTo(1));
    }

    [Test]
    public void ApplyDamage_ZeroOrNegative_IsIgnored()
    {
        HealthPool healthPool = new HealthPool(10);
        int damagedCount = 0;
        healthPool.Damaged += (_, _) => damagedCount++;

        healthPool.ApplyDamage(0);
        healthPool.ApplyDamage(-1);

        Assert.That(healthPool.CurrentHealth, Is.EqualTo(10));
        Assert.That(damagedCount, Is.Zero);
    }

    [Test]
    public void ApplyDamage_FiresDamagedAndHealthChanged()
    {
        HealthPool healthPool = new HealthPool(30);
        int damagedFinalDamage = -1;
        int damagedRemaining = -1;
        int healthChangedCurrent = -1;
        int healthChangedMax = -1;
        healthPool.Damaged += (finalDamage, remaining) =>
        {
            damagedFinalDamage = finalDamage;
            damagedRemaining = remaining;
        };
        healthPool.HealthChanged += (current, max) =>
        {
            healthChangedCurrent = current;
            healthChangedMax = max;
        };

        healthPool.ApplyDamage(7);

        Assert.That(damagedFinalDamage, Is.EqualTo(7));
        Assert.That(damagedRemaining, Is.EqualTo(23));
        Assert.That(healthChangedCurrent, Is.EqualTo(23));
        Assert.That(healthChangedMax, Is.EqualTo(30));
    }
}
