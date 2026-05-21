using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Globalization;
using Raylib_cs;
using ImGuiNET;
using rlImGui_cs;
using LibreHardwareMonitor.Hardware;

namespace XOSC
{
    // --------------------------------------------------------------------------
    // Data Models & Configuration
    // --------------------------------------------------------------------------
    public class StatusItem { public string Text { get; set; } = "New Status"; public bool IsFavorited { get; set; } = false; }
    
    public static class ThemePresets
    {
        public record Preset(string Name, float[] Accent, float[] Bg, float[] Sidebar, float[] Card);

        public static readonly Preset[] All =
        {
            new("XOSC Default", new[]{ 0.38f, 0.73f, 1.00f }, new[]{ 0.10f, 0.10f, 0.13f }, new[]{ 0.07f, 0.07f, 0.09f }, new[]{ 0.14f, 0.14f, 0.18f }),
            new("Midnight Purple", new[]{ 0.70f, 0.40f, 1.00f }, new[]{ 0.08f, 0.06f, 0.12f }, new[]{ 0.05f, 0.04f, 0.09f }, new[]{ 0.12f, 0.10f, 0.18f }),
            new("Forest Green", new[]{ 0.30f, 0.85f, 0.50f }, new[]{ 0.06f, 0.11f, 0.08f }, new[]{ 0.04f, 0.08f, 0.05f }, new[]{ 0.09f, 0.15f, 0.11f }),
            new("Sunset Orange", new[]{ 1.00f, 0.55f, 0.20f }, new[]{ 0.13f, 0.09f, 0.07f }, new[]{ 0.09f, 0.06f, 0.05f }, new[]{ 0.18f, 0.12f, 0.09f }),
            new("Rose Pink", new[]{ 1.00f, 0.45f, 0.65f }, new[]{ 0.13f, 0.08f, 0.10f }, new[]{ 0.09f, 0.05f, 0.07f }, new[]{ 0.18f, 0.11f, 0.14f }),
            new("Ice White", new[]{ 0.10f, 0.45f, 0.90f }, new[]{ 0.94f, 0.95f, 0.97f }, new[]{ 0.84f, 0.86f, 0.90f }, new[]{ 0.99f, 0.99f, 1.00f }),
            new("Deep Red", new[]{ 1.00f, 0.25f, 0.25f }, new[]{ 0.10f, 0.06f, 0.06f }, new[]{ 0.07f, 0.04f, 0.04f }, new[]{ 0.15f, 0.09f, 0.09f }),
            new("Cyberpunk", new[]{ 1.00f, 0.95f, 0.00f }, new[]{ 0.05f, 0.05f, 0.07f }, new[]{ 0.03f, 0.03f, 0.05f }, new[]{ 0.09f, 0.09f, 0.12f })
        };
    }

    public class AppConfig
    {
        public bool ChatboxEnabled = true;
        public int Interval = 3;
        public string City = "Springfield";
        public string CustomCity = "";
        public string State = "Illinois";
        public string CustomState = "";
        public string Country = "United States";
        public string CustomCountry = "";
        public string Pronouns = "They/Them";
        public string CustomPronouns = "";
        public string StatusIcon = "⭐";
        public bool ThinMode = false;
        public bool PcMode = false;
        public bool ShowRam = false;
        public bool ShowVram = false;
        public bool DistroMode = false;
        public bool WeatherMode = false;
        public bool WeatherTempMode = true;
        public string WeatherTempUnit = "°F";
        public bool WeatherAlertMode = true;
        public bool TimeMode = false;
        public bool MilitaryTime = false;
        public bool SongMode = true;
        public bool SongProgressMode = false;
        public bool AudioVisualizerMode = false;
        public bool AfkDetectionMode = false;
        public bool VrBatteryMode = false;
        public bool EasMode = false;
        public bool NetMode = false;
        public bool VrcPingMode = false;
        public bool HwNameMode = false;
        public bool CustomCpuNameOn = false;
        public bool CustomGpuNameOn = false;
        public string CustomCpuName = "CPU";
        public string CustomGpuName = "GPU";
        public bool StatusTextMode = false;
        public bool PronounsMode = false;
        public bool CpuTempOn = false;
        public bool GpuTempOn = false;
        public bool CpuPowerOn = false;
        public bool GpuPowerOn = false;
        public bool GpuHotspotOn = false;
        public bool RamDdrVersionOn = false;
        public string CpuUnit = "%";
        public string RamUnit = "GB";
        public string GpuUnit = "%";
        public string VramUnit = "GB";
        public bool StylizeTextMode = false;
        public List<StatusItem> StatusList { get; set; } = new();
        public float TabRounding { get; set; }

        public bool AutoCycleStatus = false;
        public string PublishPath = "https://github.com/hollyntt/XOSC/raw/refs/heads/master/publish/XOSC.zip";
        public bool BetaOptIn = false;
        public string Cookie = "";
        public string SavedVersion = "";
        public string OscIP = "127.0.0.1";
        public int OscPort = 9000;
        public float[] AccentColor = { 0.38f, 0.73f, 1.00f };
        public float[] BgColor = { 0.10f, 0.10f, 0.13f };
        public float[] SidebarColor = { 0.07f, 0.07f, 0.09f };
        public float[] CardColor = { 0.14f, 0.14f, 0.18f };
        public float SidebarWidth = 172f;
        public float FontScale = 1.0f;
        public float WindowRounding = 0f;
        public float ChildRounding = 6f;
        public float FrameRounding = 5f;
    }

    public static class NativeMethods
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct FILETIME { public uint dwLowDateTime; public uint dwHighDateTime; public ulong ToULong() => ((ulong)dwHighDateTime << 32) | dwLowDateTime; }
        
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool GetSystemTimes(out FILETIME lpIdleTime, out FILETIME lpKernelTime, out FILETIME lpUserTime);

