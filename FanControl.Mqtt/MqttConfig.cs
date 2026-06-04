namespace FanControl.Mqtt
{
  public class MqttConfig
  {
    public string BrokerIp { get; set; } = string.Empty;
    public int Port { get; set; } = 1883;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string BaseTopic { get; set; } = "fancontrol";
    public bool UseTls { get; set; } = false;
    public List<string> SubscribedTopics { get; set; } = [];
  }
}