using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using XOSC.Motor.Extentions;

namespace XOSC.Motor.Engines;

public static class MusicChatEngine
{
    private static CancellationTokenSource? _cts;
    private static int _statusIdx = 0;
    private static bool _showHardwareTick = false;
    private static string _cpu = "CPU", _gpu = "GPU";
    private static readonly IPlatformEngine _platformEngine = PlatformEngine.Create();

    private static DateTime _lastWeatherFetch = DateTime.MinValue;
    private static int _weatherCode = 0;
    private static double _weatherTempC = 0;
    private static MusicData _musicData = new("Chilling", 0, 0);
    private static DateTime _lastR = DateTime.MinValue, _lastS = DateTime.MinValue, _manualE = DateTime.MinValue;
    private static string _manualM = "";
    private static int _manualSoundPending;
    private static int _isManuallyTyping;
    private static long _manualTypingUntilTicks;
    public static int PacketsSent = 0;
    public static string EngineState = "Idle";
    private static string _lastChatboxMessage = "";
    public static string LastChatboxMessage => Volatile.Read(ref _lastChatboxMessage);
    public static readonly object ListLock = new();
    private static Random _visRand = new Random();
    private static string[] _visBars = { " ", "▂", "▃", "▄", "▅", "▆", "▇", "█" };

    public static void Init() 
    { 
        OscEngine.Initialize();
        _cts?.Cancel(); 
        _cts = new CancellationTokenSource(); 
        HardwareService.Initialize(); 
        NetworkStats.Start(); 
        ScrapeHardwareNames(); 
        _platformEngine.Start(); 
        Task.Run(() => Loop(_cts.Token)); 
    }

    public static void SetManual(string m)
    {
        StopManualTyping();
        _manualM = m;
        _manualE = DateTime.Now.AddSeconds(20);
        Interlocked.Exchange(ref _manualSoundPending, Program.Config.ManualSendSound ? 1 : 0);
    }

    public static void NotifyManualTyping()
    {
        if (!Program.Config.ChatboxEnabled)
            return;

        Interlocked.Exchange(ref _manualTypingUntilTicks, DateTime.UtcNow.AddSeconds(2).Ticks);
        if (Interlocked.Exchange(ref _isManuallyTyping, 1) == 0)
            OscEngine.SendChatboxTyping(true);
    }

    private static void StopManualTyping()
    {
        Interlocked.Exchange(ref _manualTypingUntilTicks, 0);
        if (Interlocked.Exchange(ref _isManuallyTyping, 0) == 1)
            OscEngine.SendChatboxTyping(false);
    }

    private static async Task Loop(CancellationToken t) 
    { 
        while (!t.IsCancellationRequested) 
        { 
            if (Program.Config.ChatboxEnabled) try { await Update(); } catch { } 
            if (!Program.Config.ChatboxEnabled || DateTime.UtcNow.Ticks >= Interlocked.Read(ref _manualTypingUntilTicks))
                StopManualTyping();
            await Task.Delay(1000, t); 
        } 
    }

    private static async Task<(double lat, double lon)> GetCoordinatesAsync() { var cfg = Program.Config; string search = !string.IsNullOrWhiteSpace(cfg.CustomCity) ? cfg.CustomCity : cfg.City; if (!string.IsNullOrWhiteSpace(search)) { try { using var http = new HttpClient(); var q = Uri.EscapeDataString(search); var j = await http.GetStringAsync($"https://geocoding-api.open-meteo.com/v1/search?name={q}&count=1"); using var doc = JsonDocument.Parse(j); var r = doc.RootElement.GetProperty("results"); if (r.ValueKind == JsonValueKind.Array && r.GetArrayLength() > 0) { var f = r[0]; return (f.GetProperty("latitude").GetDouble(), f.GetProperty("longitude").GetDouble()); } } catch { } } try { using var http = new HttpClient(); http.DefaultRequestHeaders.Add("User-Agent", "XOSC-Weather"); var j = await http.GetStringAsync("https://ipapi.co/json/"); using var doc = JsonDocument.Parse(j); return (doc.RootElement.GetProperty("latitude").GetDouble(), doc.RootElement.GetProperty("longitude").GetDouble()); } catch { } return (39.78, -89.65); }
    private static async Task FetchWeatherAsync(double lat, double lon) { try { using var http = new HttpClient(); var u = $"https://api.open-meteo.com/v1/forecast?latitude={lat}&longitude={lon}&current=weather_code,temperature_2m"; var r = await http.GetStringAsync(u); using var doc = JsonDocument.Parse(r); var c = doc.RootElement.GetProperty("current"); _weatherCode = c.GetProperty("weather_code").GetInt32(); _weatherTempC = c.GetProperty("temperature_2m").GetDouble(); } catch { } }
    private static string WeatherCodeToString(int code, double tempC, string unit) { string condition = code switch { 0 => "☀️ Clear", 1 => "🌤️ Mostly Clear", 2 => "⛅ Partly Cloudy", 3 => "☁️ Overcast", 45 or 48 => "🌫️ Foggy", 51 or 53 or 55 => "🌦️ Drizzle", 56 or 57 => "🌨️ Freezing Drizzle", 61 or 63 or 65 => "🌧️ Rain", 66 or 67 => "🌨️ Freezing Rain", 71 or 73 or 75 => "❄️ Snow", 77 => "🌨️ Snow Grains", 80 or 81 or 82 => "🌧️ Showers", 85 or 86 => "❄️ Snow Showers", 95 => "⛈️ Thunderstorm", 96 or 99 => "⛈️ Thunderstorm w/ Hail", _ => $"🌡️ Code {code}" }; if (!Program.Config.WeatherTempMode) return condition; string tempStr = unit == "°F" ? $"{Math.Round(tempC * 9.0 / 5.0 + 32, 0)}°F" : $"{Math.Round(tempC, 0)}°C"; return $"{condition} {tempStr}"; }

