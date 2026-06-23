using System.Diagnostics;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace XOSC.Motor.Extentions;

public static class Updater 
{ 
    public static string Status = "idle"; 
    public static bool NewVersionFound = false; 
    private static byte[]? _pData; 

    private const string StableApiUrl = "https://api.github.com/repos/hollyntt/XOSC/releases/latest";

    private static string GetSelfPath()
    {
        string? appImage = Environment.GetEnvironmentVariable("APPIMAGE");
        if (!string.IsNullOrEmpty(appImage) && File.Exists(appImage))
            return appImage;
        return Environment.ProcessPath!;
    }

    public static async Task CheckForUpdates()
    {
        Status = "checking GitHub...";
        NewVersionFound = false;

        try
        {
            using var http = new HttpClient();
            http.DefaultRequestHeaders.Add("User-Agent", "XOSC-Updater");

            var r = await http.GetStringAsync(StableApiUrl);
            using var doc = JsonDocument.Parse(r);

            string tag = doc.RootElement.GetProperty("tag_name").GetString() ?? "";
            string latestVersion = tag.TrimStart('v');

            string localVersion = Program.AppVersion.Split('+')[0];

            if (localVersion == latestVersion)
            {
                Status = "already on latest";
                return;
            }

            bool isAppImage = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("APPIMAGE"));

            var asset = doc.RootElement
                .GetProperty("assets")
                .EnumerateArray()
                .FirstOrDefault(a =>
                {
                    string name = a.GetProperty("name").GetString() ?? "";
                    if (isAppImage) return name == "XOSC-x86_64.AppImage";
                    return name == "XOSC.zip";
                });

            if (asset.ValueKind == JsonValueKind.Undefined)
            {
                Status = isAppImage ? "AppImage not found in release" : "XOSC.zip not found";
                return;
            }

            string dUrl = asset.GetProperty("browser_download_url").GetString() ?? "";

            if (isAppImage)
            {
                var bytes = await http.GetByteArrayAsync(dUrl);
                _pData = bytes;
            }
            else
            {
                var z = await http.GetByteArrayAsync(dUrl);

                using var ms = new MemoryStream(z);
                using var arch = new ZipArchive(ms);

                string platformFolder = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                    ? "win-x64/"
                    : "linux-x64/";

                var entries = arch.Entries
                    .Where(e => e.FullName.StartsWith(platformFolder, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                string[] names = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                    ? new[] { "XOSC.exe" }
                    : new[] { "XOSC" };

                ZipArchiveEntry? entry = null;

                foreach (var name in names)
                {
                    entry = entries.FirstOrDefault(e =>
                        Path.GetFileName(e.FullName).Equals(name, StringComparison.OrdinalIgnoreCase));

                    if (entry != null)
                        break;
                }

                if (entry == null)
                {
                    Status = "No executable found in update package";
                    return;
                }

                using var es = entry.Open();
                using var msw = new MemoryStream();
                await es.CopyToAsync(msw);
                _pData = msw.ToArray();
            }

            Status = $"Update found! (v{latestVersion})";
            NewVersionFound = true;
        }
        catch (Exception e)
        {
            Status = $"error: {e.Message}";
        }
    }

    public static void ApplyUpdate()
    {
        if (_pData == null) return;

        try
        {
            string self = GetSelfPath();
            Program.SaveConfig();

            string bak = self + ".bak";
            if (File.Exists(bak)) File.Delete(bak);

            File.Move(self, bak, true);
            File.WriteAllBytes(self, _pData);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                Process.Start("chmod", $"+x \"{self}\"")?.WaitForExit();

            Thread.Sleep(500);
            Process.Start(new ProcessStartInfo(self) { UseShellExecute = true });

            Environment.Exit(0);
        }
        catch (Exception e)
        {
            Status = $"apply error: {e.Message}";
        }
    }
}