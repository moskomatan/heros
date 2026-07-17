using UnityEngine;

/// <summary>
/// Mobile/UI attack input. Wire a UI Button OnClick to <see cref="NotifyBasicAttackPressed"/>.
/// </summary>
public sealed class UiButtonAttackInputSource : MonoBehaviour, IAttackInputSource
{
    private bool _basicAttackPressed;

    public void NotifyBasicAttackPressed()
    {
        _basicAttackPressed = true;
    }

    public bool ConsumeBasicAttackPressed()
    {
        if (!_basicAttackPressed)
        {
            return false;
        }

        _basicAttackPressed = false;
        return true;
    }
}
