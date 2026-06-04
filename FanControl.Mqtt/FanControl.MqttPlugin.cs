using System.Text;
using System.Text.Json;
using FanControl.Plugins;
using MQTTnet;
using System.Buffers;

namespace FanControl.Mqtt
{
  public class MqttPlugin(IPluginDialog dialog, IPluginLogger logger) : IPlugin
  {
    public string Name => "MQTT Bridge";

    private IMqttClient? _mqttClient;
    private MqttConfig? _config;
    private readonly string _configPath = "MqttConfig.json";

    public void Initialize()
    {
      if (!File.Exists(_configPath))
      {
        dialog.ShowMessageDialog($"Configuration manquante. Créer {_configPath}.");
        return;
      }
      _config = JsonSerializer.Deserialize<MqttConfig>(File.ReadAllText(_configPath));
      Task.Run(ConnectToBrokerAsync);
    }

    private async Task ConnectToBrokerAsync()
    {
      var factory = new MqttClientFactory();
      _mqttClient = factory.CreateMqttClient();

      var options = new MqttClientOptionsBuilder()
          .WithTcpServer(_config!.BrokerIp, _config.Port)
          .WithClientId($"FanControl_{Environment.MachineName}")
          .WithTlsOptions(o => o.WithCertificateValidationHandler(_ => true))
          .WithCredentials(_config.Username, _config.Password)
          .Build();

      // Gestion de la réception (lecture des topics HA)
      _mqttClient.ApplicationMessageReceivedAsync += e =>
      {
        string topic = e.ApplicationMessage.Topic;

        // On convertit la séquence en tableau, puis en string
        string payload = Encoding.UTF8.GetString(e.ApplicationMessage.Payload.ToArray());

        logger.Log($"MQTT Rx: {topic} -> {payload}");

        // ...
        return Task.CompletedTask;
      };

      await _mqttClient.ConnectAsync(options, CancellationToken.None);

      // Souscription aux topics listés dans le config
      foreach (var topic in _config.SubscribedTopics)
      {
        await _mqttClient.SubscribeAsync(topic);
      }
    }

    public void Load(IPluginSensorsContainer container)
    {
      // Auto-Discovery : Publication différée après connexion
      Task.Run(async () =>
      {
        while (_mqttClient == null || !_mqttClient.IsConnected) await Task.Delay(1000);

        foreach (var sensor in container.TempSensors)
        {
          string sensorId = sensor.Id.ToLower().Replace(" ", "_");
          string configTopic = $"homeassistant/sensor/{_config!.BaseTopic}/{sensorId}/config";

          var payload = new HaDiscoveryPayload(
              sensor.Name,
              $"{_config.BaseTopic}_{sensorId}",
              $"{_config.BaseTopic}/sensor/{sensorId}/state",
              "°C",
              "temperature"
          );

          string json = JsonSerializer.Serialize(payload);
          await _mqttClient.PublishAsync(new MqttApplicationMessageBuilder()
              .WithTopic(configTopic)
              .WithPayload(json)
              .WithRetainFlag() // CRUCIAL pour HA
              .Build());
        }
      });
    }

    // Appel cette méthode régulièrement (ex: via un Timer ou dans ta boucle de rendu)
    public async Task PublishSensorValue(IPluginSensor sensor)
    {
      if (_mqttClient == null || !_mqttClient.IsConnected) return;

      string sensorId = sensor.Id.ToLower().Replace(" ", "_");
      string topic = $"{_config!.BaseTopic}/sensor/{sensorId}/state";

      await _mqttClient.PublishAsync(new MqttApplicationMessageBuilder()
          .WithTopic(topic)
          .WithPayload(sensor.Value?.ToString("F1") ?? "0")
          .Build());
    }

    public void Close() => _mqttClient?.Dispose();
  }
}