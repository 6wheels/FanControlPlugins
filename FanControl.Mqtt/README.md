# FanControl.Mqtt

[![Build](https://github.com/6wheels/FanControlPlugins/actions/workflows/release.yml/badge.svg)](https://github.com/6wheels/FanControlPlugins/actions/workflows/release.yml) [![Coverage](https://codecov.io/gh/6wheels/FanControlPlugins/branch/main/graph/badge.svg?flag=FanControl.Mqtt)](https://codecov.io/gh/6wheels/FanControlPlugins)

A FanControl plugin that bridges your hardware sensors to an MQTT broker. It publishes FanControl temperature sensors as MQTT topics and advertises them to **Home Assistant** through its [MQTT auto-discovery](https://www.home-assistant.io/integrations/mqtt/#mqtt-discovery) protocol, so each sensor shows up automatically in your HA dashboard.

## Features
* **Home Assistant Auto-Discovery:** Each FanControl temperature sensor is published as a retained `homeassistant/sensor/.../config` entity — no manual HA configuration required.
* **State Publishing:** Sensor values are pushed to `<BaseTopic>/sensor/<id>/state`.
* **Topic Subscriptions:** Optionally subscribe to inbound topics for logging/automation hooks.
* **Authentication & TLS:** Connects with username/password and supports TLS brokers.

## ⚙️ Prerequisites

1. A reachable **MQTT broker** (e.g. [Mosquitto](https://mosquitto.org/), or the Home Assistant Mosquitto add-on).
2. *(Optional)* Home Assistant with the MQTT integration enabled to consume auto-discovered sensors.

## 📝 Configuration

On startup the plugin reads a `MqttConfig.json` file in your FanControl folder. If the file is missing, a dialog prompts you to create it.

### Example `MqttConfig.json`

```json
{
  "BrokerIp": "192.168.1.10",
  "Port": 1883,
  "Username": "fancontrol",
  "Password": "secret",
  "BaseTopic": "fancontrol",
  "UseTls": false,
  "SubscribedTopics": []
}
```

| Field | Description |
| --- | --- |
| `BrokerIp` | Hostname or IP of the MQTT broker. |
| `Port` | Broker port (default `1883`, or `8883` for TLS). |
| `Username` / `Password` | Broker credentials. |
| `BaseTopic` | Prefix used for state topics and discovery unique IDs. |
| `UseTls` | Enable a TLS connection to the broker. |
| `SubscribedTopics` | List of topics the plugin subscribes to (received messages are logged). |

## 📚 Dependencies

This project uses:
- **[MQTTnet](https://github.com/dotnet/MQTTnet) 5.1.0.1559** — MIT license — embedded into the output `.dll` via Costura.Fody (bundling permitted by MIT).
- **[FanControl.Plugins](https://github.com/Rem0o/FanControl.Releases)** — the host plugin API (proprietary; supplied by FanControl at runtime, **not** redistributed).
