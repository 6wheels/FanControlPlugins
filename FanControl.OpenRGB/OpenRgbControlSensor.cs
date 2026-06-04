using FanControl.Plugins;

namespace FanControl.OpenRGB
{
  public class OpenRgbControlSensor(string id, string name) : IPluginControlSensor
  {
    public string Id { get; } = id;
    public string Name { get; } = name;
    public float? Value { get; private set; } = 0f;

    // FanControl appelle cette méthode pour donner la valeur (soit via le slider manuel, soit via une courbe)
    public void Set(float val) => Value = val;

    public void Reset() => Value = 0f;

    public void Update() { } // Pas besoin d'update, FanControl pousse la valeur via Set()
  }
}