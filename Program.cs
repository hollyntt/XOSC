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
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Numerics;
using System.Runtime.InteropServices;
using Raylib_cs;
using ImGuiNET;
using rlImGui_cs;

// Lets see how awesome and skidded this program is!!!!!!!!!!1

namespace XOSC
{
    public class AppConfig
    {
        public bool ChatboxEnabled = true;
        public int Interval = 1;
        public string City = "";
        public string Pronouns = "";
        public string StatusIcon = "";
        public bool ThinMode = false;
        public bool PcMode = false;
        public bool ShowRam = false;
        public bool ShowVram = false;
        public bool DistroMode = false;
        public bool WeatherMode = false;
        public bool TimeMode = false;
        public bool SongMode = true;
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
        public string CpuUnit = "%";
        public string RamUnit = "GB";
        public string GpuUnit = "%";
        public string VramUnit = "GB";
        public string TempUnit = "°C";
        public List<string> StatusList = new() { };
        public bool AutoCycleStatus = false;
        public string PublishPath = "";
        public string Cookie = "";
        public string SavedVersion = "";
    } // normal

    public static class HardwareService
    {
        private static long _lastTotal, _lastIdle;
        private static bool IsLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
        public static string GetCpuLoad()
        {
            try {
                if (!IsLinux || !File.Exists("/proc/stat")) return "--";
                var parts = File.ReadLines("/proc/stat").First().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                long idle = long.Parse(parts[4]), total = parts.Skip(1).Select(long.Parse).Sum();
                long dIdle = idle - _lastIdle, dTotal = total - _lastTotal;
                _lastIdle = idle; _lastTotal = total;
                return dTotal == 0 ? "0%" : Math.Round(100.0 * (1.0 - (double)dIdle / dTotal), 0) + "%";
            } catch { return "--"; }
        }
        public static string GetCpuTemp(string unit)
        {
            if (!IsLinux) return "--";
            try {
                string path = "";
                if (Directory.Exists("/sys/class/hwmon/")) {
                    foreach (var dir in Directory.GetDirectories("/sys/class/hwmon/")) {
                        if (!File.Exists($"{dir}/name")) continue;
                        if (File.ReadAllText($"{dir}/name").Contains("k10temp") || File.ReadAllText($"{dir}/name").Contains("coretemp")) {
                            if (File.Exists($"{dir}/temp1_input")) { path = $"{dir}/temp1_input"; break; }
                        }
                    }
                }
                if (string.IsNullOrEmpty(path)) return "--";
                int c = int.Parse(File.ReadAllText(path).Trim()) / 1000;
                return unit == "°C" ? $"{c}°C" : $"{(c * 9 / 5) + 32}°F";
            } catch { return "--"; }
        }
        public static (string Load, string Vram, string Temp) GetGpuStats(string gUnit, string vUnit, string tUnit)
        {
            string smi = IsLinux ? "/usr/bin/nvidia-smi" : "C:\\Windows\\System32\\nvidia-smi.exe"; // good app for dogshit gpu company
            if (File.Exists(smi)) {
                try {
                    var psi = new ProcessStartInfo(smi, "--query-gpu=utilization.gpu,memory.used,memory.total,temperature.gpu --format=csv,noheader,nounits") { RedirectStandardOutput = true, UseShellExecute = false };
                    using var p = Process.Start(psi);
                    var v = p?.StandardOutput.ReadToEnd().Trim().Split(',');
                    if (v?.Length >= 4) {
                        string l = v[0].Trim() + "%";
                        long u = long.Parse(v[1].Trim()), t = long.Parse(v[2].Trim());
                        string vr = vUnit == "GB" ? $"{Math.Round(u / 1024.0, 1)}/{Math.Round(t / 1024.0, 1)}GB" : $"{(u * 100 / t)}%";
                        string tmp = tUnit == "°C" ? $"{v[3].Trim()}°C" : $"{(int.Parse(v[3].Trim()) * 9 / 5) + 32}°F";
                        return (l, vr, tmp);
                    }
                } catch { }
            }
            if (!IsLinux) return ("--", "--", "--");
            try {
                string gpu = Directory.GetDirectories("/sys/class/drm/").Where(d => d.Contains("card")).OrderByDescending(d => File.Exists($"{d}/device/mem_info_vram_total") ? long.Parse(File.ReadAllText($"{d}/device/mem_info_vram_total").Trim()) : 0).FirstOrDefault() ?? "";
                if (string.IsNullOrEmpty(gpu)) return ("--", "--", "--");
                string l = File.Exists($"{gpu}/device/gpu_busy_percent") ? File.ReadAllText($"{gpu}/device/gpu_busy_percent").Trim() + "%" : "--%";
                long used = long.Parse(File.ReadAllText($"{gpu}/device/mem_info_vram_used").Trim()), total = long.Parse(File.ReadAllText($"{gpu}/device/mem_info_vram_total").Trim());
                string vr = vUnit == "GB" ? $"{Math.Round(used / 1073741824.0, 1)}/{Math.Round(total / 1073741824.0, 1)}GB" : $"{Math.Round(100.0 * used / total, 0)}%";
                string tmp = "--", hw = $"{gpu}/device/hwmon/";
                if (Directory.Exists(hw)) {
                    var d = Directory.GetDirectories(hw).FirstOrDefault();
                    if (d != null && File.Exists($"{d}/temp1_input")) { int c = int.Parse(File.ReadAllText($"{d}/temp1_input").Trim()) / 1000; tmp = tUnit == "°C" ? $"{c}°C" : $"{(c * 9 / 5) + 32}°F"; }
                }
                return (l, vr, tmp);
            } catch { return ("--", "--", "--"); }
        }
        public static string GetRamUsage(string unit)
        {
            if (!IsLinux) return "??GB";
            try {
                var m = File.ReadLines("/proc/meminfo").ToList();
                long t = long.Parse(m.FirstOrDefault(x => x.StartsWith("MemTotal:"))?.Split(' ', StringSplitOptions.RemoveEmptyEntries)[1] ?? "0");
                long a = long.Parse(m.FirstOrDefault(x => x.StartsWith("MemAvailable:"))?.Split(' ', StringSplitOptions.RemoveEmptyEntries)[1] ?? "0");
                return unit == "GB" ? $"{Math.Round((t - a) / 1048576.0, 1)}/{Math.Round(t / 1048576.0, 1)}GB" : $"{Math.Round(100.0 * (t - a) / t, 0)}%";
            } catch { return "--"; }
        }
    }

