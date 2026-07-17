using UnityEngine;

/// <summary>
/// Presentation-only flash feedback for combat events. Does not affect gameplay.
/// </summary>
public sealed class CombatDebugFeedback : MonoBehaviour
{
    [SerializeField] private CharacterCombat _combat;
    [SerializeField] private SpriteRenderer _spriteRenderer;
    [SerializeField] private Color _attackFlashColor = new Color(1f, 0.92f, 0.2f, 1f);
    [SerializeField] private Color _damageFlashColor = new Color(1f, 0.25f, 0.25f, 1f);
    [SerializeField] private float _flashDuration = 0.08f;

    private Color _originalColor;
    private float _flashRemaining;

    private void Awake()
    {
        if (_combat == null)
        {
            _combat = GetComponent<CharacterCombat>();
        }

        if (_spriteRenderer == null)
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (_spriteRenderer != null)
        {
            _originalColor = _spriteRenderer.color;
        }
    }

    private void OnEnable()
    {
        if (_combat == null)
        {
            return;
        }

        _combat.AttackStarted += OnAttackStarted;
        _combat.DamageApplied += OnDamageApplied;
    }

    private void OnDisable()
    {
        if (_combat == null)
        {
            return;
        }

        _combat.AttackStarted -= OnAttackStarted;
        _combat.DamageApplied -= OnDamageApplied;
    }

    private void Update()
    {
        if (_spriteRenderer == null || _flashRemaining <= 0f)
        {
            return;
        }

        _flashRemaining -= Time.deltaTime;
        if (_flashRemaining <= 0f)
        {
            _spriteRenderer.color = _originalColor;
            _flashRemaining = 0f;
        }
    }

    private void OnAttackStarted()
    {
        Flash(_attackFlashColor);
    }

    private void OnDamageApplied(DamageResult _)
    {
        Flash(_damageFlashColor);
    }

    private void Flash(Color flashColor)
    {
        if (_spriteRenderer == null)
        {
            return;
        }

        _spriteRenderer.color = flashColor;
        _flashRemaining = _flashDuration;
    }
}
