using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace XOSC.Motor.Engines;

public static class AfkEngine
{
    public static bool IsAfk { get; private set; }
    public static string AfkDuration { get; private set; } = "";

    private static DateTime _afkSince = DateTime.MinValue;

#if WINDOWS_BUILD
    [DllImport("user32.dll")]
    private static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

    [StructLayout(LayoutKind.Sequential)]
    private struct LASTINPUTINFO
    {
        public uint cbSize;
        public uint dwTime;
    }

    private static uint GetIdleSeconds()
    {
        var info = new LASTINPUTINFO { cbSize = (uint)Marshal.SizeOf(typeof(LASTINPUTINFO)) };
        GetLastInputInfo(ref info);
        return ((uint)Environment.TickCount - info.dwTime) / 1000;
    }
#endif

    public static void Update()
    {
        int timeout = Program.Config.AfkTimeout;

#if WINDOWS_BUILD
        uint idle = GetIdleSeconds();
        bool nowAfk = idle >= timeout;

        if (nowAfk && !IsAfk)
        {
            IsAfk = true;
            _afkSince = DateTime.Now - TimeSpan.FromSeconds(idle);
        }
        else if (!nowAfk && IsAfk)
        {
            IsAfk = false;
            AfkDuration = "";
            _afkSince = DateTime.MinValue;
        }
#else
        CheckAfkFromVrcLog();
#endif

        if (IsAfk && _afkSince != DateTime.MinValue)
            AfkDuration = FormatDuration(DateTime.Now - _afkSince);
    }

    private static void CheckAfkFromVrcLog()
    {
        string? log = Program.FindVrcLog();
        if (log == null) return;
        try
        {
            using var fs = new FileStream(log, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var sr = new StreamReader(fs);
            string? l, last = null;
            while ((l = sr.ReadLine()) != null) last = l;
            if (last == null) return;

            if (last.Contains("OnPlayerResting") && !IsAfk)
            {
                IsAfk = true;
                _afkSince = DateTime.Now;
            }
            else if (last.Contains("OnPlayerActive") && IsAfk)
            {
                IsAfk = false;
                AfkDuration = "";
                _afkSince = DateTime.MinValue;
            }
        }
        catch { }
    }

    private static string FormatDuration(TimeSpan t)
    {
        if (t.TotalHours >= 1)
            return $"{(int)t.TotalHours}ʰ {t.Minutes}ᵐ {t.Seconds}ˢ";
        if (t.TotalMinutes >= 1)
            return $"{t.Minutes}ᵐ {t.Seconds}ˢ";
        return $"{t.Seconds}ˢ";
    }
}