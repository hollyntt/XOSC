using System.Net.Sockets;
using System.Text;

namespace XOSC.Motor.Engines;

public static class OscEngine
{
    private static readonly object ClientLock = new();
    private static UdpClient _client = new();

    public static void Initialize()
    {
        lock (ClientLock)
        {
            _client.Dispose();
            _client = new UdpClient();
        }
    }

    public static void SendChatboxInput(string message, bool notificationSound = false)
    {
        ArgumentNullException.ThrowIfNull(message);
        SendPacket(CreateChatboxPacket(message, notificationSound), "chatbox message");
    }

    public static void SendChatboxTyping(bool isTyping)
    {
        using var packet = new MemoryStream();
        WriteOscString(packet, "/chatbox/typing");
        WriteOscString(packet, isTyping ? ",T" : ",F");
        SendPacket(packet.ToArray(), "chatbox typing state");
    }

    private static void SendPacket(byte[] packet, string description)
    {
        try
        {
            string ip = Program.Config.OscIP.Trim();
            if (string.IsNullOrEmpty(ip))
                return;

            lock (ClientLock)
                _client.Send(packet, packet.Length, ip, Program.Config.OscPort);
        }
        catch (Exception ex) when (ex is SocketException or ObjectDisposedException or ArgumentException)
        {
            Console.Error.WriteLine($"Failed to send OSC {description}: {ex.Message}");
            lock (ClientLock)
            {
                _client.Dispose();
                _client = new UdpClient();
            }
        }
    }

    private static byte[] CreateChatboxPacket(string message, bool notificationSound)
    {
        using var packet = new MemoryStream();
        WriteOscString(packet, "/chatbox/input");
        WriteOscString(packet, notificationSound ? ",sTT" : ",sTF");
        WriteOscString(packet, message);
        return packet.ToArray();
    }

    private static void WriteOscString(Stream stream, string value)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(value);
        stream.Write(bytes);
        stream.WriteByte(0);
        while (stream.Position % 4 != 0)
            stream.WriteByte(0);
    }
}
