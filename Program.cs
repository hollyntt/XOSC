using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using Raylib_cs;
using ImGuiNET;
using rlImGui_cs;
using XOSC.Motor;
using XOSC.Motor.Engines;
using XOSC.Motor.Extentions;
using XOSC.Motor.UI;

namespace XOSC
{
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
            unsafe { Raylib.SetTraceLogCallback(&NativeMethods.RaylibLogCallback); } 
            Raylib.SetTraceLogLevel(TraceLogLevel.None); 
            Console.SetOut(TextWriter.Null); 
            Console.SetError(TextWriter.Null); 

        #if WINDOWS_BUILD
            NativeMethods.FreeConsole();
        #endif
        #endif
            LoadConfig();

            ColAccent   = V4(Config.AccentColor);
            ColBg       = V4(Config.BgColor);
            ColSidebar  = V4(Config.SidebarColor);
            ColCard     = V4(Config.CardColor);
            ColText     = DeriveText(V4(Config.BgColor));
            ColSubText  = DeriveSubText(V4(Config.BgColor));

            _mtx = new Mutex(true, "XOSC_VRC_Unique_Runner", out bool fresh);
            if (!fresh)
                Environment.Exit(0);

            Directory.CreateDirectory(Path.GetDirectoryName(_path));

            if (Config.SavedVersion != AppVersion)
            {
                Config.SavedVersion = AppVersion;
                SaveConfig();
            }

#if WINDOWS_BUILD
            CreateStartMenuShortcut();
#endif

            MusicChatEngine.Init();
            Updater.StartAutoCheck();

            if (OperatingSystem.IsLinux())
            {
                Environment.SetEnvironmentVariable("SDL_VIDEODRIVER", "wayland");
                Environment.SetEnvironmentVariable("WAYLAND_DISPLAY", "wayland-0");
            }

            Raylib.SetConfigFlags(ConfigFlags.ResizableWindow);
            Raylib.InitWindow(1280, 720, "XOSC");
            Raylib.SetExitKey(KeyboardKey.Null);

#if WINDOWS_BUILD
            try
            {
                IntPtr hwnd;
                unsafe { hwnd = (IntPtr)Raylib.GetWindowHandle(); }
            }
            catch { }
#endif

            try
            {
                string iconPath = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "icon.png"
                );

                if (File.Exists(iconPath))
                {
                    Image img = Raylib.LoadImage(iconPath);
                    Raylib.SetWindowIcon(img);
                    Raylib.UnloadImage(img);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load icon: {ex.Message}");
            }

            rlImGui.Setup(true);

            Raylib.SetTargetFPS(60);

            ApplyTheme();

            while (!Raylib.WindowShouldClose())
            {
                Raylib.BeginDrawing();

                Raylib.ClearBackground(new Color(
                    (int)(Config.BgColor[0]*255),
                    (byte)(Config.BgColor[1]*255),
                    (byte)(Config.BgColor[2]*255),
                    255));

                rlImGui.Begin();

                DrawUI();

                rlImGui.End();

                Raylib.EndDrawing();
            }

            NetworkStats.Stop();
            HardwareService.Close();

            SaveConfig();

            Raylib.CloseWindow();
        }

