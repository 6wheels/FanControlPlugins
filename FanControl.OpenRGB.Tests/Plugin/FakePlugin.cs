using FanControl.Plugins;

namespace FanControl.OpenRGB.Tests.Plugin;

internal sealed class FakeLogger : IPluginLogger
{
    public void Log(string message) { }
}

internal sealed class FakeDialog : IPluginDialog
{
    public Task ShowMessageDialog(string message) => Task.CompletedTask;
}

internal sealed class FakeContainer : IPluginSensorsContainer
{
    public List<IPluginControlSensor> ControlSensors { get; } = [];
    public List<IPluginSensor> TempSensors { get; } = [];
    public List<IPluginSensor> FanSensors { get; } = [];
}
