using FanControl.Plugins;

namespace FanControl.OpenRGB
{
  public class OpenRgbControlSensor(string id, string name) : IPluginControlSensor
  {
    public string Id { get; } = id;
    public string Name { get; } = name;
    public float? Value { get; private set; } = 0f;

    // FanControl calls this method to give the value (either via the manual slider or via a curve)
    public void Set(float val) => Value = val;

    public void Reset() => Value = 0f;

    public void Update() { } // No need for update, FanControl pushes the value via Set()
  }
}