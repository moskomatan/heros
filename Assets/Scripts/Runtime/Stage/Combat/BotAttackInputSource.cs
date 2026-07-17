using UnityEngine;

/// <summary>
/// Bot attack input. Requests a basic attack while the chase target is within range.
/// BasicAttackRuntime cooldown rate-limits repeated requests.
/// </summary>
public sealed class BotAttackInputSource : MonoBehaviour, IAttackInputSource
{
    [SerializeField] private float _attackRange = 1.5f;
    [SerializeField] private BotMovementInputSource _chaseTargetSource;

    public float AttackRange => _attackRange;

    private void Awake()
    {
        ResolveChaseTargetSource();
    }

    private void OnValidate()
    {
        _attackRange = Mathf.Max(0f, _attackRange);
        ResolveChaseTargetSource();
    }

    public bool ConsumeBasicAttackPressed()
    {
        ResolveChaseTargetSource();
        if (_chaseTargetSource == null)
        {
            return false;
        }

        Transform target = _chaseTargetSource.Target;
        if (target == null)
        {
            return false;
        }

        float range = Mathf.Max(0f, _attackRange);
        Vector2 offset = target.position - transform.position;
        return offset.sqrMagnitude <= range * range;
    }

    private void ResolveChaseTargetSource()
    {
        if (_chaseTargetSource == null)
        {
            _chaseTargetSource = GetComponent<BotMovementInputSource>();
        }
    }
}
