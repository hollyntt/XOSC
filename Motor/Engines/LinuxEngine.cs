using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;

namespace XOSC.Motor.Engines;

public sealed class LinuxEngine : IPlatformEngine
{
    private const string DefaultTitle = "Chilling";
    private string? _operatingSystemName;

    public void Start()
    {
    }

    public MusicData GetMusicData()
    {
        string? metadata = RunCommand(
            "playerctl",
            "--all-players",
            "metadata",
            "--format",
            "{{playerName}}\u001f{{status}}\u001f{{artist}}\u001f{{title}}\u001f{{mpris:length}}");
        if (!string.IsNullOrWhiteSpace(metadata))
        {
            var tracks = new List<(string Player, string Status, string Artist, string Title, double Length)>();
            foreach (string line in metadata.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                string[] fields = line.Split('\u001f');
                if (fields.Length < 5)
                    continue;

                string title = CleanTitle(fields[3]);
                if (string.IsNullOrWhiteSpace(title))
                    continue;

                tracks.Add((
                    fields[0],
                    fields[1],
                    CleanTitle(fields[2]),
                    title,
                    ParseNumber(fields[4]) / 1_000_000d));
            }

            foreach (var track in tracks
                         .OrderByDescending(track => track.Status.Equals("Playing", StringComparison.OrdinalIgnoreCase))
                         .ThenByDescending(track => track.Length >= 30)
                         .ThenByDescending(track => !track.Title.Equals(track.Player, StringComparison.OrdinalIgnoreCase))
                         .ThenByDescending(track => track.Length))
            {
                double position = ParseNumber(RunCommand("playerctl", "--player", track.Player, "position"));
                string title = string.IsNullOrWhiteSpace(track.Artist)
                    ? track.Title
                    : $"{track.Artist} - {track.Title}";
                return new MusicData(title, position, track.Length);
            }
        }

        string? focusedWindow = RunCommand("xdotool", "getwindowfocus", "getwindowname");
        if (!string.IsNullOrWhiteSpace(focusedWindow))
            return new MusicData(CleanTitle(focusedWindow), 0, 0);

        return new MusicData(DefaultTitle, 0, 0);
    }

    public string GetOperatingSystemName()
    {
        if (_operatingSystemName != null)
            return _operatingSystemName;

        try
        {
            foreach (string line in File.ReadLines("/etc/os-release"))
            {
                if (line.StartsWith("PRETTY_NAME=", StringComparison.Ordinal))
                {
                    string value = line["PRETTY_NAME=".Length..].Trim();
                    if (value.Length >= 2 && value[0] == value[^1] && value[0] is '"' or '\'')
                        value = value[1..^1];
                    _operatingSystemName = value
                        .Replace("\\\"", "\"", StringComparison.Ordinal)
                        .Replace("\\\\", "\\", StringComparison.Ordinal);
                    return _operatingSystemName;
                }
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }

        _operatingSystemName = "Linux";
        return _operatingSystemName;
    }

    private static string? RunCommand(string fileName, params string[] arguments)
    {
        try
        {
            var startInfo = new ProcessStartInfo(fileName)
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            foreach (string argument in arguments)
                startInfo.ArgumentList.Add(argument);

            using var process = Process.Start(startInfo);
            if (process == null)
                return null;

            if (!process.WaitForExit(1500))
            {
                process.Kill(entireProcessTree: true);
                return null;
            }

            return process.ExitCode == 0 ? process.StandardOutput.ReadToEnd().Trim() : null;
        }
        catch (System.ComponentModel.Win32Exception)
        {
            return null;
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    private static string CleanTitle(string title) =>
        Regex.Replace(title, @"[\r\n\|]", "").Trim();

    private static double ParseNumber(string? value) =>
        double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsed)
            && double.IsFinite(parsed)
                ? parsed
                : 0;
}
