# Plugin: MQTT
Publishes/subscribes sensor data over MQTT.

## Runtime dependency
- Requires a reachable MQTT broker.

## Resource & error handling
- The MQTT client must be disposed via `IDisposable`/`using`.
- Wrap connect/publish/subscribe operations in `try-catch`; never let a broker
  outage throw into the host.