        [StructLayout(LayoutKind.Sequential)]
        public struct MEMORYSTATUSEX { public uint dwLength; public uint dwMemoryLoad; public ulong ullTotalPhys; public ulong ullAvailPhys; public ulong ullTotalPageFile; public ulong ullAvailPageFile; public ulong ullTotalVirtual; public ulong ullAvailVirtual; public ulong ullAvailExtendedVirtual; }
        
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);
        [DllImport("kernel32.dll")]
        public static extern bool FreeConsole();
        [UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        public static unsafe void RaylibLogCallback(int logLevel, sbyte* text, sbyte* args) { }
    }

    // --------------------------------------------------------------------------
    // Formatter Extensions
    // --------------------------------------------------------------------------
    public enum StatsComponentType { CPU, GPU, RAM, VRAM, FPS, Unknown }

    public static class StatsComponentTypeExtensions
    {
        public static string GetSmallName(this StatsComponentType type)
        {
            switch (type)
            {
                case StatsComponentType.CPU: return "ᶜᵖᵘ";
                case StatsComponentType.GPU: return "ᵍᵖᵘ";
                case StatsComponentType.RAM: return "ʳᵃᵐ";
                case StatsComponentType.VRAM: return "ᵛʳᵃᵐ";
                case StatsComponentType.FPS: return "ᶠᵖˢ";
                case StatsComponentType.Unknown: return "ᵘⁿᵏⁿᵒʷⁿ";
                default: throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }
    }

    // --------------------------------------------------------------------------
    // HardwareService - LibreHardwareMonitor with iGPU Fallback
    // --------------------------------------------------------------------------
    public static class HardwareService
    {
        private static Computer _computer;
        private static bool _initialized;

        private static IHardware _cpu;
        private static IHardware _gpu;
        private static IHardware _igpu; 
        private static IHardware _ram;

        public static string CpuLoad { get; private set; } = "--%";
        public static string GpuLoad { get; private set; } = "--%";
        public static string RamUsed { get; private set; } = "-- GB";
        public static string RamTotal { get; private set; } = "-- GB";
        public static string RamDdr { get; private set; } = "";
        public static string VramUsed { get; private set; } = "-- GB";
        public static string VramTotal { get; private set; } = "-- GB";
        public static string CpuTemp { get; private set; } = "--°C";
        public static string CpuPower { get; private set; } = "--W";
        public static string GpuTemp { get; private set; } = "--°C";
        public static string GpuHotspot { get; private set; } = "--°C";
        public static string GpuPower { get; private set; } = "--W";

        public static void Initialize()
        {
            if (_initialized) return;
            _computer = new Computer 
            { 
                IsCpuEnabled = true, 
                IsGpuEnabled = true, 
                IsMemoryEnabled = true,
                IsMotherboardEnabled = true, 
                IsControllerEnabled = true 
            };
            _computer.Open();
            _computer.Accept(new UpdateVisitor());

            _cpu = _computer.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.Cpu);
            
            var gpus = _computer.Hardware.Where(h => 
                h.HardwareType == HardwareType.GpuNvidia || 
                h.HardwareType == HardwareType.GpuAmd || 
                h.HardwareType == HardwareType.GpuIntel).ToList();

            string[] igpuKeywords = { "integrated", "radeon(tm) graphics", "radeon graphics", "vega", "uhd graphics", "iris xe" };
            _gpu = gpus.FirstOrDefault(h => !igpuKeywords.Any(k => h.Name.Contains(k, StringComparison.OrdinalIgnoreCase)));
            if (_gpu == null) _gpu = gpus.FirstOrDefault();
            _igpu = gpus.FirstOrDefault(h => igpuKeywords.Any(k => h.Name.Contains(k, StringComparison.OrdinalIgnoreCase)));

            _ram = _computer.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.Memory);
            RamDdr = GetDDRVersion();

            _initialized = true;
        }

        public static void Update()
        {
            if (!_initialized) return;
            _computer.Accept(new UpdateVisitor());

            if (_cpu != null)
            {
                CpuLoad = GetHighestSensorValue(_cpu, SensorType.Load, new[] { "CPU Total", "Total" }, "--%", v => $"{v:F0}%", strict: false);
                CpuTemp = GetHighestSensorValue(_cpu, SensorType.Temperature, new[] { "Package", "Core", "Tctl", "Tdie", "CCD" }, "--°C", v => $"{v:F0}°C", strict: false);
                if (CpuTemp == "--°C" && _igpu != null)
                {
                    CpuTemp = GetHighestSensorValue(_igpu, SensorType.Temperature, new[] { "Core", "Package", "Hot Spot", "Hotspot" }, "--°C", v => $"{v:F0}°C", strict: false);
                }
                CpuPower = GetHighestSensorValue(_cpu, SensorType.Power, new[] { "Package", "Core", "PPT" }, "--W", v => $"{v:F0}W", strict: false);
                if (CpuPower == "--W" && _igpu != null)
                {
                    CpuPower = GetHighestSensorValue(_igpu, SensorType.Power, new[] { "Package", "Core", "Power", "PPT" }, "--W", v => $"{v:F0}W", strict: false);
                }
            }

            if (_gpu != null)
            {
                GpuLoad = GetHighestSensorValue(_gpu, SensorType.Load, new[] { "3D", "Core" }, "--% ", v => $"{v:F0}%", strict: true);
                GpuTemp = GetHighestSensorValue(_gpu, SensorType.Temperature, new[] { "GPU Core", "Core" }, "--°C ", v => $"{v:F0}°C", strict: false);
                GpuHotspot = GetHighestSensorValue(_gpu, SensorType.Temperature, new[] { "Hot spot", "Hotspot" }, "--°C ", v => $"{v:F0}°C", strict: true);
                GpuPower = GetHighestSensorValue(_gpu, SensorType.Power, new[] { "GPU Power", "Package", "Total Board", "PPT" }, "--W ", v => $"{v:F0}W", strict: false);

                // ✅ FIX: Prioritize Dedicated VRAM sensors to ignore Shared Memory bleed-through
                float? vramUsed = GetVramSensorValue(_gpu, new[] { "Dedicated Memory Used", "Memory Used" });
                float? vramTotal = GetVramSensorValue(_gpu, new[] { "Dedicated Memory Total", "Memory Total" });

                if (vramUsed.HasValue && vramTotal.HasValue)
                {
                    VramUsed = $"{vramUsed.Value / 1024f:F1} GB";
                    VramTotal = $"{vramTotal.Value / 1024f:F1} GB";
                }
            }
            
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var memStatus = new NativeMethods.MEMORYSTATUSEX();
                memStatus.dwLength = (uint)Marshal.SizeOf(memStatus);
                if (NativeMethods.GlobalMemoryStatusEx(ref memStatus))
                {
                    double usedGB = (memStatus.ullTotalPhys - memStatus.ullAvailPhys) / (1024.0 * 1024.0 * 1024.0);
                    double totalGB = memStatus.ullTotalPhys / (1024.0 * 1024.0 * 1024.0);
                    RamUsed = $"{usedGB:F1} GB";
                    RamTotal = $"{totalGB:F1} GB";
                }
            }
            else
            {
                // Keep old LHM logic for Linux just in case, though this file is Windows-focused
                if (_ram != null)
                {
                    float ramUsedMB = 0f;
                    float ramAvailMB = 0f;
                    bool hasUsed = false, hasAvail = false;

                    var allSensors = new List<ISensor>(_ram.Sensors);
                    foreach (var sub in _ram.SubHardware) allSensors.AddRange(sub.Sensors);

                    foreach (var s in allSensors)
                    {
                        if (s.SensorType == SensorType.Data && s.Value.HasValue)
                        {
                            if (s.Name.Contains("Memory Used", StringComparison.OrdinalIgnoreCase))
                            {
                                ramUsedMB += s.Value.Value;
                                hasUsed = true;
                            }
                            else if (s.Name.Contains("Memory Available", StringComparison.OrdinalIgnoreCase))
                            {
                                ramAvailMB += s.Value.Value;
                                hasAvail = true;
                            }
                        }
                    }

                    if (hasUsed && hasAvail)
                    {
                        float totalMB = ramUsedMB + ramAvailMB;
                        RamUsed = $"{ramUsedMB / 1024f:F1} GB";
                        RamTotal = $"{totalMB / 1024f:F1} GB";
                    }
                } 
            }
        }

        public static void Close()
        {
            _computer?.Close();
            _initialized = false;
        }

        private static string GetDDRVersion()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return "";
            try
            {
                var psi = new ProcessStartInfo("powershell", "-NoProfile -Command \"(Get-CimInstance Win32_PhysicalMemory).SMBIOSMemoryType\"")
                    { RedirectStandardOutput = true, CreateNoWindow = true, UseShellExecute = false };
                using var p = Process.Start(psi);
                string output = p.StandardOutput.ReadToEnd().Trim();
                var lines = output.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                if (lines.Length > 0 && ushort.TryParse(lines[0].Trim(), out ushort type))
                {
                    return type switch
                    {
                        20 => "ᴰᴰᴿ¹", 21 => "ᴰᴰᴿ²", 24 => "ᴰᴰᴿ³", 26 => "ᴰᴰᴿ⁴", 34 => "ᴰᴰᴿ⁵", _ => "ᴰᴰᴿ"
                    };
                }
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
            if (nameParts != null && nameParts.Length > 0)
            {
                var matched = sensors.Where(x => nameParts.Any(p => x.Name.Contains(p, StringComparison.OrdinalIgnoreCase))).ToList();
                if (matched.Count > 0) sensors = matched;
                else if (strict) return fallback;
            }
            float maxVal = -1;
            bool found = false;
            foreach (var s in sensors)
            {
                if (s.Value.HasValue && s.Value.Value > 0) 
                {
                    if (!found || s.Value.Value > maxVal) { maxVal = s.Value.Value; found = true; }
                }
            }
            return found ? fmt(maxVal) : fallback;
        }

        private static float? GetVramSensorValue(IHardware hw, string[] priorityNames)
        {
            if (hw == null) return null;
            var allSensors = new List<ISensor>(hw.Sensors);
            foreach (var sub in hw.SubHardware) allSensors.AddRange(sub.Sensors);

            // 1. Try to find the specific priority names first
            foreach (var name in priorityNames)
            {
                var sensor = allSensors.FirstOrDefault(s =>
                    (s.SensorType == SensorType.Data || s.SensorType == SensorType.SmallData) &&
                    s.Name.Contains(name, StringComparison.OrdinalIgnoreCase) &&
                    s.Value.HasValue);
                
                if (sensor != null) return sensor.Value.Value;
            }

            // 2. Fallback for iGPUs: If we didn't find Dedicated, look for "GPU Memory Total" (Shared)
            // This fixes the 0.5GB issue on AMD APUs
            var fallbackSensor = allSensors.FirstOrDefault(s =>
                (s.SensorType == SensorType.Data || s.SensorType == SensorType.SmallData) &&
                (s.Name.Contains("GPU Memory Total", StringComparison.OrdinalIgnoreCase) || s.Name.Contains("Memory Total", StringComparison.OrdinalIgnoreCase)) &&
                s.Value.HasValue);

            return fallbackSensor?.Value;
        }

        private class UpdateVisitor : IVisitor
        {
            public void VisitComputer(IComputer computer) { computer.Traverse(this); }
            public void VisitHardware(IHardware hardware) { hardware.Update(); foreach (var sub in hardware.SubHardware) sub.Accept(this); }
            public void VisitSensor(ISensor sensor) { }
            public void VisitParameter(IParameter parameter) { }
        }
    }

    // --------------------------------------------------------------------------
    // NetworkStats - Full throughput + ping
    // --------------------------------------------------------------------------
    public static class NetworkStats
    {
        private static Timer _timer;
        private static NetworkInterface _activeInterface;
        private static long _prevBytesRecv, _prevBytesSent;
        private static double _totalRecvMB, _totalSentMB;
        private static readonly object _lock = new();

        public static double DownloadSpeedMbps { get; private set; }
        public static double UploadSpeedMbps { get; private set; }
        public static double TotalDownloadedMB => _totalRecvMB;
        public static double TotalUploadedMB   => _totalSentMB;
        public static double NetworkUtilization { get; private set; }
        public static string InterfaceName { get; private set; } = "Unknown";
        public static bool IsActive { get; private set; }

        private static readonly Queue<double> _latencies = new();
        private static readonly string[] _targets = { "1.1.1.1", "8.8.8.8", "vrcoscv4.vrchat.cloud" };
        private static int _pingTargetIdx;
        private static double _lastJitter;
        public static double AvgPing { get; private set; }
        public static double PacketLoss { get; private set; }
        public static double Jitter { get; private set; }
        public static string PingStatus { get; private set; } = "Idle";

        public static async Task UpdateAsync()
        {
            var target = _targets[_pingTargetIdx];
            _pingTargetIdx = (_pingTargetIdx + 1) % _targets.Length;
            try
            {
                using var ping = new Ping();
                var reply = await ping.SendPingAsync(target, 1500);
                if (reply.Status == IPStatus.Success)
                {
                    double rtt = reply.RoundtripTime;
                    lock (_latencies)
                    {
                        _latencies.Enqueue(rtt);
                        if (_latencies.Count > 8) _latencies.Dequeue();
                        if (_latencies.Count >= 2)
                        {
                            var list = _latencies.ToList();
                            AvgPing = Math.Round(list.Average(), 1);
                            PacketLoss = 0;
                            double diff = Math.Abs(rtt - list[^2]);
                            _lastJitter = (_lastJitter * 0.7) + (diff * 0.3);
                            Jitter = Math.Round(_lastJitter, 1);
                        }
                    }
                    PingStatus = "Stable";
                }
                else { PacketLoss = Math.Min(100, PacketLoss + 12); PingStatus = "Timeout"; }
            }
            catch { PacketLoss = Math.Min(100, PacketLoss + 15); PingStatus = "Error"; }
        }

        public static void Start()
        {
            var interfaces = NetworkInterface.GetAllNetworkInterfaces()
                .Where(ni => ni.OperationalStatus == OperationalStatus.Up &&
                             ni.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                             ni.NetworkInterfaceType != NetworkInterfaceType.Tunnel)
                .ToList();
            if (interfaces.Count > 1) _activeInterface = interfaces.OrderByDescending(ni => ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet ? 3 : ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 ? 2 : 1).FirstOrDefault();
            else _activeInterface = interfaces.FirstOrDefault();
            if (_activeInterface == null) return;
            InterfaceName = _activeInterface.Name;
            var stats = _activeInterface.GetIPv4Statistics();
            _prevBytesRecv = stats.BytesReceived;
            _prevBytesSent = stats.BytesSent;
            IsActive = true;
            _timer = new Timer(_ =>
            {
                if (_activeInterface == null) return;
                var s = _activeInterface.GetIPv4Statistics();
                double dlMbps = (s.BytesReceived - _prevBytesRecv) * 8 / 1e6;
                double ulMbps = (s.BytesSent - _prevBytesSent) * 8 / 1e6;
                lock (_lock) { DownloadSpeedMbps = Math.Max(0, dlMbps); UploadSpeedMbps = Math.Max(0, ulMbps); _totalRecvMB += (s.BytesReceived - _prevBytesRecv) / 1e6; _totalSentMB += (s.BytesSent - _prevBytesSent) / 1e6; double maxMbps = _activeInterface.Speed / 1e6; NetworkUtilization = maxMbps > 0 ? Math.Min(100, (dlMbps / maxMbps) * 100) : 0; }
                _prevBytesRecv = s.BytesReceived; _prevBytesSent = s.BytesSent;
            }, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
        }

        public static void Stop() { _timer?.Dispose(); _timer = null; IsActive = false; }
        public static string FormatSpeed(double mbps) => mbps < 1 ? $"{mbps * 1000:F0} Kbps" : (mbps >= 1000 ? $"{mbps / 1000:F1} Gbps" : $"{mbps:F1} Mbps");
    }

    public static class Updater 
    { 
        public static string Status = "idle"; 
        public static bool NewVersionFound = false; 
        private static byte[]? _pData; 
        private const string StableApiUrl = "https://api.github.com/repos/hollyntt/XOSC/releases/latest";
        public static async Task CheckForUpdates() { Status = "checking GitHub..."; NewVersionFound = false; try { using var http = new HttpClient(); http.DefaultRequestHeaders.Add("User-Agent", "XOSC-Updater"); var r = await http.GetStringAsync(StableApiUrl); using var doc = JsonDocument.Parse(r); string tag = doc.RootElement.GetProperty("tag_name").GetString() ?? ""; string latestVersion = tag.TrimStart('v'); if (latestVersion == Program.AppVersion) { Status = "already up to date"; return; } var asset = doc.RootElement.GetProperty("assets").EnumerateArray().FirstOrDefault(a => a.GetProperty("name").GetString() == "XOSC.zip"); if (asset.ValueKind == JsonValueKind.Undefined) { Status = "XOSC.zip not found"; return; } string dUrl = asset.GetProperty("browser_download_url").GetString() ?? ""; var z = await http.GetByteArrayAsync(dUrl); using var ms = new MemoryStream(z); using var arch = new ZipArchive(ms); string targetFolder = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "win-x64" : "linux-x64"; var entries = arch.Entries.Where(e => e.FullName.StartsWith(targetFolder, StringComparison.OrdinalIgnoreCase)).ToList(); string[] names = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? new[] { "xosc.exe", "XOSC.exe" } : new[] { "xosc", "XOSC" }; ZipArchiveEntry? entry = null; foreach (var name in names) { entry = entries.FirstOrDefault(e => Path.GetFileName(e.FullName).Equals(name, StringComparison.OrdinalIgnoreCase)); if (entry != null) break; } if (entry == null) { Status = "No executable found"; return; } Status = $"Update found! (v{latestVersion})"; NewVersionFound = true; using var es = entry.Open(); using var msw = new MemoryStream(); await es.CopyToAsync(msw); _pData = msw.ToArray(); } catch (Exception e) { Status = $"error: {e.Message}"; } }
        public static void ApplyUpdate() { if (_pData == null) return; try { string self = Environment.ProcessPath!; Program.SaveConfig(); string bak = self + ".bak"; if (File.Exists(bak)) File.Delete(bak); File.Move(self, bak, true); File.WriteAllBytes(self, _pData); if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) Process.Start("chmod", $"+x \"{self}\"").WaitForExit(); Thread.Sleep(500); Process.Start(new ProcessStartInfo(self) { UseShellExecute = true }); Environment.Exit(0); } catch (Exception e) { Status = $"apply error: {e.Message}"; } } 
    }

    public static class MusicChatEngine
    {
        private static UdpClient _client = new UdpClient();
        private static readonly object _clientLock = new();
        private static CancellationTokenSource? _cts;
        private static int _statusIdx = 0;
        private static bool _showHardwareTick = false;
        private static string _cpu = "CPU", _gpu = "GPU";
        private static bool _isAfk = false;
        private static DateTime _lastWeatherFetch = DateTime.MinValue;
        private static int _weatherCode = 0;
        private static double _weatherTempC = 0;
        private static string _activeAlert = string.Empty;
        private static string _lastNotifiedAlert = string.Empty;
        private static DateTime _alertExpire = DateTime.MinValue;
        private static (string Title, double Position, double Length) _musicData = ("Chilling", 0, 0);
        private static DateTime _lastR = DateTime.MinValue, _lastS = DateTime.MinValue, _manualE = DateTime.MinValue;
        private static string _manualM = "";
        public static int PacketsSent = 0;
        public static string EngineState = "Idle";
        public static readonly object ListLock = new();
        private static Process? _psMediaProcess;
        private static string _psMediaData = "Chilling|0|0";
        private static Random _visRand = new Random();
        private static string[] _visBars = { " ", "▂", "▃", "▄", "▅", "▆", "▇", "█" };
        public static string ActiveAlert => _activeAlert;
        
        public static void Init() { _client = new UdpClient(); _cts?.Cancel(); _cts = new CancellationTokenSource(); HardwareService.Initialize(); NetworkStats.Start(); ScrapeHardwareNames(); StartWindowsMediaScraper(); Task.Run(() => Loop(_cts.Token)); }
        public static void SetManual(string m) { _manualM = m; _manualE = DateTime.Now.AddSeconds(20); }
        private static async Task Loop(CancellationToken t) { while (!t.IsCancellationRequested) { if (Program.Config.ChatboxEnabled) try { await Update(); } catch { } await Task.Delay(1000, t); } }
        
        private static async Task<(double lat, double lon)> GetCoordinatesAsync() { var cfg = Program.Config; string search = !string.IsNullOrWhiteSpace(cfg.CustomCity) ? cfg.CustomCity : cfg.City; if (!string.IsNullOrWhiteSpace(search)) { try { using var http = new HttpClient(); var q = Uri.EscapeDataString(search); var j = await http.GetStringAsync($"https://geocoding-api.open-meteo.com/v1/search?name={q}&count=1"); using var doc = JsonDocument.Parse(j); var r = doc.RootElement.GetProperty("results"); if (r.ValueKind == JsonValueKind.Array && r.GetArrayLength() > 0) { var f = r[0]; return (f.GetProperty("latitude").GetDouble(), f.GetProperty("longitude").GetDouble()); } } catch { } } try { using var http = new HttpClient(); http.DefaultRequestHeaders.Add("User-Agent", "XOSC-Weather"); var j = await http.GetStringAsync("https://ipapi.co/json/"); using var doc = JsonDocument.Parse(j); return (doc.RootElement.GetProperty("latitude").GetDouble(), doc.RootElement.GetProperty("longitude").GetDouble()); } catch { } return (39.78, -89.65); }
        private static async Task FetchWeatherAsync(double lat, double lon) { try { using var http = new HttpClient(); var u = $"https://api.open-meteo.com/v1/forecast?latitude={lat}&longitude={lon}&current=weather_code,temperature_2m"; var r = await http.GetStringAsync(u); using var doc = JsonDocument.Parse(r); var c = doc.RootElement.GetProperty("current"); _weatherCode = c.GetProperty("weather_code").GetInt32(); _weatherTempC = c.GetProperty("temperature_2m").GetDouble(); } catch { } if (Program.Config.WeatherAlertMode) { try { using var http = new HttpClient(); http.DefaultRequestHeaders.Add("User-Agent", "XOSC-Alerts"); var j = await http.GetStringAsync($"https://api.weather.gov/alerts/active?point={lat:F4},{lon:F4}"); using var doc = JsonDocument.Parse(j); var f = doc.RootElement.GetProperty("features"); if (f.GetArrayLength() > 0) { var p = f[0].GetProperty("properties"); string evt = p.GetProperty("event").GetString() ?? "Alert"; string head = p.TryGetProperty("headline", out var hl) ? (hl.GetString() ?? evt) : evt; if (head.Length > 100) head = head[..100] + "…"; _activeAlert = $"{evt.ToUpper()}: {head}"; _alertExpire = DateTime.Now.AddMinutes(5); } else if (DateTime.Now > _alertExpire) { _activeAlert = string.Empty; } } catch { if (DateTime.Now > _alertExpire) _activeAlert = string.Empty; } } }
        private static string WeatherCodeToString(int code, double tempC, string unit) { string condition = code switch { 0 => "☀️ Clear", 1 => "🌤️ Mostly Clear", 2 => "⛅ Partly Cloudy", 3 => "☁️ Overcast", 45 or 48 => "🌫️ Foggy", 51 or 53 or 55 => "🌦️ Drizzle", 56 or 57 => "🌨️ Freezing Drizzle", 61 or 63 or 65 => "🌧️ Rain", 66 or 67 => "🌨️ Freezing Rain", 71 or 73 or 75 => "❄️ Snow", 77 => "🌨️ Snow Grains", 80 or 81 or 82 => "🌧️ Showers", 85 or 86 => "❄️ Snow Showers", 95 => "⛈️ Thunderstorm", 96 or 99 => "⛈️ Thunderstorm w/ Hail", _ => $"🌡️ Code {code}" }; if (!Program.Config.WeatherTempMode) return condition; string tempStr = unit == "°F" ? $"{Math.Round(tempC * 9.0 / 5.0 + 32, 0)}°F" : $"{Math.Round(tempC, 0)}°C"; return $"{condition} {tempStr}"; }

        private static async Task Update()
        {
            var cfg = Program.Config;
            if (DateTime.Now < _manualE) { EngineState = "Manual"; SendOsc("/chatbox/input", $"💬 {_manualM}"); return; }
            if ((DateTime.Now - _lastR).TotalSeconds >= cfg.Interval) { _musicData = FetchMusicData(); if (cfg.WeatherMode) { var c = await GetCoordinatesAsync(); await FetchWeatherAsync(c.lat, c.lon); } if (cfg.NetMode) await NetworkStats.UpdateAsync(); _lastR = DateTime.Now; }
            if (cfg.AfkDetectionMode) CheckAfk();
            if ((DateTime.Now - _lastS).TotalSeconds < Math.Max(cfg.Interval, 1.5)) return;
            if ((cfg.EasMode || cfg.WeatherAlertMode) && !string.IsNullOrEmpty(_activeAlert) && DateTime.Now < _alertExpire) { if (_activeAlert != _lastNotifiedAlert) { _lastNotifiedAlert = _activeAlert; NotifyOS(_activeAlert); } string al = $"⚠️ {_activeAlert}"; if (al.Length > 140) al = al[..140]; if (cfg.ThinMode) al += "\u0003\u001f"; SendOsc("/chatbox/input", al); _lastS = DateTime.Now; PacketsSent++; EngineState = "Alert"; return; }
            var p1 = new List<string>(); bool statusAdded = false; string sText = null;
            lock (ListLock) { if (cfg.StatusTextMode && cfg.StatusList.Count > 0) { if (_statusIdx >= cfg.StatusList.Count) _statusIdx = 0; sText = cfg.StatusList[_statusIdx].Text; p1.Add(_isAfk ? "AFK" : sText); statusAdded = true; } }
            string pr = cfg.Pronouns == "Custom..." ? cfg.CustomPronouns : cfg.Pronouns;
            if (cfg.PronounsMode && !string.IsNullOrEmpty(pr)) p1.Add($"{cfg.StatusIcon} {(cfg.StylizeTextMode ? Stylize(pr) : pr)}");
            var e1 = new List<string>();
            if (cfg.TimeMode) { string t = DateTime.Now.ToString(cfg.MilitaryTime ? "HH:mm" : "hh:mm tt"); e1.Add($"🕒 {(cfg.StylizeTextMode ? Stylize(t) : t)}"); }
            if (cfg.DistroMode) { string dn = GetDistroName(); e1.Add(cfg.StylizeTextMode ? Stylize(dn) : dn); }
            if (cfg.WeatherMode) e1.Add(WeatherCodeToString(_weatherCode, _weatherTempC, cfg.WeatherTempUnit));
            if (e1.Count > 0) p1.Add(string.Join(" | ", e1));
            if (cfg.SongMode) {
                string sTitle = _musicData.Title == "Chilling" ? "Chilling" : _musicData.Title;
                string sStr = $"♪ {(cfg.StylizeTextMode ? Stylize(sTitle) : sTitle)}";
                string top = "";
                if (cfg.SongProgressMode && cfg.AudioVisualizerMode) top = ((DateTime.Now.Second / 5) % 2 == 0) ? MakeVisualizer() : MakeProgressBar(_musicData.Position, _musicData.Length);
                else if (cfg.AudioVisualizerMode) top = MakeVisualizer();
                else if (cfg.SongProgressMode) { top = MakeProgressBar(_musicData.Position, _musicData.Length); if (cfg.StylizeTextMode) top = Stylize(top); }
                if (!string.IsNullOrEmpty(top)) sStr = $"{top}\n{sStr}";
                p1.Add(sStr);
            }
            if (cfg.NetMode) { string n = $"🌐 {NetworkStats.AvgPing}ms ({NetworkStats.PacketLoss}% loss)"; if (NetworkStats.IsActive) n += $"\n⬇ {NetworkStats.FormatSpeed(NetworkStats.DownloadSpeedMbps)} ⬆ {NetworkStats.FormatSpeed(NetworkStats.UploadSpeedMbps)}\n📶 {NetworkStats.NetworkUtilization:F0}%"; p1.Add(n); }
            var p2 = new List<string>();
            if (cfg.PcMode) {
                HardwareService.Update();
                if (sText != null) p2.Add(_isAfk ? "AFK" : sText);
                var e2 = new List<string>();
                if (cfg.TimeMode) { string t2 = DateTime.Now.ToString(cfg.MilitaryTime ? "HH:mm" : "hh:mm tt"); e2.Add($"🕒 {(cfg.StylizeTextMode ? Stylize(t2) : t2)}"); }
                if (cfg.DistroMode) { string dn2 = GetDistroName(); e2.Add(cfg.StylizeTextMode ? Stylize(dn2) : dn2); }
                if (e2.Count > 0) p2.Add(string.Join(" | ", e2));
                string cS = cfg.HwNameMode ? StatsComponentType.CPU.GetSmallName() : "CPU"; string gS = cfg.HwNameMode ? StatsComponentType.GPU.GetSmallName() : "GPU"; string rS = cfg.HwNameMode ? StatsComponentType.RAM.GetSmallName() : "RAM"; string vS = cfg.HwNameMode ? StatsComponentType.VRAM.GetSmallName() : "VRAM";
                string cId = cfg.CustomCpuNameOn ? (cfg.HwNameMode ? Stylize(cfg.CustomCpuName) : cfg.CustomCpuName) : (cfg.HwNameMode ? Stylize(_cpu) : cS);
                string cL = $"🖥️ {cId}: {HardwareService.CpuLoad}"; List<string> cEx = new(); if (cfg.CpuTempOn) cEx.Add(HardwareService.CpuTemp); if (cfg.CpuPowerOn) cEx.Add(HardwareService.CpuPower); if (cEx.Count > 0) cL += $" ({string.Join(" / ", cEx)})";
                string gId = cfg.CustomGpuNameOn ? (cfg.HwNameMode ? Stylize(cfg.CustomGpuName) : cfg.CustomGpuName) : (cfg.HwNameMode ? Stylize(_gpu) : gS);
                string gL = $"🎮 {gId}: {HardwareService.GpuLoad}"; List<string> gEx = new(); if (cfg.GpuTempOn) gEx.Add(HardwareService.GpuTemp); if (cfg.GpuHotspotOn) gEx.Add($"H {HardwareService.GpuHotspot}"); if (cfg.GpuPowerOn) gEx.Add(HardwareService.GpuPower); if (gEx.Count > 0) gL += $" ({string.Join(" / ", gEx)})";
                p2.Add($"{cL} | {gL}");
                var mem = new List<string>(); if (cfg.ShowRam) { string ddr = (cfg.RamDdrVersionOn && !string.IsNullOrEmpty(HardwareService.RamDdr)) ? $" ⁽{HardwareService.RamDdr}⁾" : ""; mem.Add($"🐏 {rS}{ddr}: {HardwareService.RamUsed}/{HardwareService.RamTotal}"); } if (cfg.ShowVram) mem.Add($"🎞️ {vS}: {HardwareService.VramUsed}/{HardwareService.VramTotal}"); if (cfg.VrBatteryMode && RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) mem.Add($"🔋 VR: {GetVrBattery()}"); if (mem.Count > 0) p2.Add(string.Join(" | ", mem));
            }
            List<string> active; if (p1.Count > 0 && p2.Count > 0) { _showHardwareTick = !_showHardwareTick; active = _showHardwareTick ? p2 : p1; } else if (p2.Count > 0) { _showHardwareTick = true; active = p2; } else { _showHardwareTick = false; active = p1; }
            string outStr = string.Join("\n", active); if (cfg.ThinMode) { if (outStr.Length > 138) outStr = outStr[..138]; outStr += "\u0003\u001f"; } SendOsc("/chatbox/input", outStr); _lastS = DateTime.Now; PacketsSent++; EngineState = "Chatting";
            if (!_showHardwareTick && statusAdded && cfg.AutoCycleStatus) lock (ListLock) _statusIdx = (_statusIdx + 1) % cfg.StatusList.Count;
        }

        public static void NotifyOS(string txt) { string s = txt.Replace("'", "").Replace("\"", "").Replace("\n", " "); if (s.Length > 200) s = s[..200] + "..."; try { if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) { string ps = "Add-Type -AssemblyName System.Windows.Forms; $n = New-Object System.Windows.Forms.NotifyIcon; $n.Icon = [System.Drawing.SystemIcons]::Warning; $n.Visible = $true; $n.ShowBalloonTip(8000, 'XOSC Alert', '" + s + "', [System.Windows.Forms.ToolTipIcon]::Warning); Start-Sleep -Seconds 9; $n.Visible = $false"; Process.Start(new ProcessStartInfo("powershell", "-NoProfile -WindowStyle Hidden -Command \"" + ps + "\"") { CreateNoWindow = true, UseShellExecute = false }); } else Process.Start(new ProcessStartInfo("notify-send", "--urgency=critical \"XOSC Alert\" \"" + s + "\"") { UseShellExecute = false, CreateNoWindow = true }); } catch { } }
        private static void SendOsc(string addr, string txt) { try { List<byte> p = new(); void Add(string s) { byte[] b = Encoding.UTF8.GetBytes(s); p.AddRange(b); p.Add(0); while (p.Count % 4 != 0) p.Add(0); } Add(addr); Add(addr == "/chatbox/input" ? ",sTF" : ",s"); Add(txt); var ip = Program.Config.OscIP.Trim(); var pt = Program.Config.OscPort; if (string.IsNullOrEmpty(ip)) return; lock (_clientLock) { _client.Send(p.ToArray(), p.Count, ip, pt); } } catch (Exception) { lock (_clientLock) { try { _client.Close(); } catch { } _client = new UdpClient(); } } }
        private static string GetVrBattery() { try { var psi = new ProcessStartInfo("powershell", "-Command \"if (Get-Process vrserver -ErrorAction SilentlyContinue) { '85%' } else { '0%' }\"") { RedirectStandardOutput = true, CreateNoWindow = true, UseShellExecute = false }; using var p = Process.Start(psi); return p?.StandardOutput.ReadToEnd().Trim() ?? "0%"; } catch { return "0%"; } }
        private static void CheckAfk() { string log = Program.FindVrcLog(); if (log == null) return; try { using var fs = new FileStream(log, FileMode.Open, FileAccess.Read, FileShare.ReadWrite); using var sr = new StreamReader(fs); string l, last = ""; while ((l = sr.ReadLine()) != null) last = l; if (last.Contains("OnPlayerResting")) _isAfk = true; else if (last.Contains("OnPlayerActive")) _isAfk = false; } catch { } }

        private static string MakeProgressBar(double pos, double len)
        {
            if (len <= 0) return "";
            if (pos > len) pos = len;
            if (pos < 0) pos = 0;
    
            int width = 8;
            int filled = (int)Math.Round((pos / len) * width);
            filled = Math.Clamp(filled, 0, width);
    
            // Uses StringBuilder for Unicode safety (matching your visualizer style)
            StringBuilder sb = new StringBuilder("[");
            for (int i = 0; i < filled; i++) sb.Append("■");
            for (int i = 0; i < (width - filled); i++) sb.Append("□");
            sb.Append("] ");
    
            var p = TimeSpan.FromSeconds(pos);
            var l = TimeSpan.FromSeconds(len);
            sb.Append($"{(int)p.TotalMinutes}:{p.Seconds:D2}/{(int)l.TotalMinutes}:{l.Seconds:D2}");
    
            return sb.ToString();
        }

        private static string MakeVisualizer() { StringBuilder sb = new StringBuilder("♪ "); for (int i = 0; i < 14; i++) sb.Append(_visBars[_visRand.Next(0, _visBars.Length)]); return sb.Append(" ♪").ToString(); }

        private static void StartWindowsMediaScraper()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return;
            try
            {
                // RE-ADDED ASEMBLY INJECTION: Necessary to get EndTime and status on many Windows versions
                string script = @"Add-Type -AssemblyName System.Runtime.WindowsRuntime -ErrorAction SilentlyContinue; [Windows.Media.Control.GlobalSystemMediaTransportControlsSessionManager, Windows.Media, ContentType = WindowsRuntime] | Out-Null; $req = [Windows.Media.Control.GlobalSystemMediaTransportControlsSessionManager]::RequestAsync(); while($req.Status -eq 0) { Start-Sleep -Milliseconds 10 }; $m = $req.GetResults(); while($true) { try { $s = $m.GetCurrentSession(); if ($s) { $t = $s.GetTimelineProperties(); $p = $s.GetPlaybackInfo(); $propsReq = $s.TryGetMediaPropertiesAsync(); while($propsReq.Status -eq 0) { Start-Sleep -Milliseconds 10 }; $i = $propsReq.GetResults(); $art = $i.Artist; $tit = $i.Title; $name = if ([string]::IsNullOrWhiteSpace($art)) { $tit } else { ""$art - $tit"" }; if ([string]::IsNullOrWhiteSpace($name)) { $name = ""Chilling"" }; $pos = 0; $end = 0; if ($t) { $end = $t.EndTime.TotalSeconds; if ($p -and $p.PlaybackStatus -eq 4) { $diff = ([DateTimeOffset]::Now - $t.LastUpdatedTime).TotalSeconds; $pos = $t.Position.TotalSeconds + $diff; } else { $pos = $t.Position.TotalSeconds; } }; $pos = [math]::Round($pos); $end = [math]::Round($end); if ($end -lt $pos) { $end = $pos }; [Console]::WriteLine(""$name|$pos|$end""); [Console]::Out.Flush(); } else { [Console]::WriteLine(""Chilling|0|0""); [Console]::Out.Flush(); } } catch { [Console]::WriteLine(""Chilling|0|0""); [Console]::Out.Flush(); } Start-Sleep -Seconds 1; }";
                string b64 = Convert.ToBase64String(Encoding.Unicode.GetBytes(script));
                var psi = new ProcessStartInfo("powershell", $"-NoProfile -ExecutionPolicy Bypass -EncodedCommand {b64}") { RedirectStandardOutput = true, UseShellExecute = false, CreateNoWindow = true };
                _psMediaProcess = Process.Start(psi);
                AppDomain.CurrentDomain.ProcessExit += (s, e) => { try { _psMediaProcess?.Kill(); } catch { } };
                Task.Run(() => { if (_psMediaProcess != null) { using var sr = _psMediaProcess.StandardOutput; while (!sr.EndOfStream) { string? line = sr.ReadLine(); if (!string.IsNullOrWhiteSpace(line)) _psMediaData = line; } } });
            } catch { }
        }

        private static (string Title, double Position, double Length) FetchMusicData()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                if (!string.IsNullOrEmpty(_psMediaData) && _psMediaData != "Chilling|0|0") {
                    var parts = _psMediaData.Split('|');
                    if (parts.Length >= 3) {
                        double.TryParse(parts[parts.Length - 2], NumberStyles.Any, CultureInfo.InvariantCulture, out double pos);
                        double.TryParse(parts[parts.Length - 1], NumberStyles.Any, CultureInfo.InvariantCulture, out double len);
                        string title = string.Join("|", parts.Take(parts.Length - 2));
                        return (title, pos, len);
                    }
                }
                try { var spot = Process.GetProcessesByName("Spotify"); foreach (var p in spot) { string t = p.MainWindowTitle; if (!string.IsNullOrWhiteSpace(t) && t != "Spotify" && t != "Spotify Premium") return (t, 0, 0); } } catch { }
                StringBuilder b = new StringBuilder(256); IntPtr h = GetForegroundWindow(); if (GetWindowText(h, b, 256) > 0) { string t = b.ToString(); if (t.Contains("YouTube") || t.Contains("SoundCloud") || t.Contains("Spotify")) return (Regex.Replace(t, @" - (Spotify|YouTube|SoundCloud).*", "").Trim(), 0, 0); }
                return ("Chilling", 0, 0);
            } else {
                try { var psi = new ProcessStartInfo("playerctl", "metadata --format \"{{artist}} - {{title}}\"") { RedirectStandardOutput = true, UseShellExecute = false, CreateNoWindow = true }; using var p = Process.Start(psi); string r = p?.StandardOutput.ReadToEnd().Trim() ?? ""; if (!string.IsNullOrEmpty(r) && r != " - ") { double pos = 0, len = 0; try { var pP = new ProcessStartInfo("playerctl", "position") { RedirectStandardOutput = true, UseShellExecute = false, CreateNoWindow = true }; using var pp = Process.Start(pP); double.TryParse(pp?.StandardOutput.ReadToEnd().Trim(), out pos); var pL = new ProcessStartInfo("playerctl", "metadata mpris:length") { RedirectStandardOutput = true, UseShellExecute = false, CreateNoWindow = true }; using var pl = Process.Start(pL); if (long.TryParse(pl?.StandardOutput.ReadToEnd().Trim(), out long lM)) len = lM / 1000000.0; } catch { } return (r, pos, len); } } catch { } return ("Chilling", 0, 0);
            }
        }

        private static void ScrapeHardwareNames() { string log = Program.FindVrcLog(); if (log == null) return; try { using var fs = new FileStream(log, FileMode.Open, FileAccess.Read, FileShare.ReadWrite); using var sr = new StreamReader(fs); string l; int count = 0; while ((l = sr.ReadLine()) != null && count < 2000) { count++; if (l.Contains("Processor Type:")) { string r = l.Substring(l.IndexOf(':') + 1); _cpu = Regex.Replace(Regex.Replace(r, @"(?i)(AMD|Intel(?:\(R\))?|Core(?:\(TM\))?|Ryzen|\d+-Core|Processor|@.*)", " "), @"\s+", " ").Trim(); } else if (l.Contains("Graphics Device Name:")) { string r = l.Substring(l.IndexOf(':') + 1); _gpu = Regex.Replace(Regex.Replace(r, @"(?i)(NVIDIA|AMD|GeForce|Radeon|Graphics|\(RADV.*?\)|Direct3D.*)", ""), @"\s+", " ").Trim(); } } } catch { } }
        private static string GetDistroName() { if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return "Windows"; try { if (File.Exists("/etc/os-release")) { string? n = null, p = null; foreach (var l in File.ReadLines("/etc/os-release")) { if (l.StartsWith("NAME=")) n = l[5..].Trim('"', '\''); if (l.StartsWith("PRETTY_NAME=")) p = l[12..].Trim('"', '\''); } return (n ?? p ?? "Linux").Split(' ')[0]; } } catch { } return "Linux"; }
        private static string Stylize(string t) { string n = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789", s = "ᵃᵇᶜᵈᵉᶠᵍʰᶦʲᵏˡᵐⁿᵒᵖᵠʳˢᵗᵘᵛʷˣʸᶻᵃᵇᶜᵈᵉᶠᵍʰᶦʲᵏˡᵐⁿᵒᵖᵠʳˢᵗᵘᵛʷˣʸᶻ⁰¹²³⁴⁵⁶⁷⁸⁹"; StringBuilder sb = new(); foreach (char c in t) { int i = n.IndexOf(c); sb.Append(i != -1 ? s[i] : c); } return sb.ToString(); }
        [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow(); [DllImport("user32.dll")] private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);
    }

    class Program
    {
        public static string AppVersion { get { var a = Assembly.GetExecutingAssembly(); var v = a.GetCustomAttribute<AssemblyInformationalVersionAttribute>(); if (v != null && !string.IsNullOrEmpty(v.InformationalVersion)) return v.InformationalVersion.Length >= 7 ? v.InformationalVersion[..7] : v.InformationalVersion; return "unknown"; } }
        public static AppConfig Config = new();
        private static string _path = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) 
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "xosc", "config.json") 
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config", "xosc", "config.json");
        private static string _chatIn = "";
        private static Mutex? _mtx; private static int _navPage = 0;
        private static readonly string[] _navLabels = { "Dashboard", "Statuses", "Chatbox", "Hardware", "Network", "Appearance", "Misc", "Updater" };
        private static Vector4 ColAccent, ColBg, ColSidebar, ColCard, ColText, ColSubText;
        private static HashSet<int> _selectedIndices = new();
        private static readonly string[] _pronounsList = { "He/Him", "She/Her", "They/Them", "He/They", "She/They", "It/Its", "Any", "Custom..." };
        private static readonly string[] _tempUnits = { "°F", "°C" };
        private static readonly string[] _countriesList = { "Afghanistan", "Albania", "Algeria", "Andorra", "Angola", "Antigua and Barbuda", "Argentina", "Armenia", "Australia", "Austria", "Azerbaijan", "Bahamas", "Bahrain", "Bangladesh", "Barbados", "Belarus", "Belgium", "Belize", "Benin", "Bhutan", "Bolivia", "Bosnia and Herzegovina", "Botswana", "Brazil", "Brunei", "Bulgaria", "Burkina Faso", "Burundi", "Cabo Verde", "Cambodia", "Cameroon", "Canada", "Central African Republic", "Chad", "Chile", "China", "Colombia", "Comoros", "Congo", "Costa Rica", "Croatia", "Cuba", "Cyprus", "Czechia", "Democratic Republic of the Congo", "Denmark", "Djibouti", "Dominica", "Dominican Republic", "Ecuador", "Egypt", "El Salvador", "Equatorial Guinea", "Eritrea", "Estonia", "Eswatini", "Ethiopia", "Fiji", "Finland", "France", "Gabon", "Gambia", "Georgia", "Germany", "Ghana", "Greece", "Grenada", "Guatemala", "Guinea", "Guinea-Bissau", "Guyana", "Haiti", "Honduras", "Hungary", "Iceland", "India", "Indonesia", "Iran", "Iraq", "Ireland", "Israel", "Italy", "Jamaica", "Japan", "Jordan", "Kazakhstan", "Kenya", "Kiribati", "Kuwait", "Kyrgyzstan", "Laos", "Latvia", "Lebanon", "Lesotho", "Liberia", "Libya", "Liechtenstein", "Lithuania", "Luxembourg", "Madagascar", "Malawi", "Malaysia", "Maldives", "Mali", "Malta", "Marshall Islands", "Mauritania", "Mauritius", "Mexico", "Micronesia", "Moldova", "Monaco", "Mongolia", "Montenegro", "Morocco", "Mozambique", "Myanmar", "Namibia", "Nauru", "Nepal", "Netherlands", "New Zealand", "Nicaragua", "Niger", "Nigeria", "North Korea", "North Macedonia", "Norway", "Oman", "Pakistan", "Palau", "Palestine", "Panama", "Papua New Guinea", "Paraguay", "Peru", "Philippines", "Poland", "Portugal", "Qatar", "Romania", "Russia", "Rwanda", "Saint Kitts and Nevis", "Saint Lucia", "Saint Vincent and the Grenadines", "Samoa", "San Marino", "Sao Tome and Principe", "Saudi Arabia", "Senegal", "Serbia", "Seychelles", "Sierra Leone", "Singapore", "Slovakia", "Slovenia", "Solomon Islands", "Somalia", "South Africa", "South Korea", "South Sudan", "Spain", "Sri Lanka", "Sudan", "Suriname", "Sweden", "Switzerland", "Syria", "Tajikistan", "Tanzania", "Thailand", "Timor-Leste", "Togo", "Tonga", "Trinidad and Tobago", "Tunisia", "Turkey", "Turkmenistan", "Tuvalu", "Uganda", "Ukraine", "United Arab Emirates", "United Kingdom", "United States", "Uruguay", "Uzbekistan", "Vanuatu", "Vatican City", "Venezuela", "Vietnam", "Yemen", "Zambia", "Zimbabwe", "Custom..." };
        private static readonly Dictionary<string, string[]> _statesMap = new() { { "United States", new[] { "Alabama", "Alaska", "Arizona", "Arkansas", "California", "Colorado", "Connecticut", "Delaware", "Florida", "Georgia", "Hawaii", "Idaho", "Illinois", "Indiana", "Iowa", "Kansas", "Kentucky", "Louisiana", "Maine", "Maryland", "Massachusetts", "Michigan", "Minnesota", "Mississippi", "Missouri", "Montana", "Nebraska", "Nevada", "New Hampshire", "New Jersey", "New Mexico", "New York", "North Carolina", "North Dakota", "Ohio", "Oklahoma", "Oregon", "Pennsylvania", "Rhode Island", "South Carolina", "South Dakota", "Tennessee", "Texas", "Utah", "Vermont", "Virginia", "Washington", "West Virginia", "Wisconsin", "Wyoming", "Custom..." } }, { "Canada", new[] { "Alberta", "British Columbia", "Manitoba", "New Brunswick", "Newfoundland and Labrador", "Nova Scotia", "Ontario", "Prince Edward Island", "Quebec", "Saskatchewan", "Northwest Territories", "Nunavut", "Yukon", "Custom..." } }, { "Australia", new[] { "New South Wales", "Victoria", "Queensland", "Western Australia", "South Australia", "Tasmania", "Australian Capital Territory", "Northern Territory", "Custom..." } }, { "United Kingdom", new[] { "England", "Scotland", "Wales", "Northern Ireland", "Custom..." } } };
        private static readonly Dictionary<string, string[]> _citiesMap = new() { { "Alabama", new[] { "Birmingham", "Montgomery", "Huntsville", "Mobile", "Custom..." } }, { "Alaska", new[] { "Anchorage", "Fairbanks", "Juneau", "Custom..." } }, { "Arizona", new[] { "Phoenix", "Tucson", "Mesa", "Custom..." } }, { "Arkansas", new[] { "Little Rock", "Fayetteville", "Fort Smith", "Custom..." } }, { "California", new[] { "Los Angeles", "San Francisco", "San Diego", "Sacramento", "San Jose", "Custom..." } }, { "Colorado", new[] { "Denver", "Colorado Springs", "Aurora", "Custom..." } }, { "Connecticut", new[] { "Bridgeport", "New Haven", "Hartford", "Custom..." } }, { "Delaware", new[] { "Wilmington", "Dover", "Newark", "Custom..." } }, { "Florida", new[] { "Miami", "Orlando", "Tampa", "Jacksonville", "Tallahassee", "Custom..." } }, { "Georgia", new[] { "Atlanta", "Augusta", "Savannah", "Custom..." } }, { "Hawaii", new[] { "Honolulu", "Hilo", "Kailua", "Custom..." } }, { "Idaho", new[] { "Boise", "Meridian", "Nampa", "Custom..." } }, { "Illinois", new[] { "Chicago", "Aurora", "Springfield", "Custom..." } }, { "Indiana", new[] { "Indianapolis", "Fort Wayne", "Evansville", "Custom..." } }, { "Iowa", new[] { "Des Moines", "Cedar Rapids", "Davenport", "Custom..." } }, { "Kansas", new[] { "Wichita", "Overland Park", "Kansas City", "Custom..." } }, { "Kentucky", new[] { "Louisville", "Lexington", "Bowling Green", "Custom..." } }, { "Louisiana", new[] { "New Orleans", "Baton Rouge", "Shreveport", "Custom..." } }, { "Maine", new[] { "Portland", "Lewiston", "Bangor", "Custom..." } }, { "Maryland", new[] { "Baltimore", "Annapolis", "Frederick", "Custom..." } }, { "Massachusetts", new[] { "Boston", "Worcester", "Springfield", "Custom..." } }, { "Michigan", new[] { "Detroit", "Grand Rapids", "Lansing", "Custom..." } }, { "Minnesota", new[] { "Minneapolis", "St. Paul", "Rochester", "Custom..." } }, { "Mississippi", new[] { "Jackson", "Gulfport", "Southaven", "Custom..." } }, { "Missouri", new[] { "Kansas City", "St. Louis", "Springfield", "Custom..." } }, { "Montana", new[] { "Billings", "Missoula", "Great Falls", "Custom..." } }, { "Nebraska", new[] { "Omaha", "Lincoln", "Bellevue", "Custom..." } }, { "Nevada", new[] { "Las Vegas", "Henderson", "Reno", "Custom..." } }, { "New Hampshire", new[] { "Manchester", "Nashua", "Concord", "Custom..." } }, { "New Jersey", new[] { "Newark", "Jersey City", "Paterson", "Custom..." } }, { "New Mexico", new[] { "Albuquerque", "Las Cruces", "Rio Rancho", "Custom..." } }, { "New York", new[] { "New York City", "Buffalo", "Rochester", "Albany", "Syracuse", "Custom..." } }, { "North Carolina", new[] { "Charlotte", "Raleigh", "Greensboro", "Custom..." } }, { "North Dakota", new[] { "Fargo", "Bismarck", "Grand Forks", "Custom..." } }, { "Ohio", new[] { "Columbus", "Cleveland", "Cincinnati", "Custom..." } }, { "Oklahoma", new[] { "Oklahoma City", "Tulsa", "Norman", "Custom..." } }, { "Oregon", new[] { "Portland", "Salem", "Eugene", "Custom..." } }, { "Pennsylvania", new[] { "Philadelphia", "Pittsburgh", "Allentown", "Custom..." } }, { "Rhode Island", new[] { "Providence", "Warwick", "Cranston", "Custom..." } }, { "South Carolina", new[] { "Charleston", "Columbia", "North Charleston", "Custom..." } }, { "South Dakota", new[] { "Sioux Falls", "Rapid City", "Aberdeen", "Custom..." } }, { "Tennessee", new[] { "Nashville", "Memphis", "Knoxville", "Custom..." } }, { "Texas", new[] { "Houston", "San Antonio", "Dallas", "Austin", "Fort Worth", "Custom..." } }, { "Utah", new[] { "Salt Lake City", "West Valley City", "Provo", "Custom..." } }, { "Vermont", new[] { "Burlington", "South Burlington", "Rutland", "Custom..." } }, { "Virginia", new[] { "Virginia Beach", "Norfolk", "Chesapeake", "Richmond", "Custom..." } }, { "Washington", new[] { "Seattle", "Spokane", "Tacoma", "Custom..." } }, { "West Virginia", new[] { "Charleston", "Huntington", "Morgantown", "Custom..." } }, { "Wisconsin", new[] { "Milwaukee", "Madison", "Green Bay", "Custom..." } }, { "Wyoming", new[] { "Cheyenne", "Casper", "Laramie", "Custom..." } }, { "Alberta", new[] { "Calgary", "Edmonton", "Red Deer", "Custom..." } }, { "British Columbia", new[] { "Vancouver", "Victoria", "Kelowna", "Custom..." } }, { "Manitoba", new[] { "Winnipeg", "Brandon", "Steinbach", "Custom..." } }, { "New Brunswick", new[] { "Moncton", "Saint John", "Fredericton", "Custom..." } }, { "Newfoundland and Labrador", new[] { "St. John's", "Corner Brook", "Mount Pearl", "Custom..." } }, { "Nova Scotia", new[] { "Halifax", "Sydney", "Truro", "Custom..." } }, { "Ontario", new[] { "Toronto", "Ottawa", "Mississauga", "Hamilton", "Custom..." } }, { "Prince Edward Island", new[] { "Charlottetown", "Summerside", "Stratford", "Custom..." } }, { "Quebec", new[] { "Montreal", "Quebec City", "Laval", "Custom..." } }, { "Saskatchewan", new[] { "Saskatoon", "Regina", "Prince Albert", "Custom..." } }, { "Northwest Territories", new[] { "Yellowknife", "Hay River", "Inuvik", "Custom..." } }, { "Nunavut", new[] { "Iqaluit", "Rankin Inlet", "Arviat", "Custom..." } }, { "Yukon", new[] { "Whitehorse", "Dawson City", "Watson Lake", "Custom..." } }, { "New South Wales", new[] { "Sydney", "Newcastle", "Wollongong", "Custom..." } }, { "Victoria", new[] { "Melbourne", "Geelong", "Ballarat", "Custom..." } }, { "Queensland", new[] { "Brisbane", "Gold Coast", "Sunshine Coast", "Custom..." } }, { "Western Australia", new[] { "Perth", "Mandurah", "Bunbury", "Custom..." } }, { "South Australia", new[] { "Adelaide", "Mount Gambier", "Gawler", "Custom..." } }, { "Tasmania", new[] { "Hobart", "Launceston", "Devonport", "Custom..." } }, { "Australian Capital Territory", new[] { "Canberra", "Custom..." } }, { "Northern Territory", new[] { "Darwin", "Alice Springs", "Katherine", "Custom..." } }, { "England", new[] { "London", "Birmingham", "Manchester", "Liverpool", "Leeds", "Custom..." } }, { "Scotland", new[] { "Glasgow", "Edinburgh", "Aberdeen", "Dundee", "Custom..." } }, { "Wales", new[] { "Cardiff", "Swansea", "Newport", "Custom..." } }, { "Northern Ireland", new[] { "Belfast", "Derry", "Lisburn", "Custom..." } } };
        static Vector4 V4(float[] c) => new(c[0], c[1], c[2], 1f);
        static Vector4 DeriveText(Vector4 bg) { bool l = (bg.X + bg.Y + bg.Z) / 3f > 0.6f; return l ? new Vector4(0.12f, 0.12f, 0.16f, 1f) : new Vector4(0.88f, 0.88f, 0.92f, 1f); }
        static Vector4 DeriveSubText(Vector4 bg) { bool l = (bg.X + bg.Y + bg.Z) / 3f > 0.6f; return l ? new Vector4(0.35f, 0.35f, 0.42f, 1f) : new Vector4(0.52f, 0.52f, 0.60f, 1f); }
        public static void Main() { 
#if RELEASE
            unsafe { Raylib.SetTraceLogCallback(&NativeMethods.RaylibLogCallback); } Raylib.SetTraceLogLevel(TraceLogLevel.None); Console.SetOut(TextWriter.Null); Console.SetError(TextWriter.Null); NativeMethods.FreeConsole(); 
#endif
            LoadConfig(); ColAccent = V4(Config.AccentColor); ColBg = V4(Config.BgColor); ColSidebar = V4(Config.SidebarColor); ColCard = V4(Config.CardColor); ColText = DeriveText(V4(Config.BgColor)); ColSubText = DeriveSubText(V4(Config.BgColor)); _mtx = new Mutex(true, "XOSC_VRC_Unique_Runner", out bool fresh); if (!fresh) Environment.Exit(0); Directory.CreateDirectory(Path.GetDirectoryName(_path)); if (Config.SavedVersion != AppVersion) { Config.SavedVersion = AppVersion; SaveConfig(); } MusicChatEngine.Init(); Raylib.InitWindow(960, 640, "XOSC"); try { string iP = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icon.png"); if (!File.Exists(iP)) iP = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icon.ico"); if (File.Exists(iP)) { Image img = Raylib.LoadImage(iP); Raylib.SetWindowIcon(img); Raylib.UnloadImage(img); } } catch { } Raylib.SetWindowState(ConfigFlags.ResizableWindow); rlImGui.Setup(true); Raylib.SetTargetFPS(60); ApplyTheme(); while (!Raylib.WindowShouldClose()) { Raylib.BeginDrawing(); Raylib.ClearBackground(new Color((int)(Config.BgColor[0]*255),(byte)(Config.BgColor[1]*255),(byte)(Config.BgColor[2]*255),255)); rlImGui.Begin(); DrawUI(); rlImGui.End(); Raylib.EndDrawing(); } NetworkStats.Stop(); HardwareService.Close(); SaveConfig(); Raylib.CloseWindow(); }
        public static string FindVrcLog() { if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) { string wP = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "AppData", "LocalLow", "VRChat", "VRChat"); if (Directory.Exists(wP)) return Directory.GetFiles(wP, "output_log_*.txt").OrderByDescending(File.GetLastWriteTime).FirstOrDefault(); return null; } string h = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile); string[] s = { Path.Combine(h, ".local/share/Steam"), Path.Combine(h, ".var/app/com.valvesoftware.Steam/.local/share/Steam") }; foreach (var b in s) { if (!Directory.Exists(b)) continue; string v = Path.Combine(b, "steamapps", "libraryfolders.vdf"); List<string> l = new() { b }; if (File.Exists(v)) { var ms = Regex.Matches(File.ReadAllText(v), "\"path\"\\s+\"(.+?)\""); foreach (Match m in ms) l.Add(m.Groups[1].Value.Replace("\\\\", "/")); } foreach (var lib in l) { string p = Path.Combine(lib, "steamapps/compatdata/438100/pfx/drive_c/users/steamuser/AppData/LocalLow/VRChat/VRChat"); if (Directory.Exists(p)) return Directory.GetFiles(p, "output_log_*.txt").OrderByDescending(File.GetLastWriteTime).FirstOrDefault(); } } return null; }
        static void ApplyTheme() { var s = ImGui.GetStyle(); s.WindowRounding = Config.WindowRounding; s.ChildRounding = Config.ChildRounding; s.FrameRounding = Config.FrameRounding; s.PopupRounding = Config.FrameRounding; s.ScrollbarRounding = Config.FrameRounding; s.GrabRounding = Config.FrameRounding; s.TabRounding = Config.TabRounding; s.WindowPadding = new Vector2(12, 12); s.FramePadding = new Vector2(8, 4); s.ItemSpacing = new Vector2(8, 6); ImGui.GetIO().FontGlobalScale = Config.FontScale; ColAccent = V4(Config.AccentColor); ColBg = V4(Config.BgColor); ColSidebar = V4(Config.SidebarColor); ColCard = V4(Config.CardColor); ColText = DeriveText(ColBg); ColSubText = DeriveSubText(ColBg); var colors = ImGui.GetStyle().Colors; var a = ColAccent; var bg = ColBg; var c = ColCard; float b = 0.07f; var frame = new Vector4(Math.Min(c.X+b, 1f), Math.Min(c.Y+b, 1f), Math.Min(c.Z+b, 1f), 1f); var frameH = new Vector4(Math.Min(c.X+b+0.04f, 1f), Math.Min(c.Y+b+0.04f, 1f), Math.Min(c.Z+b+0.04f, 1f), 1f); var frameA = new Vector4(Math.Min(c.X+b+0.08f, 1f), Math.Min(c.Y+b+0.08f, 1f), Math.Min(c.Z+b+0.08f, 1f), 1f); var popup = new Vector4(Math.Min(c.X+0.04f, 1f), Math.Min(c.Y+0.04f, 1f), Math.Min(c.Z+0.06f, 1f), 1f); colors[(int)ImGuiCol.WindowBg] = bg; colors[(int)ImGuiCol.ChildBg] = c; colors[(int)ImGuiCol.PopupBg] = popup; colors[(int)ImGuiCol.MenuBarBg] = c; colors[(int)ImGuiCol.FrameBg] = frame; colors[(int)ImGuiCol.FrameBgHovered] = frameH; colors[(int)ImGuiCol.FrameBgActive] = frameA; colors[(int)ImGuiCol.Button] = new Vector4(a.X, a.Y, a.Z, 0.20f); colors[(int)ImGuiCol.ButtonHovered] = new Vector4(a.X, a.Y, a.Z, 0.40f); colors[(int)ImGuiCol.ButtonActive] = new Vector4(a.X, a.Y, a.Z, 0.65f); colors[(int)ImGuiCol.CheckMark] = a; colors[(int)ImGuiCol.SliderGrab] = a; colors[(int)ImGuiCol.SliderGrabActive] = new Vector4(a.X, a.Y, a.Z, 0.85f); colors[(int)ImGuiCol.ScrollbarBg] = new Vector4(bg.X, bg.Y, bg.Z, 1f); colors[(int)ImGuiCol.ScrollbarGrab] = new Vector4(a.X, a.Y, a.Z, 0.35f); colors[(int)ImGuiCol.ScrollbarGrabHovered] = new Vector4(a.X, a.Y, a.Z, 0.65f); colors[(int)ImGuiCol.ScrollbarGrabActive] = a; colors[(int)ImGuiCol.Header] = new Vector4(a.X, a.Y, a.Z, 0.22f); colors[(int)ImGuiCol.HeaderHovered] = new Vector4(a.X, a.Y, a.Z, 0.38f); colors[(int)ImGuiCol.HeaderActive] = new Vector4(a.X, a.Y, a.Z, 0.55f); colors[(int)ImGuiCol.TitleBg] = c; colors[(int)ImGuiCol.TitleBgActive] = popup; colors[(int)ImGuiCol.TitleBgCollapsed] = c; colors[(int)ImGuiCol.Tab] = c; colors[(int)ImGuiCol.TabHovered] = new Vector4(a.X, a.Y, a.Z, 0.35f); colors[(int)ImGuiCol.TabSelected] = new Vector4(a.X, a.Y, a.Z, 0.25f); colors[(int)ImGuiCol.TabSelectedOverline] = a; colors[(int)ImGuiCol.TabDimmed] = c; colors[(int)ImGuiCol.TabDimmedSelected] = new Vector4(a.X, a.Y, a.Z, 0.14f); colors[(int)ImGuiCol.TabDimmedSelectedOverline] = new Vector4(a.X, a.Y, a.Z, 0.40f); colors[(int)ImGuiCol.Separator] = new Vector4(a.X, a.Y, a.Z, 0.22f); colors[(int)ImGuiCol.SeparatorHovered] = new Vector4(a.X, a.Y, a.Z, 0.55f); colors[(int)ImGuiCol.SeparatorActive] = a; colors[(int)ImGuiCol.ResizeGrip] = new Vector4(a.X, a.Y, a.Z, 0.18f); colors[(int)ImGuiCol.ResizeGripHovered] = new Vector4(a.X, a.Y, a.Z, 0.45f); colors[(int)ImGuiCol.ResizeGripActive] = a; colors[(int)ImGuiCol.Border] = new Vector4(a.X, a.Y, a.Z, 0.18f); colors[(int)ImGuiCol.BorderShadow] = new Vector4(0f, 0f, 0f, 0f); bool lT = (bg.X + bg.Y + bg.Z) / 3f > 0.6f; colors[(int)ImGuiCol.Text] = lT ? new Vector4(0.10f, 0.10f, 0.13f, 1f) : new Vector4(0.92f, 0.92f, 0.95f, 1f); colors[(int)ImGuiCol.TextDisabled] = lT ? new Vector4(0.40f, 0.40f, 0.45f, 1f) : new Vector4(0.50f, 0.50f, 0.55f, 1f); colors[(int)ImGuiCol.TextLink] = a; colors[(int)ImGuiCol.NavCursor] = a; colors[(int)ImGuiCol.DragDropTarget] = a; colors[(int)ImGuiCol.TextSelectedBg] = new Vector4(a.X, a.Y, a.Z, 0.35f); colors[(int)ImGuiCol.NavWindowingHighlight] = new Vector4(a.X, a.Y, a.Z, 0.70f); colors[(int)ImGuiCol.NavWindowingDimBg] = new Vector4(0f, 0f, 0f, 0.45f); colors[(int)ImGuiCol.ModalWindowDimBg] = new Vector4(0f, 0f, 0f, 0.45f); }

        static void DrawUI() { ApplyTheme(); int w = Raylib.GetScreenWidth(), sh = Raylib.GetScreenHeight(); float sw = Config.SidebarWidth; ImGui.SetNextWindowPos(Vector2.Zero); ImGui.SetNextWindowSize(new Vector2(w, sh)); ImGui.Begin("##root", ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoScrollbar); string alert = MusicChatEngine.ActiveAlert; if (!string.IsNullOrWhiteSpace(alert)) { ImGui.PushStyleColor(ImGuiCol.ChildBg, new Vector4(0.55f, 0.08f, 0.08f, 1f)); ImGui.BeginChild("##alertbanner", new Vector2(w, 30), ImGuiChildFlags.None, ImGuiWindowFlags.NoScrollbar); ImGui.SetCursorPos(new Vector2(10, 6)); ImGui.TextColored(new Vector4(1f, 0.85f, 0.85f, 1f), alert); ImGui.EndChild(); ImGui.PopStyleColor(); } ImGui.PushStyleColor(ImGuiCol.ChildBg, ColSidebar); ImGui.BeginChild("##sidebar", new Vector2(sw, sh), ImGuiChildFlags.None, ImGuiWindowFlags.NoScrollbar); ImGui.Dummy(new Vector2(0, 20)); ImGui.SetCursorPosX(20); ImGui.TextColored(ColAccent, "XOSC"); ImGui.SetCursorPosX(20); ImGui.TextColored(ColSubText, $"v{AppVersion}"); ImGui.Dummy(new Vector2(0, 20)); for (int i = 0; i < _navLabels.Length; i++) { bool active = _navPage == i; ImGui.PushStyleColor(ImGuiCol.Button, active ? new Vector4(ColAccent.X, ColAccent.Y, ColAccent.Z, 0.15f) : Vector4.Zero); ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(ColAccent.X, ColAccent.Y, ColAccent.Z, 0.08f)); ImGui.PushStyleColor(ImGuiCol.Text, active ? ColAccent : ColText); ImGui.SetCursorPosX(10); if (ImGui.Button(_navLabels[i], new Vector2(sw - 20, 36))) _navPage = i; ImGui.PopStyleColor(3); } ImGui.EndChild(); ImGui.PopStyleColor(); ImGui.SameLine(); ImGui.PushStyleColor(ImGuiCol.ChildBg, ColBg); ImGui.BeginChild("##content", new Vector2(w - sw, sh), ImGuiChildFlags.None, ImGuiWindowFlags.NoScrollbar); ImGui.Dummy(new Vector2(0, 24)); switch (_navPage) { 
            case 0: Card("Dashboard", () => { Toggle("Enable Chatbox", ref Config.ChatboxEnabled); ImGui.Text($"Engine State: {MusicChatEngine.EngineState}"); ImGui.Text($"Packets Sent: {MusicChatEngine.PacketsSent}"); ImGui.Text($"Weather Alert: {(string.IsNullOrEmpty(MusicChatEngine.ActiveAlert) ? "None" : "ACTIVE")}"); ImGui.Dummy(new Vector2(0, 6)); ImGui.InputText("Manual Message", ref _chatIn, 128); if (ImGui.Button("Send Manual")) { MusicChatEngine.SetManual(_chatIn); _chatIn = ""; } ImGui.Dummy(new Vector2(0, 10)); if (ImGui.InputText("OSC IP", ref Config.OscIP, 64)) SaveConfig(); if (ImGui.InputInt("OSC Port", ref Config.OscPort)) SaveConfig(); }); break; 
            case 1: Card("Statuses", () => { lock (MusicChatEngine.ListLock) { for (int i = 0; i < Config.StatusList.Count; i++) { ImGui.PushID(i); var item = Config.StatusList[i]; bool isSelected = _selectedIndices.Contains(i); if (ImGui.Checkbox("##select", ref isSelected)) { if (isSelected) _selectedIndices.Add(i); else _selectedIndices.Remove(i); } ImGui.SameLine(); if (ImGui.Button(item.IsFavorited ? "[*]" : "[ ]", new Vector2(32, 24))) { item.IsFavorited = !item.IsFavorited; Config.StatusList = Config.StatusList.OrderByDescending(s => s.IsFavorited).ToList(); SaveConfig(); ImGui.PopID(); break; } ImGui.SameLine(); if (ImGui.Button("Up", new Vector2(32, 24)) && i > 0) { (Config.StatusList[i], Config.StatusList[i - 1]) = (Config.StatusList[i - 1], Config.StatusList[i]); SaveConfig(); } ImGui.SameLine(); if (ImGui.Button("Dn", new Vector2(32, 24)) && i < Config.StatusList.Count - 1) { (Config.StatusList[i], Config.StatusList[i + 1]) = (Config.StatusList[i + 1], Config.StatusList[i]); SaveConfig(); } ImGui.SameLine(); string statusText = item.Text; if (ImGui.InputText("##s", ref statusText, 100)) { item.Text = statusText; SaveConfig(); } ImGui.PopID(); } } ImGui.Dummy(new Vector2(0, 10)); if (ImGui.Button("+ Add New Status")) Config.StatusList.Add(new StatusItem()); ImGui.SameLine(); if (_selectedIndices.Any() && ImGui.Button("Remove Selected")) { var sorted = _selectedIndices.ToList(); sorted.Sort(); for (int i = sorted.Count - 1; i >= 0; i--) Config.StatusList.RemoveAt(sorted[i]); _selectedIndices.Clear(); SaveConfig(); } }); break; 
            case 2: Card("Chatbox", () => { Toggle("Status Text", ref Config.StatusTextMode); Toggle("Pronouns##Toggle", ref Config.PronounsMode); Toggle("Song Mode", ref Config.SongMode); if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) Toggle("Song Progress Bar", ref Config.SongProgressMode); Toggle("Audio Visualizer", ref Config.AudioVisualizerMode); Toggle("Time", ref Config.TimeMode); Toggle("Military Time", ref Config.MilitaryTime); Toggle("Distro", ref Config.DistroMode); Toggle("Thin Mode", ref Config.ThinMode); Toggle("Auto-Cycle", ref Config.AutoCycleStatus); Toggle("Stylize All Text", ref Config.StylizeTextMode); DrawCombo("Pronouns", _pronounsList, ref Config.Pronouns, ref Config.CustomPronouns); DrawCombo("Country", _countriesList, ref Config.Country, ref Config.CustomCountry); string[] states = _statesMap.ContainsKey(Config.Country) ? _statesMap[Config.Country] : new[] { "Custom..." }; DrawCombo("State", states, ref Config.State, ref Config.CustomState); string[] cities = _citiesMap.ContainsKey(Config.State) ? _citiesMap[Config.State] : new[] { "Custom..." }; DrawCombo("City", cities, ref Config.City, ref Config.CustomCity); ImGui.SliderInt("Interval##slider", ref Config.Interval, 1, 60); }); Card("Weather", () => { Toggle("Enable Weather", ref Config.WeatherMode); Toggle("Show Temperature", ref Config.WeatherTempMode); Toggle("Emergency Alerts", ref Config.WeatherAlertMode); int tempIdx = Array.IndexOf(_tempUnits, Config.WeatherTempUnit); if (tempIdx < 0) tempIdx = 0; if (ImGui.Combo("Temp Unit##tempunit", ref tempIdx, _tempUnits, _tempUnits.Length)) { Config.WeatherTempUnit = _tempUnits[tempIdx]; SaveConfig(); } }); break; 
            case 3: Card("Hardware", () => { Toggle("Show Stats", ref Config.PcMode); Toggle("Show RAM", ref Config.ShowRam); if (Config.ShowRam) { ImGui.Indent(); Toggle("Show DDR Version", ref Config.RamDdrVersionOn); ImGui.Unindent(); } Toggle("Show VRAM", ref Config.ShowVram); if(RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) Toggle("VR Headset Battery", ref Config.VrBatteryMode); Toggle("Stylized Names", ref Config.HwNameMode); ImGui.Separator(); ImGui.Text("Sensors"); Toggle("CPU Temp", ref Config.CpuTempOn); Toggle("CPU Power (Wattage)", ref Config.CpuPowerOn); Toggle("GPU Temp", ref Config.GpuTempOn); Toggle("GPU Hotspot Temp", ref Config.GpuHotspotOn); Toggle("GPU Power (Wattage)", ref Config.GpuPowerOn); ImGui.Separator(); ImGui.Text("Custom Names"); Toggle("Custom CPU Name", ref Config.CustomCpuNameOn); if (Config.CustomCpuNameOn) ImGui.InputText("##c_cpu", ref Config.CustomCpuName, 32); Toggle("Custom GPU Name", ref Config.CustomGpuNameOn); if (Config.CustomGpuNameOn) ImGui.InputText("##c_gpu", ref Config.CustomGpuName, 32); }); break; 
            case 4: Card("Network", () => { Toggle("Internet Ping", ref Config.NetMode); ImGui.Dummy(new Vector2(0, 8)); ImGui.Text($"Avg: {NetworkStats.AvgPing}ms"); ImGui.Text($"Loss: {NetworkStats.PacketLoss}%"); }); break; 
            case 5: Card("Appearance", () => { var presets = ThemePresets.All; for (int i = 0; i < presets.Length; i++) { if (i > 0 && i % 4 != 0) ImGui.SameLine(0, 6); var p = presets[i]; var ac = new Vector4(p.Accent[0], p.Accent[1], p.Accent[2], 1f); ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(p.Bg[0], p.Bg[1], p.Bg[2], 1f)); ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(p.Card[0], p.Card[1], p.Card[2], 1f)); ImGui.PushStyleColor(ImGuiCol.Text, ac); if (ImGui.Button(p.Name, new Vector2(140, 40))) { Config.AccentColor = (float[])p.Accent.Clone(); Config.BgColor = (float[])p.Bg.Clone(); Config.SidebarColor = (float[])p.Sidebar.Clone(); Config.CardColor = (float[])p.Card.Clone(); ColAccent = V4(Config.AccentColor); ColBg = V4(Config.BgColor); ColSidebar = V4(Config.SidebarColor); ColCard = V4(Config.CardColor); ApplyTheme(); SaveConfig(); } ImGui.PopStyleColor(3); } }); Card("Colors", () => { ColorPicker3("Accent Color", Config.AccentColor, ref ColAccent, c => Config.AccentColor = c); ColorPicker3("Background", Config.BgColor, ref ColBg, c => Config.BgColor = c); ColorPicker3("Sidebar", Config.SidebarColor, ref ColSidebar, c => Config.SidebarColor = c); ColorPicker3("Card", Config.CardColor, ref ColCard, c => Config.CardColor = c); }); Card("Layout & Shape", () => { bool ch = false; float swW = Config.SidebarWidth; if (ImGui.SliderFloat("Sidebar Width", ref swW, 120, 280)) { Config.SidebarWidth = swW; ch = true; } float wr = Config.WindowRounding; if (ImGui.SliderFloat("Window Rounding", ref wr, 0, 14)) { Config.WindowRounding = wr; ch = true; } float cr = Config.ChildRounding; if (ImGui.SliderFloat("Card Rounding", ref cr, 0, 14)) { Config.ChildRounding = cr; ch = true; } float fr = Config.FrameRounding; if (ImGui.SliderFloat("Frame Rounding", ref fr, 0, 14)) { Config.FrameRounding = fr; ch = true; } float tr = Config.TabRounding; if (ImGui.SliderFloat("Tab Rounding", ref tr, 0, 14)) { Config.TabRounding = tr; ch = true; } float fs = Config.FontScale; if (ImGui.SliderFloat("Font Scale", ref fs, 0.7f, 1.8f)) { Config.FontScale = fs; ImGui.GetIO().FontGlobalScale = fs; ch = true; } if (ch) { ApplyTheme(); SaveConfig(); } }); break; 
            case 6: Card("Misc", () => { Toggle("EAS Alert Mode", ref Config.EasMode); Toggle("AFK Detection", ref Config.AfkDetectionMode); }); break; 
            case 7: Card("Updater", () => { if (ImGui.Button("Check for Update")) Task.Run(() => Updater.CheckForUpdates()); if (Updater.NewVersionFound && ImGui.Button("Apply Update")) Updater.ApplyUpdate(); ImGui.Text($"Status: {Updater.Status}"); ImGui.Text($"Version: {AppVersion}"); }); break; 
        } ImGui.EndChild(); ImGui.PopStyleColor(); ImGui.End(); }
        static void ColorPicker3(string l, float[] src, ref Vector4 col, Action<float[]> onChange) { var v = new Vector3(src[0], src[1], src[2]); if (ImGui.ColorEdit3(l, ref v)) { onChange(new[] { v.X, v.Y, v.Z }); col = new Vector4(v.X, v.Y, v.Z, 1f); ApplyTheme(); SaveConfig(); } }
        static void DrawCombo(string l, string[] items, ref string sel, ref string cV) { int i = Array.IndexOf(items, sel); if (i == -1) { if (!string.IsNullOrEmpty(sel) && sel != "Custom...") cV = sel; i = items.Length - 1; sel = "Custom..."; } if (ImGui.Combo(l, ref i, items, items.Length)) { sel = items[i]; SaveConfig(); } if (sel == "Custom..." && ImGui.InputText("Custom " + l, ref cV, 64)) SaveConfig(); }
        static void Card(string t, Action d) { ImGui.SetCursorPosX(24); ImGui.TextColored(ColAccent, t); ImGui.Dummy(new Vector2(0, 8)); ImGui.SetCursorPosX(24); ImGui.PushStyleColor(ImGuiCol.ChildBg, ColCard); ImGui.PushStyleColor(ImGuiCol.Text, DeriveText(ColCard)); ImGui.PushStyleColor(ImGuiCol.TextDisabled, DeriveSubText(ColCard)); ImGui.BeginChild($"##c{t}", new Vector2(ImGui.GetContentRegionAvail().X - 48, 0), ImGuiChildFlags.Borders | ImGuiChildFlags.AutoResizeY); ImGui.Dummy(new Vector2(0, 10)); d(); ImGui.Dummy(new Vector2(0, 10)); ImGui.EndChild(); ImGui.PopStyleColor(3); ImGui.Dummy(new Vector2(0, 10)); }
        static void Toggle(string l, ref bool v) { if (ImGui.Checkbox(l, ref v)) SaveConfig(); }
        public static void SaveConfig() 
        { 
            try 
            { 
                var dir = Path.GetDirectoryName(_path);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                    
                var options = new JsonSerializerOptions { WriteIndented = true, IncludeFields = true };
                File.WriteAllText(_path, JsonSerializer.Serialize(Config, options));
                Console.WriteLine($"[Config] Saved to {_path}");
            } 
            catch (Exception ex) 
            { 
                Console.WriteLine($"[Config Save Error]: {ex.Message}");
                Console.WriteLine($"[Config Path]: {_path}");
            } 
        }

        static void LoadConfig() 
        { 
            if (!File.Exists(_path)) return; 
    
            try 
            { 
                var rawJson = File.ReadAllText(_path); 
        
                // FIX: Define options ONCE with IncludeFields = true
                var options = new JsonSerializerOptions { 
                    IncludeFields = true, 
                    Converters = { new StatusItemConverter() } 
                };

                var jsonNode = System.Text.Json.Nodes.JsonNode.Parse(rawJson); 
        
                // Check for legacy StatusList (Array of strings)
                if (jsonNode["StatusList"] is System.Text.Json.Nodes.JsonArray sL && sL.All(n => n is System.Text.Json.Nodes.JsonValue)) 
                { 
                    var sLL = JsonSerializer.Deserialize<List<string>>(sL.ToJsonString()); 
            
                    // FIX: Use 'options' here so fields (IP, Toggles) are actually loaded!
                    var tC = JsonSerializer.Deserialize<AppConfig>(rawJson, options); 
            
                    if (tC != null && sLL != null) 
                    { 
                        tC.StatusList = sLL.Select(s => new StatusItem { Text = s }).ToList(); 
                        Config = tC; 
                        SaveConfig(); 
                        return; 
                    } 
                } 
        
                // Standard load
                var loaded = JsonSerializer.Deserialize<AppConfig>(rawJson, options); 
                if (loaded != null) Config = loaded; 
        
            } 
            catch (Exception ex) 
            { 
                // Log error so you know if loading fails
                Console.WriteLine($"[Config Load Error]: {ex.Message}");
            } 
        }
    }
    public class StatusItemConverter : JsonConverter<List<StatusItem>> { public override List<StatusItem> Read(ref Utf8JsonReader r, Type t, JsonSerializerOptions o) { if (r.TokenType == JsonTokenType.StartArray) { var l = new List<StatusItem>(); while (r.Read() && r.TokenType != JsonTokenType.EndArray) { if (r.TokenType == JsonTokenType.String) l.Add(new StatusItem { Text = r.GetString() }); else if (r.TokenType == JsonTokenType.StartObject) l.Add(JsonSerializer.Deserialize<StatusItem>(ref r, o)); } return l; } return new List<StatusItem>(); } public override void Write(Utf8JsonWriter w, List<StatusItem> v, JsonSerializerOptions o) => JsonSerializer.Serialize(w, v, o); }
}