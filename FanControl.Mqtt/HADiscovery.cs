namespace FanControl.Mqtt
{
  public record HaDiscoveryPayload(
      string Name,
      string Unique_id,
      string State_topic,
      string Unit_of_measurement,
      string Device_class
  );
}