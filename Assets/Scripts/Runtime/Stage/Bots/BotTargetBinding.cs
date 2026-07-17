using UnityEngine;

public sealed class BotTargetBinding : MonoBehaviour
{
    private BotTargetRunner _runner;
    private BotTargetController _controller;
    private bool _isInitialized;

    private void OnEnable()
    {
        if (_isInitialized && _runner != null && _controller != null)
        {
            _runner.Register(_controller);
        }
    }

    private void OnDisable()
    {
        UnregisterIfInitialized();
    }

    private void OnDestroy()
    {
        UnregisterIfInitialized();
    }

    public void Initialize(BotTargetRunner runner, BotTargetController controller)
    {
        if (runner == null)
        {
            Debug.LogError($"{nameof(BotTargetBinding)} on {name} received a null runner.", this);
            return;
        }

        if (controller == null)
        {
            Debug.LogError($"{nameof(BotTargetBinding)} on {name} received a null controller.", this);
            return;
        }

        if (_isInitialized)
        {
            if (_runner == runner && _controller == controller)
            {
                if (isActiveAndEnabled)
                {
                    _runner.Register(_controller);
                }

                return;
            }

            Debug.LogError(
                $"{nameof(BotTargetBinding)} on {name} was already initialized with different dependencies.",
                this);
            return;
        }

        _runner = runner;
        _controller = controller;
        _isInitialized = true;

        if (isActiveAndEnabled)
        {
            _runner.Register(_controller);
        }
    }

    private void UnregisterIfInitialized()
    {
        if (_isInitialized && _runner != null && _controller != null)
        {
            _runner.Unregister(_controller);
        }
    }
}
