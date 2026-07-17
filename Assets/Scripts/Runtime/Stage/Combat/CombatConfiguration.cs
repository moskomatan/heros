using System;
using UnityEngine;

[Serializable]
public sealed class CombatConfiguration
{
    [SerializeField] private VitalitySettings _vitality = new VitalitySettings();
    [SerializeField] private DefenseSettings _defense = new DefenseSettings();
    [SerializeField] private BasicAttackSettings _basicAttack = new BasicAttackSettings();

    public CombatConfiguration()
    {
    }

    public CombatConfiguration(
        VitalitySettings vitality,
        DefenseSettings defense,
        BasicAttackSettings basicAttack)
    {
        _vitality = vitality ?? new VitalitySettings();
        _defense = defense ?? new DefenseSettings();
        _basicAttack = basicAttack ?? new BasicAttackSettings();
        Clamp();
    }

    public VitalitySettings Vitality => _vitality;

    public DefenseSettings Defense => _defense;

    public BasicAttackSettings BasicAttack => _basicAttack;

    public void Clamp()
    {
        _vitality.Clamp();
        _defense.Clamp();
        _basicAttack.Clamp();
    }

    [Serializable]
    public sealed class VitalitySettings
    {
        [SerializeField] private int _maxHealth = 30;

        public VitalitySettings()
        {
        }

        public VitalitySettings(int maxHealth)
        {
            _maxHealth = maxHealth;
            Clamp();
        }

        public int MaxHealth => _maxHealth;

        public void Clamp()
        {
            _maxHealth = ClampNonNegative(_maxHealth);
        }
    }

    [Serializable]
    public sealed class DefenseSettings
    {
        [SerializeField] private int _defense;
        [SerializeField] private int _criticalDefense;

        public DefenseSettings()
        {
        }

        public DefenseSettings(int defense, int criticalDefense)
        {
            _defense = defense;
            _criticalDefense = criticalDefense;
            Clamp();
        }

        public int Defense => _defense;

        public int CriticalDefense => _criticalDefense;

        public void Clamp()
        {
            _defense = ClampNonNegative(_defense);
            _criticalDefense = ClampNonNegative(_criticalDefense);
        }
    }

    [Serializable]
    public sealed class BasicAttackSettings
    {
        [SerializeField] private int _damage = 10;
        [SerializeField] private float _criticalChance = 0.1f;
        [SerializeField] private float _criticalDamageMultiplier = 1.5f;
        [SerializeField] private float _cooldown = 0.5f;
        [SerializeField] private float _activeTime = 0.2f;

        public BasicAttackSettings()
        {
        }

        public BasicAttackSettings(
            int damage,
            float criticalChance,
            float criticalDamageMultiplier,
            float cooldown,
            float activeTime)
        {
            _damage = damage;
            _criticalChance = criticalChance;
            _criticalDamageMultiplier = criticalDamageMultiplier;
            _cooldown = cooldown;
            _activeTime = activeTime;
            Clamp();
        }

        public int Damage => _damage;

        public float CriticalChance => _criticalChance;

        public float CriticalDamageMultiplier => _criticalDamageMultiplier;

        public float Cooldown => _cooldown;

        public float ActiveTime => _activeTime;

        public void Clamp()
        {
            _damage = ClampNonNegative(_damage);
            _criticalChance = Mathf.Clamp01(_criticalChance);
            _criticalDamageMultiplier = ClampNonNegative(_criticalDamageMultiplier);
            _cooldown = ClampNonNegative(_cooldown);
            _activeTime = ClampNonNegative(_activeTime);
        }
    }

    private static int ClampNonNegative(int value)
    {
        return value < 0 ? 0 : value;
    }

    private static float ClampNonNegative(float value)
    {
        return value < 0f ? 0f : value;
    }
}
