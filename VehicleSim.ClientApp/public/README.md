# VDA5050 Vehicle Simulator

A **VDA 5050 compliant** simulation engine for testing and validating AGV/AMR fleet management systems.

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![React](https://img.shields.io/badge/React-19-61DAFB?logo=react)](https://react.dev/)
[![MQTT](https://img.shields.io/badge/MQTT-5.0-660066?logo=eclipsemosquitto)](https://mqtt.org/)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

## Overview

This simulator provides a complete testing environment for **VDA 5050** compliant vehicle control systems. It simulates Unmanned Autonomous Ground Vehicles (uAGVs) that communicate via MQTT, enabling developers to test fleet management software without physical hardware or heavy emulation.

---

### Key Features

| Feature                 | Description                                                         |
| ----------------------- | ------------------------------------------------------------------- |
| **VDA 5050 Compliance** | Full protocol support for orders, state, connection & visualization |
| **Real-time Updates**   | SignalR-powered live UI updates                                     |
| **Time Scaling**        | Adjustable simulation speed (1x - 100x)                             |
| **Multi-Vehicle**       | Simulate entire fleets simultaneously                               |
| **Error Injection**     | Test error handling with WARNING/FATAL states                       |
| **Hot Reload**          | Add/remove vehicles at runtime                                      |

---

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js 20+](https://nodejs.org/)
- MQTT Broker (e.g., [Mosquitto](https://mosquitto.org/))

### 1. Start MQTT Broker

**Using Docker:**

```bash
docker run -d -p 1883:1883 eclipse-mosquitto
```

**Or install locally:**

#### Windows

[Download Mosquitto Installer](https://mosquitto.org/files/binary/win64/mosquitto-2.0.22-install-windows-x64.exe)

#### macOS

```bash
brew install mosquitto
```

#### Linux

```bash
snap install mosquitto
```

#### Topics (per uAGV)

| Topic                                       | Description       |
| ------------------------------------------- | ----------------- |
| `uagv/v2/movu/{SerialNumber}/order`         | Incoming orders   |
| `uagv/v2/movu/{SerialNumber}/state`         | State updates     |
| `uagv/v2/movu/{SerialNumber}/connection`    | Connection status |
| `uagv/v2/movu/{SerialNumber}/visualization` | Position updates  |

---

### 2. Run the .NET Backend

```bash
cd VehicleSimulator
dotnet build
dotnet run --project VehicleSim.WebHost
```

---

### 3. Run the React Frontend

```bash
cd ./VehicleSim.ClientApp
npm install
npm run dev
```

---

## Project Structure

```
VehicleSimulator/
├── VehicleSim.Core/            # 0 dependencies - VDA models and Vehicle class
├── VehicleSim.Core.Tests/      # Unit tests for Vehicle class
├── VehicleSim.Application/     # Simulation engine & fleet manager
├── VehicleSim.Infrastructure/  # MQTT contact/entrypoint
├── VehicleSim.UI/              # SignalR notification service
├── VehicleSim.WebHost/         # Program entrypoint
├── VehicleSim.ClientApp/       # React UI project
├── VdaOrders.txt               # VDA5050 compliant test orders
└── appsettings.json            # Settings and initial vehicles config
```

> **Note:** Since there's no database, vehicles are instantiated via `appsettings.json` or dynamically through the UI at runtime.

---

## Features

### Simulation

- VDA5050 compliant vehicle driving simulation
- Send orders on `/order` topic
- Receive state updates on `/state` topic
- Receive connection updates on `/connection` topic
- Receive frequent position updates on `/visualization`

### Fleet Management

- Initialize multiple vehicles via `appsettings.json` (pre-runtime)
- Add/remove vehicles via UI (at-runtime)
- Set MQTT connection to offline when removing vehicles

### Testing & Control

- Inject Fatal and Warning errors
- Publish orders (stitching possible)
- Soft reset a vehicle (reset errors, continue path)
- Hard reset simulation (reset all vehicles to starting values)
- Adjustable simulation timescale for faster/slower driving

---

## External Tools

### MQTT Explorer

For visualizing MQTT data flow and publishing orders to a uagv's /order topic.

🔗 [MQTT_Explorer](https://mqtt-explorer.com/)

### VDA5050 Visualizer

For visualizing the simulator, you can use an external visualizer that listens on MQTT. Use this to validate orders and simulator behaviour:

🔗 [vda5050_visualizer](https://github.com/bekirbostanci/vda5050_visualizer)