    public static class Updater
    {
        public static string Status = "idle"; // zZzZ
        public static bool NewVersionFound = false;
        private static byte[]? _pendingData;
        private const string StableApiUrl = "https://api.github.com/repos/hollyntt/XOSC/releases/latest"; // hi no im not a token logger, im on github so chill out yo

            public static async Task CheckForUpdates()
            {
                Status = "checking GitHub..."; NewVersionFound = false;
                try {
                    using var http = new HttpClient();
                    http.DefaultRequestHeaders.Add("User-Agent", "XOSC-Updater");

                    var resp = await http.GetStringAsync(StableApiUrl);
                    using var doc = JsonDocument.Parse(resp);
                    string latestTag = doc.RootElement.GetProperty("tag_name").GetString() ?? "";

                    if (latestTag == Program.AppVersion) {
                        Status = "already up to date";
                        return;
                    }

                    var asset = doc.RootElement.GetProperty("assets").EnumerateArray()
                    .FirstOrDefault(a => a.GetProperty("name").GetString() == "XOSC.zip");

                    string downloadUrl = asset.GetProperty("browser_download_url").GetString() ?? "";
                    if (string.IsNullOrEmpty(downloadUrl)) { Status = "XOSC.zip not found in release"; return; }

                    Status = "downloading...";
                    var zipData = await http.GetByteArrayAsync(downloadUrl);
                    using var ms = new MemoryStream(zipData);
                    using var archive = new ZipArchive(ms);
                    var entry = archive.GetEntry("linux-x64/XOSC") ?? archive.GetEntry("XOSC");

                    if (entry != null) {
                        using var es = entry.Open(); using var msw = new MemoryStream();
                        await es.CopyToAsync(msw); _pendingData = msw.ToArray(); // why the line yellow yo, is that a piss stream??
                        Status = $"Update Found: {latestTag}"; NewVersionFound = true;
                    }
                } catch (Exception e) { Status = $"error: {e.Message}"; }
            }

