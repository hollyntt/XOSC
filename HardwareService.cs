using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

#if WINDOWS_BUILD
using LibreHardwareMonitor.Hardware;
#endif

namespace XOSC
{
    public static class HardwareService
    {
        public static string CpuLoad    { get; private set; } = "--%";
        public static string GpuLoad    { get; private set; } = "--%";
        public static string RamUsed    { get; private set; } = "-- GB";
        public static string RamTotal   { get; private set; } = "-- GB";
        public static string RamDdr     { get; private set; } = "";
        public static string VramUsed   { get; private set; } = "-- GB";
        public static string VramTotal  { get; private set; } = "-- GB";
        public static string CpuTemp    { get; private set; } = "--°C";
        public static string CpuPower   { get; private set; } = "--W";
        public static string GpuTemp    { get; private set; } = "--°C";
        public static string GpuHotspot { get; private set; } = "--°C";
        public static string GpuPower   { get; private set; } = "--W";
#if WINDOWS_BUILD
        private static Computer _computer = null!;
        private static bool _initialized;
        private static IHardware? _cpu;
        private static IHardware? _gpu;
        private static IHardware? _igpu;
        private static IHardware? _ram;
#endif

        public static void Initialize()
        {
#if WINDOWS_BUILD
            if (_initialized) return;
            try
            {
                _computer = new Computer
                {
                    IsCpuEnabled         = true,
                    IsGpuEnabled         = true,
                    IsMemoryEnabled      = true,
                    IsMotherboardEnabled = true,
                    IsControllerEnabled  = true
                };
                _computer.Open();
                _computer.Accept(new UpdateVisitor());
                _cpu  = _computer.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.Cpu);
                var gpus = _computer.Hardware.Where(h =>
                    h.HardwareType == HardwareType.GpuNvidia ||
                    h.HardwareType == HardwareType.GpuAmd    ||
                    h.HardwareType == HardwareType.GpuIntel).ToList();
                string[] igpuKeywords = { "integrated ", "radeon(tm) graphics ", "radeon graphics ", "vega ", "uhd graphics ", "iris xe " };
                _gpu  = gpus.FirstOrDefault(h => !igpuKeywords.Any(k => h.Name.Contains(k, StringComparison.OrdinalIgnoreCase)));
                if (_gpu == null) _gpu = gpus.FirstOrDefault();
                _igpu = gpus.FirstOrDefault(h => igpuKeywords.Any(k => h.Name.Contains(k, StringComparison.OrdinalIgnoreCase)));
                _ram   = _computer.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.Memory);
                RamDdr = GetDDRVersion();
                _initialized = true;
            }
            catch { }
#endif
        }

        public static void Close()
        {
#if WINDOWS_BUILD
            _computer?.Close();
            _initialized = false;
#endif
        }

        public static void Update()
        {
#if WINDOWS_BUILD
            UpdateWindows();
#else
            UpdateLinux();
#endif
        }