#if WINDOWS_BUILD
        public static void RelaunchAsAdmin()
        {
            try
            {
                string exePath = Process.GetCurrentProcess().MainModule!.FileName;
                Program.SaveConfig();
                Process.Start(new ProcessStartInfo(exePath)
                {
                    UseShellExecute = true,
                    Verb            = "runas",
                });
                Environment.Exit(0);
            }
            catch {  }
        }

        private static void CreateStartMenuShortcut()
        {
            try
            {
                string exePath   = Process.GetCurrentProcess().MainModule!.FileName;
                string startMenu = @"C:\ProgramData\Microsoft\Windows\Start Menu\Programs";
                string lnkPath   = Path.Combine(startMenu, "XOSC.lnk");

                if (File.Exists(lnkPath))
                    return;

                var identity  = System.Security.Principal.WindowsIdentity.GetCurrent();
                var principal = new System.Security.Principal.WindowsPrincipal(identity);
                bool isAdmin  = principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);

                if (!isAdmin)
                {
                    try
                    {
                        Process.Start(new ProcessStartInfo(exePath)
                        {
                            UseShellExecute = true,
                            Verb            = "runas",
                        });
                    }
                    catch {  }
                    Environment.Exit(0);
                    return;
                }

                Type? shellType = Type.GetTypeFromProgID("WScript.Shell");
                if (shellType == null) return;
                dynamic shell   = Activator.CreateInstance(shellType)!;
                dynamic shortcut = shell.CreateShortcut(lnkPath);
                shortcut.TargetPath       = exePath;
                shortcut.WorkingDirectory = Path.GetDirectoryName(exePath);
                shortcut.Description      = "XOSC - VRChat OSC Overlay";
                shortcut.IconLocation     = $"{exePath},0";
                shortcut.Save();
            }
            catch { }
        }
