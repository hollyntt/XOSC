# XOSC - VRChat OSC Tool

![Preview](https://github.com/hollyntt/XOSC/blob/master/Product%20Images/Screenshot_20260418_225445.png?raw=true)

XOSC is a high-performance, native C# OSC manager for VRChat. It provides a sleek, resource-efficient ImGui-based interface to manage your chatbox, music, and hardware telemetry.

## ✨ Features
*   **Cross-Platform:** Full support for Windows (10/11) and Linux (Fedora, Arch, Steam Deck).
*   **Smart Music Scraper:** Native integration with Windows Media Transport Controls (SoundCloud-RPC, Spotify, etc.) and Linux `playerctl`/`xdotool`.
*   **Hardware Telemetry:** Real-time monitoring for CPU, GPU (NVIDIA/AMD), RAM, and VRAM.
*   **Status Manager:** Add, edit, and cycle through custom statuses with auto-cycle support.
*   **VRChat Focused:** Lightweight, minimal CPU footprint, and designed to fit perfectly in the VRChat chatbox.
*   **Emergency Alerts:** Built-in weather alert integration.

---

## 🛠️ Prerequisites

### Windows
*   **Media:** No extra apps required — Windows Media Transport Controls handle Spotify, SoundCloud, etc.
*   **Hardware sensors (CPU package temp / wattage, motherboard sensors):** Install **[PawnIO](https://pawnio.eu/)** once (signed kernel driver used by LibreHardwareMonitor 0.9.5+). Without it, CPU load and GPU stats still work; package temp/power may show as `--`.
*   Run XOSC **as Administrator** when CPU temp/power stay at `--`. The Dashboard shows PawnIO status and an admin relaunch button.

### Linux
Your system needs the following tools to handle hardware and music scraping:
*   **Fedora:** `sudo dnf install playerctl xdotool lm_sensors`
*   **Arch/Steam Deck:** `sudo pacman -S playerctl xdotool lm_sensors`

---

## 🚀 Installation

1. Download the latest release for your platform from the [Releases Page](https://github.com/hollyntt/XOSC/releases/).
2. **Windows:**
   1. Install [PawnIO](https://pawnio.eu/) if you want full CPU temp/power readings.
   2. Run `XOSC.exe` (preferably as Administrator for hardware sensors).
3. **Linux:**
   ```bash
   chmod +x XOSC
   ./XOSC
   ```

---

## 🎮 VRChat Setup

### Enable OSC
1. Open VRChat.
2. Open your **Action Menu** (Radial Menu).
3. Go to **Options** > **OSC**.
4. Set **Enabled** to **ON**.

### Important: Real-time Logs
To ensure XOSC can scrape hardware names and AFK status correctly from VRChat logs, add this to your VRChat **Launch Options** in Steam:
```text
-log-file-buffer-size 0
```

---

## ⚙️ Configuration
XOSC saves your preferences automatically:
*   **Windows:** `%APPDATA%\xosc\config.json`
*   **Linux:** `~/.config/xosc/config.json`

## 🤝 Contributing
XOSC is built for the VRChat community. Feel free to fork the repo, submit issues, or create pull requests!

---

### ⚠️ Note for Steam Deck Users
XOSC automatically handles the file paths for both native Steam and Flatpak installations of VRChat to ensure your logs and hardware data are always detected correctly.