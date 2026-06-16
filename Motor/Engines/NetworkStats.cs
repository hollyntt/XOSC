using System.Net.NetworkInformation;

namespace XOSC.Motor.Engines;

public static class NetworkStats
    {
        private static Timer _timer;
        private static NetworkInterface _activeInterface;
        private static long _prevBytesRecv, _prevBytesSent;
        private static double _totalRecvMB, _totalSentMB;
        private static readonly object _lock = new();

        public static double DownloadSpeedMbps { get; private set; }
        public static double UploadSpeedMbps { get; private set; }
        public static double TotalDownloadedMB => _totalRecvMB;
        public static double TotalUploadedMB   => _totalSentMB;
        public static double NetworkUtilization { get; private set; }
        public static string InterfaceName { get; private set; } = "Unknown";
        public static bool IsActive { get; private set; }

        private static readonly Queue<double> _latencies = new();
        private static readonly string[] _targets = { "1.1.1.1", "8.8.8.8", "vrcoscv4.vrchat.cloud" };
        private static int _pingTargetIdx;
        private static double _lastJitter;
        public static double AvgPing { get; private set; }
        public static double PacketLoss { get; private set; }
        public static double Jitter { get; private set; }
        public static string PingStatus { get; private set; } = "Idle";

        public static async Task UpdateAsync()
        {
            var target = _targets[_pingTargetIdx];
            _pingTargetIdx = (_pingTargetIdx + 1) % _targets.Length;
            try
            {
                using var ping = new Ping();
                var reply = await ping.SendPingAsync(target, 1500);
                if (reply.Status == IPStatus.Success)
                {
                    double rtt = reply.RoundtripTime;
                    lock (_latencies)
                    {
                        _latencies.Enqueue(rtt);
                        if (_latencies.Count > 8) _latencies.Dequeue();
                        if (_latencies.Count >= 2)
                        {
                            var list = _latencies.ToList();
                            AvgPing = Math.Round(list.Average(), 1);
                            PacketLoss = 0;
                            double diff = Math.Abs(rtt - list[^2]);
                            _lastJitter = (_lastJitter * 0.7) + (diff * 0.3);
                            Jitter = Math.Round(_lastJitter, 1);
                        }
                    }
                    PingStatus = "Stable";
                }
                else { PacketLoss = Math.Min(100, PacketLoss + 12); PingStatus = "Timeout"; }
            }
            catch { PacketLoss = Math.Min(100, PacketLoss + 15); PingStatus = "Error"; }
        }

        public static void Start()
        {
            var interfaces = NetworkInterface.GetAllNetworkInterfaces()
                .Where(ni => ni.OperationalStatus == OperationalStatus.Up &&
                             ni.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                             ni.NetworkInterfaceType != NetworkInterfaceType.Tunnel)
                .ToList();
            if (interfaces.Count > 1) _activeInterface = interfaces.OrderByDescending(ni => ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet ? 3 : ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 ? 2 : 1).FirstOrDefault();
            else _activeInterface = interfaces.FirstOrDefault();
            if (_activeInterface == null) return;
            InterfaceName = _activeInterface.Name;
            var stats = _activeInterface.GetIPv4Statistics();
            _prevBytesRecv = stats.BytesReceived;
            _prevBytesSent = stats.BytesSent;
            IsActive = true;
            _timer = new Timer(_ =>
            {
                if (_activeInterface == null) return;
                var s = _activeInterface.GetIPv4Statistics();
                double dlMbps = (s.BytesReceived - _prevBytesRecv) * 8 / 1e6;
                double ulMbps = (s.BytesSent - _prevBytesSent) * 8 / 1e6;
                lock (_lock) { DownloadSpeedMbps = Math.Max(0, dlMbps); UploadSpeedMbps = Math.Max(0, ulMbps); _totalRecvMB += (s.BytesReceived - _prevBytesRecv) / 1e6; _totalSentMB += (s.BytesSent - _prevBytesSent) / 1e6; double maxMbps = _activeInterface.Speed / 1e6; NetworkUtilization = maxMbps > 0 ? Math.Min(100, (dlMbps / maxMbps) * 100) : 0; }
                _prevBytesRecv = s.BytesReceived; _prevBytesSent = s.BytesSent;
            }, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
        }

        public static void Stop() { _timer?.Dispose(); _timer = null; IsActive = false; }
        public static string FormatSpeed(double mbps) => mbps < 1 ? $"{mbps * 1000:F0} Kbps" : (mbps >= 1000 ? $"{mbps / 1000:F1} Gbps" : $"{mbps:F1} Mbps");
    }