#if WINDOWS_BUILD
        private static void UpdateWindows()
        {
            if (!_initialized) return;
            _computer.Accept(new UpdateVisitor());
            if (_cpu != null)
            {
                CpuLoad  = GetHighestSensorValue(_cpu, SensorType.Load,        new[] { "CPU Total", "Total" },                    "--%",  v => $"{v:F0}%",  strict: false);
                CpuTemp  = GetHighestSensorValue(_cpu, SensorType.Temperature, new[] { "Package", "Core", "Tctl", "Tdie", "CCD" }, "--°C", v => $"{v:F0}°C", strict: false);
                if (CpuTemp == "--°C" && _igpu != null)
                    CpuTemp = GetHighestSensorValue(_igpu, SensorType.Temperature, new[] { "Core", "Package", "Hot Spot", "Hotspot" }, "--°C", v => $"{v:F0}°C", strict: false);
                CpuPower = GetHighestSensorValue(_cpu, SensorType.Power,       new[] { "Package", "Core", "PPT" },                "--W",  v => $"{v:F0}W",  strict: false);
                if (CpuPower == "--W" && _igpu != null)
                    CpuPower = GetHighestSensorValue(_igpu, SensorType.Power, new[] { "Package", "Core", "Power", "PPT" }, "--W", v => $"{v:F0}W", strict: false);
            }
            if (_gpu != null)
            {
                GpuLoad    = GetHighestSensorValue(_gpu, SensorType.Load,        new[] { "3D", "Core" },                                 "--% ", v => $"{v:F0}%",  strict: true);
                GpuTemp    = GetHighestSensorValue(_gpu, SensorType.Temperature, new[] { "GPU Core", "Core" },                           "--°C ", v => $"{v:F0}°C", strict: false);
                GpuHotspot = GetHighestSensorValue(_gpu, SensorType.Temperature, new[] { "Hot spot", "Hotspot" },                        "--°C ", v => $"{v:F0}°C", strict: true);
                GpuPower   = GetHighestSensorValue(_gpu, SensorType.Power,       new[] { "GPU Power", "Package", "Total Board", "PPT" }, "--W ",  v => $"{v:F0}W",  strict: false);
                float? vramUsed  = GetVramSensorValue(_gpu, new[] { "Dedicated Memory Used",  "Memory Used"  });
                float? vramTotal = GetVramSensorValue(_gpu, new[] { "Dedicated Memory Total", "Memory Total" });
                if (vramUsed.HasValue && vramTotal.HasValue)
                {
                    VramUsed  = $"{vramUsed.Value  / 1024f:F1} GB";
                    VramTotal = $"{vramTotal.Value / 1024f:F1} GB";
                }
            }
            var memStatus = new NativeMethods.MEMORYSTATUSEX();
            memStatus.dwLength = (uint)Marshal.SizeOf(memStatus);
            if (NativeMethods.GlobalMemoryStatusEx(ref memStatus))
            {
                RamUsed  = $"{(memStatus.ullTotalPhys - memStatus.ullAvailPhys) / (1024.0 * 1024.0 * 1024.0):F1} GB";
                RamTotal = $"{memStatus.ullTotalPhys / (1024.0 * 1024.0 * 1024.0):F1} GB";
            }
        }

        private static string GetDDRVersion()
        {
            try
            {
                var psi = new ProcessStartInfo("powershell", "-NoProfile -Command \"(Get-CimInstance Win32_PhysicalMemory).SMBIOSMemoryType\"") { RedirectStandardOutput = true, CreateNoWindow = true, UseShellExecute = false };
                using var p = Process.Start(psi);
                string output = p!.StandardOutput.ReadToEnd().Trim();
                var lines = output.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                if (lines.Length > 0 && ushort.TryParse(lines[0].Trim(), out ushort type))
                    return type switch { 20 => "ᴰᴰᴿ¹", 21 => "ᴰᴰᴿ²", 24 => "ᴰᴰᴿ³", 26 => "ᴰᴰᴿ⁴", 34 => "ᴰᴰᴿ⁵", _ => "ᴰᴰᴿ" };
            }
            catch { }
            return "";
        }

        private static string GetHighestSensorValue(IHardware hw, SensorType type, string[] nameParts, string fallback, Func<float, string> fmt, bool strict = false)
        {
            if (hw == null) return fallback;
            var allSensors = new List<ISensor>(hw.Sensors);
            foreach (var sub in hw.SubHardware) allSensors.AddRange(sub.Sensors);
            var sensors = allSensors.Where(x => x.SensorType == type).ToList();
            if (nameParts?.Length > 0)
            {
                var matched = sensors.Where(x => nameParts.Any(p => x.Name.Contains(p, StringComparison.OrdinalIgnoreCase))).ToList();
                if (matched.Count > 0) sensors = matched;
                else if (strict) return fallback;
            }
            float maxVal = -1; bool found = false;
            foreach (var s in sensors)
                if (s.Value.HasValue && s.Value.Value > 0 && (!found || s.Value.Value > maxVal)) { maxVal = s.Value.Value; found = true; }
            return found ? fmt(maxVal) : fallback;
        }

        private static float? GetVramSensorValue(IHardware hw, string[] priorityNames)
        {
            if (hw == null) return null;
            var allSensors = new List<ISensor>(hw.Sensors);
            foreach (var sub in hw.SubHardware) allSensors.AddRange(sub.Sensors);
            foreach (var name in priorityNames)
            {
                var s = allSensors.FirstOrDefault(x => (x.SensorType == SensorType.Data || x.SensorType == SensorType.SmallData) && x.Name.Contains(name, StringComparison.OrdinalIgnoreCase) && x.Value.HasValue);
                if (s != null) return s.Value!.Value;
            }
            var fb = allSensors.FirstOrDefault(s => (s.SensorType == SensorType.Data || s.SensorType == SensorType.SmallData) && (s.Name.Contains("GPU Memory Total", StringComparison.OrdinalIgnoreCase) || s.Name.Contains("Memory Total", StringComparison.OrdinalIgnoreCase)) && s.Value.HasValue);
            return fb?.Value;
        }

        private class UpdateVisitor : IVisitor
        {
            public void VisitComputer(IComputer computer)    { computer.Traverse(this); }
            public void VisitHardware(IHardware hardware)    { hardware.Update(); foreach (var sub in hardware.SubHardware) sub.Accept(this); }
            public void VisitSensor(ISensor sensor)          { }
            public void VisitParameter(IParameter parameter) { }
        }