#endif

        public static string FindVrcLog() { if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) { string wP = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "AppData", "LocalLow", "VRChat", "VRChat"); if (Directory.Exists(wP)) return Directory.GetFiles(wP, "output_log_*.txt").OrderByDescending(File.GetLastWriteTime).FirstOrDefault(); return null; } string h = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile); string[] s = { Path.Combine(h, ".local/share/Steam"), Path.Combine(h, ".var/app/com.valvesoftware.Steam/.local/share/Steam") }; foreach (var b in s) { if (!Directory.Exists(b)) continue; string v = Path.Combine(b, "steamapps", "libraryfolders.vdf"); List<string> l = new() { b }; if (File.Exists(v)) { var ms = Regex.Matches(File.ReadAllText(v), "\"path\"\\s+\"(.+?)\""); foreach (Match m in ms) l.Add(m.Groups[1].Value.Replace("\\\\", "/")); } foreach (var lib in l) { string p = Path.Combine(lib, "steamapps/compatdata/438100/pfx/drive_c/users/steamuser/AppData/LocalLow/VRChat/VRChat"); if (Directory.Exists(p)) return Directory.GetFiles(p, "output_log_*.txt").OrderByDescending(File.GetLastWriteTime).FirstOrDefault(); } } return null; }
        static void ApplyTheme() { var s = ImGui.GetStyle(); s.WindowRounding = Config.WindowRounding; s.ChildRounding = Config.ChildRounding; s.FrameRounding = Config.FrameRounding; s.PopupRounding = Config.FrameRounding; s.ScrollbarRounding = Config.FrameRounding; s.GrabRounding = Config.FrameRounding; s.TabRounding = Config.TabRounding; s.WindowPadding = new Vector2(12, 12); s.FramePadding = new Vector2(8, 4); s.ItemSpacing = new Vector2(8, 6); ImGui.GetIO().FontGlobalScale = Config.FontScale; ColAccent = V4(Config.AccentColor); ColBg = V4(Config.BgColor); ColSidebar = V4(Config.SidebarColor); ColCard = V4(Config.CardColor); ColText = DeriveText(ColBg); ColSubText = DeriveSubText(ColBg); var colors = ImGui.GetStyle().Colors; var a = ColAccent; var bg = ColBg; var c = ColCard; float b = 0.07f; var frame = new Vector4(Math.Min(c.X+b, 1f), Math.Min(c.Y+b, 1f), Math.Min(c.Z+b, 1f), 1f); var frameH = new Vector4(Math.Min(c.X+b+0.04f, 1f), Math.Min(c.Y+b+0.04f, 1f), Math.Min(c.Z+b+0.04f, 1f), 1f); var frameA = new Vector4(Math.Min(c.X+b+0.08f, 1f), Math.Min(c.Y+b+0.08f, 1f), Math.Min(c.Z+b+0.08f, 1f), 1f); var popup = new Vector4(Math.Min(c.X+0.04f, 1f), Math.Min(c.Y+0.04f, 1f), Math.Min(c.Z+0.06f, 1f), 1f); colors[(int)ImGuiCol.WindowBg] = bg; colors[(int)ImGuiCol.ChildBg] = c; colors[(int)ImGuiCol.PopupBg] = popup; colors[(int)ImGuiCol.MenuBarBg] = c; colors[(int)ImGuiCol.FrameBg] = frame; colors[(int)ImGuiCol.FrameBgHovered] = frameH; colors[(int)ImGuiCol.FrameBgActive] = frameA; colors[(int)ImGuiCol.Button] = new Vector4(a.X, a.Y, a.Z, 0.20f); colors[(int)ImGuiCol.ButtonHovered] = new Vector4(a.X, a.Y, a.Z, 0.40f); colors[(int)ImGuiCol.ButtonActive] = new Vector4(a.X, a.Y, a.Z, 0.65f); colors[(int)ImGuiCol.CheckMark] = a; colors[(int)ImGuiCol.SliderGrab] = a; colors[(int)ImGuiCol.SliderGrabActive] = new Vector4(a.X, a.Y, a.Z, 0.85f); colors[(int)ImGuiCol.ScrollbarBg] = new Vector4(bg.X, bg.Y, bg.Z, 1f); colors[(int)ImGuiCol.ScrollbarGrab] = new Vector4(a.X, a.Y, a.Z, 0.35f); colors[(int)ImGuiCol.ScrollbarGrabHovered] = new Vector4(a.X, a.Y, a.Z, 0.65f); colors[(int)ImGuiCol.ScrollbarGrabActive] = a; colors[(int)ImGuiCol.Header] = new Vector4(a.X, a.Y, a.Z, 0.22f); colors[(int)ImGuiCol.HeaderHovered] = new Vector4(a.X, a.Y, a.Z, 0.38f); colors[(int)ImGuiCol.HeaderActive] = new Vector4(a.X, a.Y, a.Z, 0.55f); colors[(int)ImGuiCol.TitleBg] = c; colors[(int)ImGuiCol.TitleBgActive] = popup; colors[(int)ImGuiCol.TitleBgCollapsed] = c; colors[(int)ImGuiCol.Tab] = c; colors[(int)ImGuiCol.TabHovered] = new Vector4(a.X, a.Y, a.Z, 0.35f); colors[(int)ImGuiCol.TabSelected] = new Vector4(a.X, a.Y, a.Z, 0.25f); colors[(int)ImGuiCol.TabSelectedOverline] = a; colors[(int)ImGuiCol.TabDimmed] = c; colors[(int)ImGuiCol.TabDimmedSelected] = new Vector4(a.X, a.Y, a.Z, 0.14f); colors[(int)ImGuiCol.TabDimmedSelectedOverline] = new Vector4(a.X, a.Y, a.Z, 0.40f); colors[(int)ImGuiCol.Separator] = new Vector4(a.X, a.Y, a.Z, 0.22f); colors[(int)ImGuiCol.SeparatorHovered] = new Vector4(a.X, a.Y, a.Z, 0.55f); colors[(int)ImGuiCol.SeparatorActive] = a; colors[(int)ImGuiCol.ResizeGrip] = new Vector4(a.X, a.Y, a.Z, 0.18f); colors[(int)ImGuiCol.ResizeGripHovered] = new Vector4(a.X, a.Y, a.Z, 0.45f); colors[(int)ImGuiCol.ResizeGripActive] = a; colors[(int)ImGuiCol.Border] = new Vector4(a.X, a.Y, a.Z, 0.18f); colors[(int)ImGuiCol.BorderShadow] = new Vector4(0f, 0f, 0f, 0f); bool lT = (bg.X + bg.Y + bg.Z) / 3f > 0.6f; colors[(int)ImGuiCol.Text] = lT ? new Vector4(0.10f, 0.10f, 0.13f, 1f) : new Vector4(0.92f, 0.92f, 0.95f, 1f); colors[(int)ImGuiCol.TextDisabled] = lT ? new Vector4(0.40f, 0.40f, 0.45f, 1f) : new Vector4(0.50f, 0.50f, 0.55f, 1f); colors[(int)ImGuiCol.TextLink] = a; colors[(int)ImGuiCol.NavCursor] = a; colors[(int)ImGuiCol.DragDropTarget] = a; colors[(int)ImGuiCol.TextSelectedBg] = new Vector4(a.X, a.Y, a.Z, 0.35f); colors[(int)ImGuiCol.NavWindowingHighlight] = new Vector4(a.X, a.Y, a.Z, 0.70f); colors[(int)ImGuiCol.NavWindowingDimBg] = new Vector4(0f, 0f, 0f, 0.45f); colors[(int)ImGuiCol.ModalWindowDimBg] = new Vector4(0f, 0f, 0f, 0.45f); }

        static void DrawUI() { ApplyTheme(); int w = Raylib.GetScreenWidth(), sh = Raylib.GetScreenHeight(); float sw = Config.SidebarWidth; ImGui.SetNextWindowPos(Vector2.Zero); ImGui.SetNextWindowSize(new Vector2(w, sh)); ImGui.Begin("##root", ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoScrollbar); ImGui.PushStyleColor(ImGuiCol.ChildBg, ColSidebar); ImGui.BeginChild("##sidebar", new Vector2(sw, sh), ImGuiChildFlags.None, ImGuiWindowFlags.NoScrollbar); ImGui.Dummy(new Vector2(0, 20)); ImGui.SetCursorPosX(20); ImGui.TextColored(ColAccent, "XOSC"); ImGui.SetCursorPosX(20); ImGui.TextColored(ColSubText, $"v{AppVersion}"); ImGui.Dummy(new Vector2(0, 20)); for (int i = 0; i < _navLabels.Length; i++) { bool active = _navPage == i; bool isUpdaterBtn = i == 7 && Updater.NewVersionFound && !Config.AutoApply; ImGui.PushStyleColor(ImGuiCol.Button, active ? new Vector4(ColAccent.X, ColAccent.Y, ColAccent.Z, 0.15f) : isUpdaterBtn ? new Vector4(ColAccent.X, ColAccent.Y, ColAccent.Z, 0.25f) : Vector4.Zero); ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(ColAccent.X, ColAccent.Y, ColAccent.Z, 0.08f)); ImGui.PushStyleColor(ImGuiCol.Text, (active || isUpdaterBtn) ? ColAccent : ColText); ImGui.SetCursorPosX(10); string label = isUpdaterBtn ? $"★ {_navLabels[i]}" : _navLabels[i]; if (ImGui.Button(label, new Vector2(sw - 20, 36))) _navPage = i; ImGui.PopStyleColor(3); } ImGui.EndChild(); ImGui.PopStyleColor(); ImGui.SameLine(); ImGui.PushStyleColor(ImGuiCol.ChildBg, ColBg); ImGui.BeginChild("##content", new Vector2(w - sw, sh), ImGuiChildFlags.None, ImGuiWindowFlags.NoScrollbar); ImGui.Dummy(new Vector2(0, 24)); switch (_navPage) { 
            case 0: Card("Dashboard", () => { Toggle("Enable Chatbox", ref Config.ChatboxEnabled); ImGui.Text($"Engine State: {MusicChatEngine.EngineState}"); ImGui.Text($"Packets Sent: {MusicChatEngine.PacketsSent}");
#if WINDOWS_BUILD
                if (!HardwareService.IsElevated) { ImGui.Dummy(new Vector2(0, 6)); ImGui.TextColored(new Vector4(1f, 0.7f, 0.3f, 1f), "Some sensors (CPU temp/power) need admin rights."); if (ImGui.Button("Restart as Administrator")) RelaunchAsAdmin(); }
#endif
                ImGui.Dummy(new Vector2(0, 6)); ImGui.InputText("Manual Message", ref _chatIn, 128); if (ImGui.Button("Send Manual")) { MusicChatEngine.SetManual(_chatIn); _chatIn = ""; } ImGui.Dummy(new Vector2(0, 10)); if (ImGui.InputText("OSC IP", ref Config.OscIP, 64)) SaveConfig(); if (ImGui.InputInt("OSC Port", ref Config.OscPort)) SaveConfig(); }); break; 
            case 1: Card("Statuses", () => { lock (MusicChatEngine.ListLock) { for (int i = 0; i < Config.StatusList.Count; i++) { ImGui.PushID(i); var item = Config.StatusList[i]; bool isSelected = _selectedIndices.Contains(i); if (ImGui.Checkbox("##select", ref isSelected)) { if (isSelected) _selectedIndices.Add(i); else _selectedIndices.Remove(i); } ImGui.SameLine(); if (ImGui.Button(item.IsFavorited ? "[*]" : "[ ]", new Vector2(32, 24))) { item.IsFavorited = !item.IsFavorited; Config.StatusList = Config.StatusList.OrderByDescending(s => s.IsFavorited).ToList(); SaveConfig(); ImGui.PopID(); break; } ImGui.SameLine(); if (ImGui.Button("Up", new Vector2(32, 24)) && i > 0) { (Config.StatusList[i], Config.StatusList[i - 1]) = (Config.StatusList[i - 1], Config.StatusList[i]); SaveConfig(); } ImGui.SameLine(); if (ImGui.Button("Dn", new Vector2(32, 24)) && i < Config.StatusList.Count - 1) { (Config.StatusList[i], Config.StatusList[i + 1]) = (Config.StatusList[i + 1], Config.StatusList[i]); SaveConfig(); } ImGui.SameLine(); string statusText = item.Text; if (ImGui.InputText("##s", ref statusText, 100)) { item.Text = statusText; SaveConfig(); } ImGui.PopID(); } } ImGui.Dummy(new Vector2(0, 10)); if (ImGui.Button("+ Add New Status")) Config.StatusList.Add(new StatusItem()); ImGui.SameLine(); if (_selectedIndices.Any() && ImGui.Button("Remove Selected")) { var sorted = _selectedIndices.ToList(); sorted.Sort(); for (int i = sorted.Count - 1; i >= 0; i--) Config.StatusList.RemoveAt(sorted[i]); _selectedIndices.Clear(); SaveConfig(); } }); break; 
            case 2: Card("Chatbox", () => { Toggle("Status Text", ref Config.StatusTextMode); Toggle("Pronouns##Toggle", ref Config.PronounsMode); Toggle("Song Mode", ref Config.SongMode); if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) Toggle("Song Progress Bar", ref Config.SongProgressMode); Toggle("Audio Visualizer", ref Config.AudioVisualizerMode); Toggle("Time", ref Config.TimeMode); Toggle("Military Time", ref Config.MilitaryTime); Toggle("Distro", ref Config.DistroMode); Toggle("Thin Mode", ref Config.ThinMode); Toggle("Auto-Cycle", ref Config.AutoCycleStatus); Toggle("Stylize All Text", ref Config.StylizeTextMode); DrawCombo("Pronouns", _pronounsList, ref Config.Pronouns, ref Config.CustomPronouns); DrawCombo("Country", _countriesList, ref Config.Country, ref Config.CustomCountry); string[] states = _statesMap.ContainsKey(Config.Country) ? _statesMap[Config.Country] : new[] { "Custom..." }; DrawCombo("State", states, ref Config.State, ref Config.CustomState); string[] cities = _citiesMap.ContainsKey(Config.State) ? _citiesMap[Config.State] : new[] { "Custom..." }; DrawCombo("City", cities, ref Config.City, ref Config.CustomCity); ImGui.SliderInt("Interval##slider", ref Config.Interval, 1, 60); }); Card("Weather", () => { Toggle("Enable Weather", ref Config.WeatherMode); Toggle("Show Temperature", ref Config.WeatherTempMode); int tempIdx = Array.IndexOf(_tempUnits, Config.WeatherTempUnit); if (tempIdx < 0) tempIdx = 0; if (ImGui.Combo("Temp Unit##tempunit", ref tempIdx, _tempUnits, _tempUnits.Length)) { Config.WeatherTempUnit = _tempUnits[tempIdx]; SaveConfig(); } }); break; 
            case 3: Card("Hardware", () => { Toggle("Show Stats", ref Config.PcMode); Toggle("Show RAM", ref Config.ShowRam); if (Config.ShowRam) { ImGui.Indent(); Toggle("Show DDR Version", ref Config.RamDdrVersionOn); ImGui.Unindent(); } Toggle("Show VRAM", ref Config.ShowVram); if(RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) Toggle("VR Headset Battery", ref Config.VrBatteryMode); Toggle("Stylized Names", ref Config.HwNameMode); ImGui.Separator(); ImGui.Text("Sensors"); Toggle("CPU Temp", ref Config.CpuTempOn); Toggle("CPU Power (Wattage)", ref Config.CpuPowerOn); Toggle("GPU Temp", ref Config.GpuTempOn); Toggle("GPU Hotspot Temp", ref Config.GpuHotspotOn); Toggle("GPU Power (Wattage)", ref Config.GpuPowerOn); ImGui.Separator(); ImGui.Text("Custom Names"); Toggle("Custom CPU Name", ref Config.CustomCpuNameOn); if (Config.CustomCpuNameOn) ImGui.InputText("##c_cpu", ref Config.CustomCpuName, 32); Toggle("Custom GPU Name", ref Config.CustomGpuNameOn); if (Config.CustomGpuNameOn) ImGui.InputText("##c_gpu", ref Config.CustomGpuName, 32); }); break; 
            case 4: Card("Network", () => { Toggle("Internet Ping", ref Config.NetMode); ImGui.Dummy(new Vector2(0, 8)); ImGui.Text($"Avg: {NetworkStats.AvgPing}ms"); ImGui.Text($"Loss: {NetworkStats.PacketLoss}%"); }); break; 
            case 5: Card("Appearance", () => { var presets = ThemePresets.All; for (int i = 0; i < presets.Length; i++) { if (i > 0 && i % 4 != 0) ImGui.SameLine(0, 6); var p = presets[i]; var ac = new Vector4(p.Accent[0], p.Accent[1], p.Accent[2], 1f); ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(p.Bg[0], p.Bg[1], p.Bg[2], 1f)); ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(p.Card[0], p.Card[1], p.Card[2], 1f)); ImGui.PushStyleColor(ImGuiCol.Text, ac); if (ImGui.Button(p.Name, new Vector2(140, 40))) { Config.AccentColor = (float[])p.Accent.Clone(); Config.BgColor = (float[])p.Bg.Clone(); Config.SidebarColor = (float[])p.Sidebar.Clone(); Config.CardColor = (float[])p.Card.Clone(); ColAccent = V4(Config.AccentColor); ColBg = V4(Config.BgColor); ColSidebar = V4(Config.SidebarColor); ColCard = V4(Config.CardColor); ApplyTheme(); SaveConfig(); } ImGui.PopStyleColor(3); } }); Card("Colors", () => { ColorPicker3("Accent Color", Config.AccentColor, ref ColAccent, c => Config.AccentColor = c); ColorPicker3("Background", Config.BgColor, ref ColBg, c => Config.BgColor = c); ColorPicker3("Sidebar", Config.SidebarColor, ref ColSidebar, c => Config.SidebarColor = c); ColorPicker3("Card", Config.CardColor, ref ColCard, c => Config.CardColor = c); }); Card("Layout & Shape", () => { bool ch = false; float swW = Config.SidebarWidth; if (ImGui.SliderFloat("Sidebar Width", ref swW, 120, 280)) { Config.SidebarWidth = swW; ch = true; } float wr = Config.WindowRounding; if (ImGui.SliderFloat("Window Rounding", ref wr, 0, 14)) { Config.WindowRounding = wr; ch = true; } float cr = Config.ChildRounding; if (ImGui.SliderFloat("Card Rounding", ref cr, 0, 14)) { Config.ChildRounding = cr; ch = true; } float fr = Config.FrameRounding; if (ImGui.SliderFloat("Frame Rounding", ref fr, 0, 14)) { Config.FrameRounding = fr; ch = true; } float tr = Config.TabRounding; if (ImGui.SliderFloat("Tab Rounding", ref tr, 0, 14)) { Config.TabRounding = tr; ch = true; } float fs = Config.FontScale; if (ImGui.SliderFloat("Font Scale", ref fs, 0.7f, 1.8f)) { Config.FontScale = fs; ImGui.GetIO().FontGlobalScale = fs; ch = true; } if (ch) { ApplyTheme(); SaveConfig(); } }); break; 
            case 6: Card("Misc", () => { Toggle("AFK Detection", ref Config.AfkDetectionMode); if (Config.AfkDetectionMode) { ImGui.Indent(); if (ImGui.SliderInt("AFK Timeout (seconds)", ref Config.AfkTimeout, 30, 3600)) SaveConfig(); ImGui.TextColored(ColSubText, AfkEngine.IsAfk ? $"AFK for {AfkEngine.AfkDuration}" : "Not AFK"); ImGui.Unindent(); } }); break; 
            case 7: Card("Updater", () => { Toggle("Auto Check (check on startup + every 5min)", ref Config.AutoCheck); Toggle("Auto Apply (install automatically when found)", ref Config.AutoApply); ImGui.Dummy(new Vector2(0, 4)); if (ImGui.Button("Check for Update")) Task.Run(async () => await Updater.CheckForUpdates()); if (Updater.NewVersionFound) { ImGui.SameLine(); if (ImGui.Button("Apply Update")) Updater.ApplyUpdate(); } ImGui.Text($"Status: {Updater.Status}"); ImGui.Text($"Version: {AppVersion}"); }); break; 
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
            } 
            catch { } 
        }

        static void LoadConfig() 
        { 
            if (!File.Exists(_path)) return; 
    
            try 
            { 
                var rawJson = File.ReadAllText(_path); 

                var options = new JsonSerializerOptions { 
                    IncludeFields = true, 
                    Converters = { new StatusItemConverter() } 
                };

                var jsonNode = System.Text.Json.Nodes.JsonNode.Parse(rawJson); 

                if (jsonNode["StatusList"] is System.Text.Json.Nodes.JsonArray sL && sL.All(n => n is System.Text.Json.Nodes.JsonValue)) 
                { 
                    var sLL = JsonSerializer.Deserialize<List<string>>(sL.ToJsonString()); 

                    var tC = JsonSerializer.Deserialize<AppConfig>(rawJson, options); 
            
                    if (tC != null && sLL != null) 
                    { 
                        tC.StatusList = sLL.Select(s => new StatusItem { Text = s }).ToList(); 
                        Config = tC; 
                        SaveConfig(); 
                        return; 
                    } 
                } 

                var loaded = JsonSerializer.Deserialize<AppConfig>(rawJson, options); 
                if (loaded != null) Config = loaded; 
        
            } 
            catch { } 
        }
    }
}