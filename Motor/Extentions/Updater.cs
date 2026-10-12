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

    private static Timer? _autoCheckTimer;

    public static void StartAutoCheck()
    {
        Task.Run(async () =>
        {
            await CheckForUpdates();
            if (NewVersionFound && Program.Config.AutoApply)
                ApplyUpdate();
        });

        _autoCheckTimer = new Timer(async _ =>
        {
            if (!Program.Config.AutoCheck) return;
            await CheckForUpdates();
            if (NewVersionFound && Program.Config.AutoApply)
                ApplyUpdate();
        }, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
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

            var assets = doc.RootElement
                .GetProperty("assets")
                .EnumerateArray()
                .ToArray();
            var asset = assets.FirstOrDefault(a =>
            {
                string name = a.GetProperty("name").GetString() ?? "";
                return name == (isAppImage ? "XOSC-x86_64.AppImage" : "XOSC.zip");
            });

            if (asset.ValueKind == JsonValueKind.Undefined)
            {
                if (!isAppImage)
                {
                    Status = "XOSC.zip not found in release";
                    return;
                }

                var zipAsset = assets.FirstOrDefault(a =>
                    a.GetProperty("name").GetString() == "XOSC.zip");
                if (zipAsset.ValueKind == JsonValueKind.Undefined)
                {
                    Status = "AppImage and XOSC.zip not found in release";
                    return;
                }

                string zipUrl = zipAsset.GetProperty("browser_download_url").GetString() ?? "";
                byte[] archiveBytes = await http.GetByteArrayAsync(zipUrl);
                using var archiveStream = new MemoryStream(archiveBytes);
                using var archive = new ZipArchive(archiveStream);
                var appImageEntry = archive.GetEntry("linux-x64/XOSC-x86_64.AppImage");
                if (appImageEntry == null)
                {
                    Status = "AppImage not found in release or XOSC.zip";
                    return;
                }

                using var appImageStream = appImageEntry.Open();
                using var appImageData = new MemoryStream();
                await appImageStream.CopyToAsync(appImageData);
                _pData = appImageData.ToArray();
            }
            else
            {
                string downloadUrl = asset.GetProperty("browser_download_url").GetString() ?? "";

                if (isAppImage)
                {
                    _pData = await http.GetByteArrayAsync(downloadUrl);
                }
                else
                {
                    var zipBytes = await http.GetByteArrayAsync(downloadUrl);
                    using var archiveStream = new MemoryStream(zipBytes);
                    using var arch = new ZipArchive(archiveStream);

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

                    using var entryStream = entry.Open();
                    using var executableData = new MemoryStream();
                    await entryStream.CopyToAsync(executableData);
                    _pData = executableData.ToArray();
                }
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