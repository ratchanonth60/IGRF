# IGRF Interface

<div align="center">

![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)
![Avalonia](https://img.shields.io/badge/Avalonia-UI-8B5CF6?logo=avalonia)
![License](https://img.shields.io/badge/License-MIT-green)
![Platform](https://img.shields.io/badge/Platform-Windows-blue)

**แอปพลิเคชันสำหรับอ่านข้อมูลจาก Magnetometer และคำนวณ IGRF Model**

[Features](#-features) • [Installation](#-installation) • [Usage](#-usage) • [Architecture](#-architecture)

</div>

---

## 📖 Overview

IGRF Interface เป็นแอปพลิเคชัน Desktop สำหรับ:

- 📡 **อ่านข้อมูลจาก Magnetometer** (MFG Digital Fluxgate) ผ่าน Serial Port หรือ TCP/IP
- 🌍 **คำนวณ IGRF Model** (International Geomagnetic Reference Field) 
- 🛰️ **ติดตามตำแหน่งดาวเทียม** แบบ Real-time ด้วย TLE (Two-Line Element)
- 📊 **แสดงผลกราฟ** สนามแม่เหล็กแบบ Real-time พร้อม Kalman Filtering
- 🎮 **ควบคุม PID** สำหรับ Magnetic Field Compensation

---

## ✨ Features

### 🔬 Sensor Support
- **Generic Serial Sensor** - รองรับ Modbus-like protocol
- **MFG Magnetometer** - Magson MFG Digital Fluxgate (Dual Sensors)
- รองรับทั้ง **Serial Port** และ **TCP/IP** connection

### 📈 Signal Processing
- **Kalman Filter** - กรองสัญญาณรบกวนแบบ Optimal
- **PID Controller** - ควบคุมแบบ Proportional-Integral-Derivative
- **Real-time Plotting** - แสดงกราฟ X, Y, Z ด้วย ScottPlot

### 🛰️ Satellite Tracking
- **TLE Parser** - อ่านข้อมูล Two-Line Element
- **SGP4 Propagator** - คำนวณตำแหน่งดาวเทียมแบบ Real-time
- **Space-Track API** - ดึงข้อมูล TLE อัตโนมัติ
- **IGRF Calculation** - คำนวณสนามแม่เหล็กโลกจากตำแหน่ง

### 🌐 3D Visualization (Globe3D)
- **โลก 3D** พร้อม Earth texture
- **Magnetic Field Lines** แสดง Dipole field
- **Satellite Orbit** visualization
- **Real-time Position** sync จาก Main App

---

## 📦 Projects

| Project | Description | Framework |
|---------|-------------|-----------|
| **IGRF.Avalonia** | Main UI Application | Avalonia UI |
| **IGRF.Core** | Algorithms & Services | .NET 10 Library |
| **IGRF.Globe3D** | 3D Earth Visualization | WPF + HelixToolkit |

---

## 🚀 Installation

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Windows 10/11

### Build from Source
```bash
# Clone repository
git clone https://github.com/ratchanonth60/IGRF.git
cd IGRF

# Restore dependencies
dotnet restore

# Build all projects
dotnet build

# Run main application
dotnet run --project IGRF.Avalonia
```

---

## 🎯 Usage

### 1. Connect Sensor

**Serial Port Mode:**
1. เลือก COM Port จาก Dropdown
2. ตั้งค่า Baud Rate (default: 9600)
3. คลิก "Connect"

**TCP/IP Mode (MFG):**
1. ใส่ IP Address ของ MFG Sensor
2. ใส่ Port (default: 12345)
3. คลิก "Connect MFG"

### 2. View Data

- **Dashboard** - ภาพรวมข้อมูล Sensor
- **Sensor Info** - ข้อมูลละเอียด X, Y, Z
- **Tuning** - ปรับค่า PID และ Kalman Filter
- **Satellite** - เลือกและติดตามดาวเทียม
- **Debug** - Raw data และ Console

### 3. Data Logging

1. ไปที่หน้า Settings
2. เลือก Log Path
3. เปิด "Enable Logging"
4. ข้อมูลจะบันทึกเป็น CSV

---

## 🏗️ Architecture

```
IGRF Interface Demo1.1/
├── IGRF.Avalonia/           # Main UI Application
│   ├── ViewModels/          # MVVM ViewModels
│   ├── Views/               # AXAML Views
│   ├── Services/            # Navigation, Pipe services
│   └── Common/              # Converters, Constants
│
├── IGRF.Core/               # Core Library
│   ├── Core/
│   │   ├── Algorithms/      # KalmanFilter, PidController
│   │   ├── Models/          # Data models
│   │   ├── Services/        # Calculation, Satellite, Sensor
│   │   └── Interfaces/      # Service abstractions
│   └── Infrastructure/
│       ├── Communication/   # Serial, TCP managers
│       ├── Interfaces/      # Communication abstractions
│       └── Utilities/       # CRC, MFG Parser
│
├── IGRF.Globe3D/            # 3D Visualization (WPF)
│   ├── MainWindow.xaml      # 3D Globe view
│   ├── Services/            # Magnetic data, Pipe client
│   └── assets/              # Earth texture, 3D models
│
├── magnetic/                # Geomagnetic data files
│   ├── declinationData.txt
│   ├── inclinationData.txt
│   └── intensityData.txt
│
└── docs/                    # Documentation
    └── Magson_MFG_Manual.md
```

---

## 🔧 Configuration

การตั้งค่าจะบันทึกอัตโนมัติใน:
```
%LOCALAPPDATA%/IGRF_Demo/config.json
```

### ตัวอย่าง Config:
```json
{
  "sensorType": "MFG",
  "comPort": "COM3",
  "baudRate": 9600,
  "mfgIpAddress": "192.168.1.100",
  "mfgPort": 12345,
  "kalmanR": [1.0, 1.0, 1.0],
  "pidGains": {
    "x": { "kp": 1.0, "ki": 0.1, "kd": 0.05 }
  }
}
```

---

## 📚 Algorithm Documentation

### Kalman Filter
กรองสัญญาณรบกวนจาก Sensor แบบ Optimal:
- **Q (Process Noise)**: ความไม่แน่นอนของระบบ
- **R (Measurement Noise)**: ความผิดพลาดของ Sensor
- ค่า R/Q ratio สูง = output นุ่มนวลขึ้น

### PID Controller
ควบคุมสนามแม่เหล็กให้ตรงกับ Setpoint:
- **Kp (Proportional)**: ตอบสนองต่อ Error ปัจจุบัน
- **Ki (Integral)**: กำจัด Steady-state error
- **Kd (Derivative)**: ลด Overshoot

---

## 🛠️ Development

### Tech Stack
- **UI Framework**: Avalonia UI 11
- **MVVM**: CommunityToolkit.Mvvm
- **Charts**: ScottPlot
- **3D Graphics**: HelixToolkit.Wpf
- **JSON**: System.Text.Json

### Build Requirements
```bash
# Restore and build
dotnet build

# Run tests (if available)
dotnet test

# Create release build
dotnet publish -c Release
```

---

## 📝 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

## 👥 Contributors

- **ILRS Team** - Development & Maintenance

---

<div align="center">

**Made with ❤️ for Geomagnetic Research**

</div>
