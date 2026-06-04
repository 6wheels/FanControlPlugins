using System.Diagnostics;
using FanControl.Plugins;

namespace FanControl.SystemMetrics
{
  public class MetricSensor : IPluginSensor
  {
    public string Id { get; }
    public string Name { get; }
    public float? Value { get; private set; }

    // Ajout de readonly et utilisation de la syntaxe de collection C# 12
    private readonly PerformanceCounter[] _counters = [];
    private readonly PerformanceCounter? _singleCounter;
    private readonly bool _isGpu;

    public MetricSensor(string id, string name, string category, string counterName, string instance, bool isGpuCounter = false)
    {
      Id = id;
      Name = name;
      _isGpu = isGpuCounter;

      try
      {
        if (_isGpu)
        {
          PerformanceCounterCategory cat = new(category);

          // Ajout du StringComparison exigé par les linters
          var instanceNames = cat.GetInstanceNames()
                                 .Where(x => x.EndsWith(instance, StringComparison.OrdinalIgnoreCase));

          _counters = instanceNames.Select(inst => new PerformanceCounter(category, counterName, inst)).ToArray();
          foreach (PerformanceCounter c in _counters)
          {
            c.NextValue();
          }
        }
        else
        {
          _singleCounter = new PerformanceCounter(category, counterName, instance);
          _singleCounter.NextValue();
        }
      }
      catch
      {
        // Un compteur introuvable ne doit pas crasher le plugin au démarrage
      }
    }

    public void Update()
    {
      try
      {
        if (_isGpu)
        {
          float totalGpu = 0;
          foreach (PerformanceCounter c in _counters)
          {
            try { totalGpu += c.NextValue(); } catch { }
          }
          Value = Math.Clamp(totalGpu, 0f, 100f);
        }
        else if (_singleCounter != null)
        {
          Value = _singleCounter.NextValue();
        }
      }
      catch
      {
        Value = 0f; // Valeur de repli sécurisée
      }
    }
  }
}