    private static async Task Update()
    {
        var cfg = Program.Config;
        if (DateTime.Now < _manualE) { EngineState = "Manual"; string manualMessage = ApplyThinMode($"💬 {_manualM}"); Volatile.Write(ref _lastChatboxMessage, manualMessage); bool notificationSound = Interlocked.Exchange(ref _manualSoundPending, 0) == 1; OscEngine.SendChatboxInput(manualMessage, notificationSound); return; }
        if ((DateTime.Now - _lastR).TotalSeconds >= cfg.Interval) { _musicData = _platformEngine.GetMusicData(); if (cfg.WeatherMode) { var c = await GetCoordinatesAsync(); await FetchWeatherAsync(c.lat, c.lon); } if (cfg.NetMode) await NetworkStats.UpdateAsync(); _lastR = DateTime.Now; }
        if (cfg.AfkDetectionMode) AfkEngine.Update();
        if ((DateTime.Now - _lastS).TotalSeconds < Math.Max(cfg.Interval, 1.5)) return;

        if (!cfg.WeatherMode) _weatherCode = 0;
        var p1 = new List<string>(); bool statusAdded = false; string sText = null;

        if (cfg.AfkDetectionMode && AfkEngine.IsAfk) { string afkStr = string.IsNullOrEmpty(AfkEngine.AfkDuration) ? "💤 AFK" : $"AFK for {AfkEngine.AfkDuration}"; p1.Add(afkStr); }
        lock (ListLock) { if (cfg.StatusTextMode && cfg.StatusList.Count > 0) { if (_statusIdx >= cfg.StatusList.Count) _statusIdx = 0; sText = cfg.StatusList[_statusIdx].Text; p1.Add(AfkEngine.IsAfk ? "AFK" : sText); statusAdded = true; } }
        string pr = cfg.Pronouns == "Custom..." ? cfg.CustomPronouns : cfg.Pronouns;
        if (cfg.PronounsMode && !string.IsNullOrEmpty(pr)) p1.Add($"{cfg.StatusIcon} {(cfg.StylizeTextMode ? Stylize(pr) : pr)}");
        var e1 = new List<string>();
        if (cfg.TimeMode) { string t = DateTime.Now.ToString(cfg.MilitaryTime ? "HH:mm" : "hh:mm tt"); e1.Add($"🕒 {(cfg.StylizeTextMode ? Stylize(t) : t)}"); }
        if (cfg.DistroMode) { string dn = _platformEngine.GetOperatingSystemName(); e1.Add(cfg.StylizeTextMode ? Stylize(dn) : dn); }
        if (cfg.WeatherMode) e1.Add(WeatherCodeToString(_weatherCode, _weatherTempC, cfg.WeatherTempUnit));
        if (e1.Count > 0) p1.Add(string.Join(" | ", e1));
        if (cfg.SongMode) {
            string sTitle = _musicData.Title == "Chilling" ? "Chilling" : _musicData.Title;
            string sStr = $"🎵 {(cfg.StylizeTextMode ? Stylize(sTitle) : sTitle)}";
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
            if (sText != null) p2.Add(AfkEngine.IsAfk ? "AFK" : sText);
            var e2 = new List<string>();
            if (cfg.TimeMode) { string t2 = DateTime.Now.ToString(cfg.MilitaryTime ? "HH:mm" : "hh:mm tt"); e2.Add($"🕒 {(cfg.StylizeTextMode ? Stylize(t2) : t2)}"); }
            if (cfg.DistroMode) { string dn2 = _platformEngine.GetOperatingSystemName(); e2.Add(cfg.StylizeTextMode ? Stylize(dn2) : dn2); }
            if (e2.Count > 0) p2.Add(string.Join(" | ", e2));
            
            string cS = cfg.HwNameMode ? StatsComponentType.CPU.GetSmallName() : "CPU"; string gS = cfg.HwNameMode ? StatsComponentType.GPU.GetSmallName() : "GPU"; string rS = cfg.HwNameMode ? StatsComponentType.RAM.GetSmallName() : "RAM"; string vS = cfg.HwNameMode ? StatsComponentType.VRAM.GetSmallName() : "VRAM";
            string cId = cfg.CustomCpuNameOn ? (cfg.HwNameMode ? Stylize(cfg.CustomCpuName) : cfg.CustomCpuName) : (cfg.HwNameMode ? Stylize(_cpu) : cS);
            string cL = $"🖥️ {cId}: {HardwareService.CpuLoad}"; List<string> cEx = new(); if (cfg.CpuTempOn) cEx.Add(HardwareService.CpuTemp); if (cfg.CpuPowerOn) cEx.Add(HardwareService.CpuPower); if (cEx.Count > 0) cL += $" ({string.Join(" / ", cEx)})";
            
            string gId = cfg.CustomGpuNameOn ? (cfg.HwNameMode ? Stylize(cfg.CustomGpuName) : cfg.CustomGpuName) : (cfg.HwNameMode ? Stylize(_gpu) : gS);
            string gL = $"🎮 {gId}: {HardwareService.GpuLoad}"; List<string> gEx = new(); if (cfg.GpuTempOn) gEx.Add(HardwareService.GpuTemp); if (cfg.GpuHotspotOn) gEx.Add($"H {HardwareService.GpuHotspot}"); if (cfg.GpuPowerOn) gEx.Add(HardwareService.GpuPower); if (gEx.Count > 0) gL += $" ({string.Join(" / ", gEx)})";
            
            p2.Add(cL);
            p2.Add(gL);
            
            var mem = new List<string>(); 
            if (cfg.ShowRam) { string ddr = (cfg.RamDdrVersionOn && !string.IsNullOrEmpty(HardwareService.RamDdr)) ? $" ⁽{HardwareService.RamDdr}⁾" : ""; mem.Add($"🐏 {rS}{ddr}: {HardwareService.RamUsed}/{HardwareService.RamTotal}"); } 
            if (cfg.ShowVram) mem.Add($"🎞️ {vS}: {HardwareService.VramUsed}/{HardwareService.VramTotal}"); 
            if (cfg.VrBatteryMode && RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) mem.Add($"🔋 VR: {GetVrBattery()}"); 
            
            foreach (var m in mem) p2.Add(m);
        }
        List<string> active; if (p1.Count > 0 && p2.Count > 0) { _showHardwareTick = !_showHardwareTick; active = _showHardwareTick ? p2 : p1; } else if (p2.Count > 0) { _showHardwareTick = true; active = p2; } else { _showHardwareTick = false; active = p1; }
        string outStr = ApplyThinMode(string.Join("\n", active)); Volatile.Write(ref _lastChatboxMessage, outStr); OscEngine.SendChatboxInput(outStr); _lastS = DateTime.Now; PacketsSent++; EngineState = "Chatting";
        if (!_showHardwareTick && statusAdded && cfg.AutoCycleStatus) lock (ListLock) _statusIdx = (_statusIdx + 1) % cfg.StatusList.Count;
    }

