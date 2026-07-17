using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Editor/desktop attack input. Press Space to request a basic attack.
/// </summary>
public sealed class KeyboardAttackInputSource : MonoBehaviour, IAttackInputSource
{
    public bool ConsumeBasicAttackPressed()
    {
        if (Keyboard.current == null)
        {
            return false;
        }

        return Keyboard.current.spaceKey.wasPressedThisFrame;
    }
}
