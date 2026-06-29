using XOSC.Motor.UI;

namespace XOSC.Motor;

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
        public int AfkTimeout = 300;
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
        public bool AutoUpdate = true;
        public bool AutoApply = false;
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