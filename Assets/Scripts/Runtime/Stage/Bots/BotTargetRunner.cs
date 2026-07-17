using System.Collections.Generic;

public sealed class BotTargetRunner
{
    private readonly IList<BotTargetController> _controllers = new List<BotTargetController>();

    public void Register(BotTargetController controller)
    {
        if (controller == null)
        {
            return;
        }

        if (_controllers.Contains(controller))
        {
            return;
        }

        _controllers.Add(controller);
    }

    public void Unregister(BotTargetController controller)
    {
        if (controller == null)
        {
            return;
        }

        if (_controllers.Remove(controller))
        {
            controller.Stop();
        }
    }

    public void Tick(float deltaTime)
    {
        foreach (BotTargetController controller in _controllers)
        {
            controller.Tick(deltaTime);
        }
    }

    public void StopAll()
    {
        foreach (BotTargetController controller in _controllers)
        {
            controller.Stop();
        }

        _controllers.Clear();
    }
}