#endif

#if !WINDOWS_BUILD
        private static void UpdateLinux()
        {
            UpdateCpuLoad();
            UpdateCpuTemp();
            UpdateRam();
            UpdateGpu();
        }

        private static void UpdateCpuLoad()
        {
            try
            {
                var line = File.ReadLines("/proc/stat").FirstOrDefault(l => l.StartsWith("cpu "));
                if (line == null) { CpuLoad = "--%"; return; }
                var p = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (p.Length < 5) { CpuLoad = "--%"; return; }
                long user = long.Parse(p[1]), nice = long.Parse(p[2]), sys = long.Parse(p[3]), idle = long.Parse(p[4]), iow = p.Length > 5 ? long.Parse(p[5]) : 0;
                long total = user + nice + sys + idle + iow;
                CpuLoad = total > 0 ? $"{(total - idle - iow) * 100.0 / total:F0}%" : "--%";
            }
            catch { CpuLoad = "--%"; }
        }

        private static void UpdateCpuTemp()
        {
            try
            {
                double best = double.MinValue;
                foreach (var zone in Directory.GetDirectories("/sys/class/thermal", "thermal_zone*"))
                {
                    string tempPath = Path.Combine(zone, "temp");
                    string typePath = Path.Combine(zone, "type");
                    if (!File.Exists(tempPath)) continue;
                    string type = File.Exists(typePath) ? File.ReadAllText(typePath).Trim() : "";
                    if (!type.Contains("acpitz") && !type.Contains("x86_pkg") && !type.Contains("cpu")) continue;
                    if (double.TryParse(File.ReadAllText(tempPath).Trim(), out double raw)) best = Math.Max(best, raw / 1000.0);
                }
                CpuTemp = best > double.MinValue ? $"{best:F0}°C" : "--°C";
            }
            catch { CpuTemp = "--°C"; }
        }

        private static void UpdateRam()
        {
            try
            {
                long memTotal = 0, memAvail = 0;
                foreach (var line in File.ReadAllLines("/proc/meminfo"))
                {
                    var parts = line.Split(':', 2);
                    if (parts.Length < 2) continue;
                    string key = parts[0].Trim(), val = parts[1].Trim().Split(' ')[0];
                    if (key == "MemTotal" && long.TryParse(val, out long t)) memTotal = t;
                    if (key == "MemAvailable" && long.TryParse(val, out long a)) memAvail = a;
                }
                RamUsed  = $"{(memTotal - memAvail) / (1024.0 * 1024.0):F1} GB";
                RamTotal = $"{memTotal / (1024.0 * 1024.0):F1} GB";
            }
            catch { RamUsed = "-- GB"; RamTotal = "-- GB"; }
        }

        private static void UpdateGpu() { if (TryUpdateNvidiaGpu()) return; TryUpdateAmdGpu(); }

        private static bool TryUpdateNvidiaGpu()
        {
            try
            {
                var psi = new ProcessStartInfo("nvidia-smi", "--query-gpu=utilization.gpu,temperature.gpu,power.draw,memory.used,memory.total --format=csv,noheader,nounits") { RedirectStandardOutput = true, UseShellExecute = false, CreateNoWindow = true };
                using var proc = Process.Start(psi);
                string? raw = proc?.StandardOutput.ReadLine()?.Trim();
                if (string.IsNullOrEmpty(raw)) return false;
                var v = raw.Split(',');
                if (v.Length < 5) return false;
                GpuLoad   = $"{v[0].Trim()}%";
                GpuTemp   = $"{v[1].Trim()}°C";
                GpuPower  = $"{double.Parse(v[2].Trim(), CultureInfo.InvariantCulture):F0}W";
                VramUsed  = $"{double.Parse(v[3].Trim(), CultureInfo.InvariantCulture) / 1024.0:F1} GB";
                VramTotal = $"{double.Parse(v[4].Trim(), CultureInfo.InvariantCulture) / 1024.0:F1} GB";
                return true;
            }
            catch { return false; }
        }

        private static void TryUpdateAmdGpu()
        {
            try
            {
                var drmCards = Directory.GetDirectories("/sys/class/drm", "card*").Where(c => !c.Contains('-')).ToList();
                string? selectedCard = null; long largestVram = -1;
                foreach (var card in drmCards)
                {
                    string device = Path.Combine(card, "device");
                    string vendorFile = Path.Combine(device, "vendor");
                    if (!File.Exists(vendorFile)) continue;
                    string vendor = File.ReadAllText(vendorFile).Trim().ToLower();
                    if (vendor != "0x1002") continue;
                    string vramFile = Path.Combine(device, "mem_info_vram_total");
                    long vram = 0; if (File.Exists(vramFile)) long.TryParse(File.ReadAllText(vramFile).Trim(), out vram);
                    if (vram > largestVram) { largestVram = vram; selectedCard = card; }
                }
                if (selectedCard == null && drmCards.Count > 0) selectedCard = drmCards.OrderByDescending(c => { string name = Path.GetFileName(c); return int.TryParse(name.Replace("card", ""), out int n) ? n : -1; }).FirstOrDefault();
                if (selectedCard == null) return;
                string selectedDevice = Path.Combine(selectedCard, "device");
                string busyFile = Path.Combine(selectedDevice, "gpu_busy_percent");
                if (File.Exists(busyFile)) GpuLoad = $"{File.ReadAllText(busyFile).Trim()}%";
                string hwmonBase = Path.Combine(selectedDevice, "hwmon");
                if (Directory.Exists(hwmonBase))
                {
                    var hwmon = Directory.GetDirectories(hwmonBase).FirstOrDefault();
                    if (hwmon != null)
                    {
                        string tempFile = Path.Combine(hwmon, "temp1_input");
                        if (File.Exists(tempFile) && double.TryParse(File.ReadAllText(tempFile).Trim(), out double tRaw)) GpuTemp = $"{tRaw / 1000.0:F0}°C";
                        string powerFile = Path.Combine(hwmon, "power1_average");
                        if (File.Exists(powerFile) && double.TryParse(File.ReadAllText(powerFile).Trim(), out double pRaw)) GpuPower = $"{pRaw / 1_000_000.0:F0}W";
                    }
                }
                string vUsedFile = Path.Combine(selectedDevice, "mem_info_vram_used");
                string vTotalFile = Path.Combine(selectedDevice, "mem_info_vram_total");
                if (File.Exists(vUsedFile) && File.Exists(vTotalFile) && long.TryParse(File.ReadAllText(vUsedFile).Trim(), out long vU) && long.TryParse(File.ReadAllText(vTotalFile).Trim(), out long vT))
                {
                    VramUsed = $"{vU / (1024.0 * 1024.0 * 1024.0):F1} GB";
                    VramTotal = $"{vT / (1024.0 * 1024.0 * 1024.0):F1} GB";
                }
            }
            catch { }
        }
#endif
    }
}