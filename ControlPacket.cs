using System;
using System.IO;

namespace SimpleVoiceChat;

public class ControlPacket
{
    public string Type { get; set; }        // "handshake" lub "handshake-reply"
    public string ServerIp { get; set; }    // Odpowiedź serwera
    public int ServerPort { get; set; }     // Odpowiedź serwera

    public byte[] Serialize()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        writer.Write(Type ?? "");
        writer.Write(ServerIp ?? "");
        writer.Write(ServerPort);

        return ms.ToArray();
    }

    public static ControlPacket Deserialize(byte[] data)
    {
        using var ms = new MemoryStream(data);
        using var reader = new BinaryReader(ms);

        var type = reader.ReadString();
        var ip = reader.ReadString();
        var port = reader.ReadInt32();

        return new ControlPacket
        {
            Type = type,
            ServerIp = ip,
            ServerPort = port
        };
    }
}