    private static string ApplyThinMode(string message)
    {
        if (!Program.Config.ThinMode || RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return message;

        if (message.Length > 138)
            message = message[..138];

        return message + "\u0003\u001f";
    }

    public static void NotifyOS(string txt) { string s = txt.Replace("'", "").Replace("\"", "").Replace("\n", " "); if (s.Length > 200) s = s[..200] + "..."; try { if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) { string ps = "Add-Type -AssemblyName System.Windows.Forms; $n = New-Object System.Windows.Forms.NotifyIcon; $n.Icon = [System.Drawing.SystemIcons]::Warning; $n.Visible = $true; $n.ShowBalloonTip(8000, 'XOSC Alert', '" + s + "', [System.Windows.Forms.ToolTipIcon]::Warning); Start-Sleep -Seconds 9; $n.Visible = $false"; Process.Start(new ProcessStartInfo("powershell", "-NoProfile -WindowStyle Hidden -Command \"" + ps + "\"") { CreateNoWindow = true, UseShellExecute = false }); } else Process.Start(new ProcessStartInfo("notify-send", "--urgency=critical \"XOSC Alert\" \"" + s + "\"") { UseShellExecute = false, CreateNoWindow = true }); } catch { } }
    private static string GetVrBattery() { try { var psi = new ProcessStartInfo("powershell", "-Command \"if (Get-Process vrserver -ErrorAction SilentlyContinue) { '85%' } else { '0%' }\"") { RedirectStandardOutput = true, CreateNoWindow = true, UseShellExecute = false }; using var p = Process.Start(psi); return p?.StandardOutput.ReadToEnd().Trim() ?? "0%"; } catch { return "0%"; } }


