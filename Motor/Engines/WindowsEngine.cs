#if WINDOWS_BUILD
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace XOSC.Motor.Engines;

public sealed class WindowsEngine : IPlatformEngine
{
    private WindowsMediaController.MediaManager? _mediaManager;
    private readonly object _musicLock = new();
    private MusicData _musicData = new("Chilling", 0, 0);
    private string? _operatingSystemName;

    public void Start()
    {
        _mediaManager = new WindowsMediaController.MediaManager();
        _mediaManager.OnAnyMediaPropertyChanged += (_, properties) =>
        {
            string title = string.IsNullOrWhiteSpace(properties.Artist)
                ? properties.Title
                : $"{properties.Artist} - {properties.Title}";
            title = Regex.Replace(title, @"[\r\n\|]", "").Trim();
            lock (_musicLock)
                _musicData = _musicData with { Title = string.IsNullOrWhiteSpace(title) ? "Chilling" : title };
        };
        _mediaManager.OnAnyTimelinePropertyChanged += (_, timeline) =>
        {
            lock (_musicLock)
                _musicData = _musicData with
                {
                    Position = timeline.Position.TotalSeconds,
                    Length = timeline.EndTime.TotalSeconds
                };
        };
        _mediaManager.Start();

        _ = Task.Run(async () =>
        {
            await Task.Delay(2000);
            _mediaManager?.ForceUpdate();
        });
    }

    public MusicData GetMusicData()
    {
        lock (_musicLock)
            return _musicData;
    }

    public string GetOperatingSystemName()
    {
        if (_operatingSystemName != null)
            return _operatingSystemName;

        try
        {
            var startInfo = new ProcessStartInfo(
                "powershell",
                "-NoProfile -Command \"(Get-CimInstance Win32_OperatingSystem).Caption\"")
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var process = Process.Start(startInfo);
            string? caption = process?.StandardOutput.ReadToEnd().Trim();
            if (!string.IsNullOrWhiteSpace(caption))
            {
                _operatingSystemName = caption;
                return caption;
            }
        }
        catch (System.ComponentModel.Win32Exception)
        {
        }
        catch (InvalidOperationException)
        {
        }

        try
        {
            using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                @"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
            _operatingSystemName = key?.GetValue("ProductName")?.ToString() ?? "Windows";
        }
        catch (System.Security.SecurityException)
        {
            _operatingSystemName = "Windows";
        }
        catch (UnauthorizedAccessException)
        {
            _operatingSystemName = "Windows";
        }
        catch (IOException)
        {
            _operatingSystemName = "Windows";
        }

        return _operatingSystemName;
    }
}
#endif
