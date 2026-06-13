using FanControl.Plugins;

namespace FanControl.SystemMetrics.Tests.Plugin;

internal sealed class FakeLogger : IPluginLogger
{
    public List<string> Messages { get; } = [];
    public void Log(string message) => Messages.Add(message);
}

internal sealed class FakeDialog : IPluginDialog
{
    public List<string> Messages { get; } = [];
    public Task ShowMessageDialog(string message)
    {
        Messages.Add(message);
        return Task.CompletedTask;
    }
}

internal sealed class FakeContainer : IPluginSensorsContainer
{
    public List<IPluginControlSensor> ControlSensors { get; } = [];
    public List<IPluginSensor> TempSensors { get; } = [];
    public List<IPluginSensor> FanSensors { get; } = [];
}