    private static string MakeProgressBar(double pos, double len)
    {
        if (!double.IsFinite(pos) || !double.IsFinite(len) || len <= 0)
            return "";

        pos = Math.Clamp(pos, 0, len);
        int width = Math.Clamp(Program.Config.ProgressBarWidth, 4, 32);
        int filled = Math.Clamp((int)Math.Round(pos / len * width), 0, width);
        string filledGlyph = GetProgressGlyph(Program.Config.ProgressBarFilled, "━");
        string emptyGlyph = GetProgressGlyph(Program.Config.ProgressBarEmpty, "─");
        return $"{string.Concat(Enumerable.Repeat(filledGlyph, filled))}{string.Concat(Enumerable.Repeat(emptyGlyph, width - filled))} {FormatTrackTime(pos)} - {FormatTrackTime(len)}";
    }

    private static string GetProgressGlyph(string? configured, string fallback) =>
        string.IsNullOrWhiteSpace(configured)
            ? fallback
            : StringInfo.GetNextTextElement(configured);

    private static string FormatTrackTime(double seconds)
    {
        long totalSeconds = (long)Math.Floor(Math.Clamp(seconds, 0, TimeSpan.MaxValue.TotalSeconds));
        return $"{totalSeconds / 60}:{totalSeconds % 60:D2}";
    }

    private static string MakeVisualizer() { StringBuilder sb = new StringBuilder("♪ "); for (int i = 0; i < 14; i++) sb.Append(_visBars[_visRand.Next(0, _visBars.Length)]); return sb.Append(" ♪").ToString(); }

    private static void ScrapeHardwareNames() { string log = Program.FindVrcLog(); if (log == null) return; try { using var fs = new FileStream(log, FileMode.Open, FileAccess.Read, FileShare.ReadWrite); using var sr = new StreamReader(fs); string l; int count = 0; while ((l = sr.ReadLine()) != null && count < 2000) { count++; if (l.Contains("Processor Type:")) { string r = l.Substring(l.IndexOf(':') + 1); _cpu = Regex.Replace(Regex.Replace(r, @"(?i)(AMD|Intel(?:\(R\))?|Core(?:\(TM\))?|Ryzen|\d+-Core|Processor|@.*)", " "), @"\s+", " ").Trim(); } else if (l.Contains("Graphics Device Name:")) { string r = l.Substring(l.IndexOf(':') + 1); _gpu = Regex.Replace(Regex.Replace(r, @"(?i)(NVIDIA|AMD|GeForce|Radeon|Graphics|\(RADV.*?\)|Direct3D.*)", ""), @"\s+", " ").Trim(); } } } catch { } }
    
    private static string Stylize(string t) { string n = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789", s = "ᵃᵇᶜᵈᵉᶠᵍʰᶦʲᵏˡᵐⁿᵒᵖᵠʳˢᵗᵘᵛʷˣʸᶻᵃᵇᶜᵈᵉᶠᵍʰᶦʲᵏˡᵐⁿᵒᵖᵠʳˢᵗᵘᵛʷˣʸᶻ⁰¹²³⁴⁵⁶⁷⁸⁹"; StringBuilder sb = new(); foreach (char c in t) { int i = n.IndexOf(c); sb.Append(i != -1 ? s[i] : c); } return sb.ToString(); }
}