            public static void ApplyUpdate()
            {
                if (_pendingData == null) return;
                try {
                    string self = Environment.ProcessPath!;
                    File.Move(self, self + ".bak", true);
                    File.WriteAllBytes(self, _pendingData);
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) Process.Start("chmod", $"+x \"{self}\"").WaitForExit();
                    Thread.Sleep(500); // yo, ig this is the method
                    Process.Start(new ProcessStartInfo(self) { UseShellExecute = true });
                    Environment.Exit(0);
                } catch { }
            }
    }

    public static class MusicChatEngine // wat
    {
        private static UdpClient _client = new();
        private static CancellationTokenSource? _cts;
        private static int _statusIdx = 0;
        private static string _cpu = "CPU", _gpu = "GPU", _music = "Chilling", _weather = "...";
        private static DateTime _lastRefresh = DateTime.MinValue, _manualExpiry = DateTime.MinValue, _lastSent = DateTime.MinValue;
        private static string _manualMsg = "";
        public static int PacketsSent = 0;
        public static string EngineState = "Idle"; // ZzZz
        public static readonly object ListLock = new();

        public static void Init() { _client = new UdpClient(); _cts?.Cancel(); _cts = new CancellationTokenSource(); ScrapeHardwareNames(); Task.Run(() => Loop(_cts.Token)); }
        public static void SetManual(string m) { _manualMsg = m; _manualExpiry = DateTime.Now.AddSeconds(20); }
        private static async Task Loop(CancellationToken t) { while (!t.IsCancellationRequested) { if (Program.Config.ChatboxEnabled) try { await Update(); } catch { } await Task.Delay(1000, t); } }

        private static async Task Update()
        {
            var cfg = Program.Config;
            if (DateTime.Now < _manualExpiry) { EngineState = "Manual"; SendOsc("/chatbox/input", $"💬 {_manualMsg}"); return; }
            if ((DateTime.Now - _lastRefresh).TotalSeconds >= cfg.Interval) {
                _music = FetchMusic(); if (cfg.WeatherMode && !string.IsNullOrEmpty(cfg.City)) _weather = (await new HttpClient().GetStringAsync($"https://wttr.in/{cfg.City}?format=%C+%t")).Trim();
                lock (ListLock) { if (cfg.AutoCycleStatus && cfg.StatusList.Count > 0) _statusIdx = (_statusIdx + 1) % cfg.StatusList.Count; }
                _lastRefresh = DateTime.Now;
            }
            if ((DateTime.Now - _lastSent).TotalSeconds < Math.Max(cfg.Interval, 1.5)) return;
            var lines = new List<string>();
            lock (ListLock) { if (cfg.StatusTextMode && cfg.StatusList.Count > _statusIdx) { if (_statusIdx >= cfg.StatusList.Count) _statusIdx = 0; lines.Add(cfg.StatusList[_statusIdx]); } }
            if (cfg.PronounsMode && !string.IsNullOrEmpty(cfg.Pronouns)) lines.Add($"{cfg.StatusIcon} {cfg.Pronouns}");
            var env = new List<string>();
            if (cfg.TimeMode) env.Add($"🕒 {DateTime.Now:hh:mm tt}");
            if (cfg.DistroMode) env.Add("| " + (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "Windows" : "Fedora"));
            if (cfg.WeatherMode) env.Add($"| 🌤️ {_weather}");
            if (env.Count > 0) lines.Add(string.Join(" ", env));
            if (cfg.PcMode) {
                var g = HardwareService.GetGpuStats(cfg.GpuUnit, cfg.VramUnit, cfg.TempUnit);
                string cpuL = cfg.CustomCpuNameOn ? cfg.CustomCpuName : (cfg.HwNameMode ? Stylize(_cpu) : "CPU");
                string gpuL = cfg.CustomGpuNameOn ? cfg.CustomGpuName : (cfg.HwNameMode ? Stylize(_gpu) : "GPU");
                lines.Add($"🖥️ {cpuL}: {HardwareService.GetCpuLoad()}" + (cfg.CpuTempOn ? $" ({HardwareService.GetCpuTemp(cfg.TempUnit)})" : "") + $" | 🎮 {gpuL}: {g.Load} ({g.Temp})");
                var mem = new List<string>();
                if (cfg.ShowRam) mem.Add($"🐏 ʳᵃᵐ: {HardwareService.GetRamUsage(cfg.RamUnit)}");
                if (cfg.ShowVram) mem.Add($"🎞️ ᵛʳᵃᵐ: {g.Vram}");
                if (mem.Count > 0) lines.Add(string.Join(" | ", mem));
            }
            if (cfg.NetMode) lines.Add($"🌐 {new System.Net.NetworkInformation.Ping().Send("1.1.1.1", 300).RoundtripTime}ms");
            if (cfg.SongMode && _music != "Chilling") lines.Add($"♪ {_music}");
            string output = string.Join("\n", lines);
            if (cfg.ThinMode) { if (output.Length > 138) output = output.Substring(0, 138); output += "\u0003\u001f"; }
            SendOsc("/chatbox/input", output); _lastSent = DateTime.Now; PacketsSent++;
        } // Always code as if the guy who ends up maintaining you code will be a hypersexual rapist furry who knows what your private e621 account is

        private static void SendOsc(string addr, string text) {
            try { List<byte> p = new(); void Add(string s) { byte[] b = Encoding.UTF8.GetBytes(s); p.AddRange(b); p.Add(0); while (p.Count % 4 != 0) p.Add(0); }
            Add(addr); Add(",sTT"); Add(text); _client.Send(p.ToArray(), p.Count, "127.0.0.1", 9000);
            } catch { }
        }

        private static string FetchMusic() {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                StringBuilder buff = new StringBuilder(256); IntPtr handle = GetForegroundWindow();
                if (GetWindowText(handle, buff, 256) > 0) {
                    string t = buff.ToString(); if (t.Contains("Spotify") || t.Contains("YouTube") || t.Contains("SoundCloud"))
                    return Regex.Replace(t, @" - (Spotify|YouTube|SoundCloud).*", "").Trim();
                }
                return "Chilling";
            }
            try {
                var psi = new ProcessStartInfo("playerctl", "metadata --format \"{{artist}} - {{title}}\"") { RedirectStandardOutput = true, UseShellExecute = false };
                using var p = Process.Start(psi); string r = p?.StandardOutput.ReadToEnd().Trim() ?? "";
                return (string.IsNullOrEmpty(r) || r == " - ") ? "Chilling" : r;
            } catch { return "Chilling"; }
        }

        private static void ScrapeHardwareNames() {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return;
            string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string p = Path.Combine(home, ".local/share/Steam/steamapps/compatdata/438100/pfx/drive_c/users/steamuser/AppData/LocalLow/VRChat/VRChat"); // yes yes hardcoding yes yes
            if (Directory.Exists(p)) {
                var log = Directory.GetFiles(p, "output_log_*.txt").OrderByDescending(File.GetLastWriteTime).FirstOrDefault();
                if (log != null) foreach (var l in File.ReadLines(log).Take(1500)) {
                    if (l.Contains("Processor Type:")) _cpu = Regex.Replace(l.Split(": ")[1], @"(\s\d+-Core| Processor| @.*|AMD |Intel |Core |Ryzen )", "").Trim();
                    if (l.Contains("Graphics Device Name:")) _gpu = Regex.Replace(l.Split(": ")[1], @"(\(RADV.*| Graphics|AMD |NVIDIA |GeForce |Radeon |\sRX\s|\sXT)", "").Trim();
                }
            }
        }
        private static string Stylize(string t) {
            string n = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789", s = "ᵃᵇᶜᵈᵉᶠᵍʰᶦʲᵏˡᵐⁿᵒᵖᵠʳˢᵗᵘᵛʷˣʸᶻᵃᵇᶜᵈᵉᶠᵍʰᶦʲᵏˡᵐⁿᵒᵖᵠʳˢᵗᵘᵛʷˣʸᶻ⁰¹²³⁴⁵⁶⁷⁸⁹";
            StringBuilder sb = new(); foreach (char c in t) { int i = n.IndexOf(c); sb.Append(i != -1 ? s[i] : c); }
            return sb.ToString();
        }
        [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow(); // literally magicchatbox has this in their code too i don't mind a little bits of CTRL+C+V
        [DllImport("user32.dll")] private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);
    }

    class Program
    {
        private static string GetConfigPath()
        {
            // 1. Check if XDG_CONFIG_HOME is configured. Should be for most Linux distros
            string? xdgConfigHome = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
            string baseDir;
            if (!string.IsNullOrEmpty(xdgConfigHome))
            {
                baseDir = xdgConfigHome;
            }
            else
            {
                // 2. If not, just assume .config. This is the place you can decide to hardcode it if you want. Probably better to use ~/xosc instead however
                string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                baseDir = Path.Combine(home, ".config");
            }
            // 3. Create the actual xosc folder and config file
            return Path.Combine(baseDir, "xosc", "config.json");
        }
        public const string AppVersion = "f61994b"; // oh cool this is automated by publish nice
        public static AppConfig Config = new();
        private static string _path = GetConfigPath();
        private static string _chatIn = "";
        private static Mutex? _mtx; private static int _navPage = 0;
        private static readonly string[] _navLabels = { "Dashboard", "Statuses", "Chatbox", "Hardware", "Network", "Updater" };
        private static readonly Vector4 ColAccent = new(0.38f, 0.73f, 1.00f, 1f), ColBg = new(0.10f, 0.10f, 0.13f, 1f), ColSidebar = new(0.07f, 0.07f, 0.09f, 1f), ColCard = new(0.14f, 0.14f, 0.18f, 1f);

        public static void Main() {
            _mtx = new Mutex(true, "XOSC_VRC_Unique_REL", out bool fresh); if (!fresh) Environment.Exit(0);
            Directory.CreateDirectory(Path.GetDirectoryName(_path)); LoadConfig();
            if (Config.SavedVersion != AppVersion) { Config.SavedVersion = AppVersion; SaveConfig(); }
            MusicChatEngine.Init(); Raylib.InitWindow(960, 640, "XOSC"); Raylib.SetWindowState(ConfigFlags.ResizableWindow); rlImGui.Setup(true); Raylib.SetTargetFPS(60); ApplyTheme();
            while (!Raylib.WindowShouldClose()) { Raylib.BeginDrawing(); Raylib.ClearBackground(new Color(26, 26, 33, 255)); rlImGui.Begin(); DrawUI(); rlImGui.End(); Raylib.EndDrawing(); }
            SaveConfig(); Raylib.CloseWindow();
        }
        static void ApplyTheme() {
            var s = ImGui.GetStyle(); s.WindowRounding = 0; s.ChildRounding = 6; s.FrameRounding = 5; s.PopupRounding = 5; s.ScrollbarRounding = 5; s.GrabRounding = 4; s.TabRounding = 5; s.WindowPadding = new Vector2(12, 12);
        }
        static void DrawUI() {
            int sw = Raylib.GetScreenWidth(), sh = Raylib.GetScreenHeight();
            ImGui.SetNextWindowPos(Vector2.Zero); ImGui.SetNextWindowSize(new Vector2(sw, sh));
            ImGui.Begin("##root", ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoScrollbar);
            ImGui.PushStyleColor(ImGuiCol.ChildBg, ColSidebar);
            ImGui.BeginChild("##sidebar", new Vector2(172, sh), ImGuiChildFlags.None, ImGuiWindowFlags.NoScrollbar);
            ImGui.Dummy(new Vector2(0, 20)); ImGui.SetCursorPosX(20); ImGui.TextColored(ColAccent, "XOSC");
            ImGui.SetCursorPosX(20); ImGui.TextColored(new Vector4(0.5f, 0.5f, 0.6f, 1f), $"Commit: {AppVersion}"); ImGui.Dummy(new Vector2(0, 20)); // Changed it a little bit here to clarificate better
            for (int i = 0; i < _navLabels.Length; i++) {
                bool active = _navPage == i; ImGui.PushStyleColor(ImGuiCol.Button, active ? new Vector4(0.38f, 0.73f, 1.00f, 0.13f) : Vector4.Zero); ImGui.PushStyleColor(ImGuiCol.Text, active ? ColAccent : new Vector4(0.72f, 0.72f, 0.80f, 1f));
                ImGui.SetCursorPosX(10); if (ImGui.Button(_navLabels[i], new Vector2(152, 36))) _navPage = i; ImGui.PopStyleColor(2);
            }
            ImGui.EndChild(); ImGui.PopStyleColor(); ImGui.SameLine();
            ImGui.PushStyleColor(ImGuiCol.ChildBg, ColBg);
            ImGui.BeginChild("##content", new Vector2(sw - 172, sh), ImGuiChildFlags.None, ImGuiWindowFlags.NoScrollbar); ImGui.Dummy(new Vector2(0, 24));
            switch (_navPage) {
                case 0: Card("Dashboard", () => {
                    ImGui.Text($"Engine State: {MusicChatEngine.PacketsSent}");
                    ImGui.InputText("Override##field", ref _chatIn, 128); if (ImGui.Button("Send")) { MusicChatEngine.SetManual(_chatIn); _chatIn = ""; }
                }); break;
                case 1: Card("Statuses", () => {
                    int toRemove = -1; lock (MusicChatEngine.ListLock) {
                        for (int i = 0; i < Config.StatusList.Count; i++) {
                            string s = Config.StatusList[i]; ImGui.PushID(i);
                            if (ImGui.InputText("##s", ref s, 100)) Config.StatusList[i] = s;
                            ImGui.SameLine(); if (ImGui.Button("X")) toRemove = i;
                            ImGui.PopID();
                        }
                        if (toRemove != -1) { Config.StatusList.RemoveAt(toRemove); SaveConfig(); }
                    }
                    if (ImGui.Button("+ Add")) Config.StatusList.Add("New Status");
                }); break;
                case 2: Card("Chatbox", () => {
                    Toggle("Status Text", ref Config.StatusTextMode); Toggle("Pronouns", ref Config.PronounsMode);
                    Toggle("Song Mode", ref Config.SongMode); Toggle("Time", ref Config.TimeMode);
                    Toggle("Distro", ref Config.DistroMode); Toggle("Weather", ref Config.WeatherMode);
                    Toggle("Thin Mode", ref Config.ThinMode); Toggle("Auto-Cycle", ref Config.AutoCycleStatus);
                    ImGui.InputText("Pronouns##field", ref Config.Pronouns, 64);
                    ImGui.InputText("Weather City##field", ref Config.City, 64);
                    ImGui.SliderInt("Interval##slider", ref Config.Interval, 1, 60);
                }); break;
                case 3: Card("Hardware", () => {
                    Toggle("Show Stats", ref Config.PcMode); Toggle("Show RAM", ref Config.ShowRam);
                    Toggle("Show VRAM", ref Config.ShowVram); Toggle("Stylized Names", ref Config.HwNameMode);
                    Toggle("CPU Temp", ref Config.CpuTempOn); Toggle("GPU Temp", ref Config.GpuTempOn);
                    Toggle("Custom CPU Name", ref Config.CustomCpuNameOn); if (Config.CustomCpuNameOn) ImGui.InputText("##c_cpu", ref Config.CustomCpuName, 32);
                    Toggle("Custom GPU Name", ref Config.CustomGpuNameOn); if (Config.CustomGpuNameOn) ImGui.InputText("##c_gpu", ref Config.CustomGpuName, 32);
                }); break;
                case 4: Card("Network", () => { Toggle("Internet Ping", ref Config.NetMode); }); break;
                case 5: Card("Updater", () => {
                    if (ImGui.Button("Check for Update")) Task.Run(() => Updater.CheckForUpdates());
                    if (Updater.NewVersionFound && ImGui.Button("Apply Update")) Updater.ApplyUpdate();
                    ImGui.Text($"Status: {Updater.Status}");
                }); break;
            }
            ImGui.EndChild(); ImGui.PopStyleColor(); ImGui.End();
        }
        static void Card(string t, Action d) {
            ImGui.SetCursorPosX(24); ImGui.TextColored(new Vector4(0.92f, 0.92f, 0.97f, 1f), t); ImGui.Dummy(new Vector2(0, 8));
            ImGui.SetCursorPosX(24); ImGui.PushStyleColor(ImGuiCol.ChildBg, ColCard);
            ImGui.BeginChild($"##c{t}", new Vector2(ImGui.GetContentRegionAvail().X - 48, 0), ImGuiChildFlags.Borders | ImGuiChildFlags.AutoResizeY);
            ImGui.Dummy(new Vector2(0, 10)); d(); ImGui.Dummy(new Vector2(0, 10)); ImGui.EndChild(); ImGui.PopStyleColor();
        }
        static void Toggle(string l, ref bool v) { if (ImGui.Checkbox(l, ref v)) SaveConfig(); }
        public static void SaveConfig() { try { Directory.CreateDirectory(Path.GetDirectoryName(_path)); var options = new JsonSerializerOptions { WriteIndented = true, IncludeFields = true }; File.WriteAllText(_path, JsonSerializer.Serialize(Config, options)); } catch { } }
        static void LoadConfig() { if (!File.Exists(_path)) return; try { var options = new JsonSerializerOptions { IncludeFields = true }; var loaded = JsonSerializer.Deserialize<AppConfig>(File.ReadAllText(_path), options); if (loaded != null) Config = loaded; } catch { } }
    } // nevermind i def wrote all of this by hand, i must hate myself
}
