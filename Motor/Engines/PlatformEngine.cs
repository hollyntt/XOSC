namespace XOSC.Motor.Engines;

public readonly record struct MusicData(string Title, double Position, double Length);

public interface IPlatformEngine
{
    void Start();
    MusicData GetMusicData();
    string GetOperatingSystemName();
}

public static class PlatformEngine
{
    public static IPlatformEngine Create()
    {
#if WINDOWS_BUILD
        return new WindowsEngine();
#else
        return new LinuxEngine();
#endif
    }
}
