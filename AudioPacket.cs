using System;
using System.IO;
using System.Numerics;

namespace SimpleVoiceChat;

public class AudioPacket
{
    public string PlayerId { get; set; }
    public byte[] AudioData { get; set; }
    public Vector3 Position { get; set; }
    public Vector3 Direction { get; set; }

    /// <summary>
    /// Serializuje AudioPacket do byte[] dla przesyłania przez UDP.
    /// </summary>
    public byte[] Serialize()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        writer.Write(PlayerId);
        writer.Write(AudioData.Length);
        writer.Write(AudioData);

        writer.Write(Position.X);
        writer.Write(Position.Y);
        writer.Write(Position.Z);

        writer.Write(Direction.X);
        writer.Write(Direction.Y);
        writer.Write(Direction.Z);

        return ms.ToArray();
    }

    /// <summary>
    /// Deserializuje byte[] do AudioPacket.
    /// </summary>
    public static AudioPacket Deserialize(byte[] data)
    {
        using var ms = new MemoryStream(data);
        using var reader = new BinaryReader(ms);

        var playerId = reader.ReadString();

        int audioLength = reader.ReadInt32();
        byte[] audioData = reader.ReadBytes(audioLength);

        float posX = reader.ReadSingle();
        float posY = reader.ReadSingle();
        float posZ = reader.ReadSingle();

        float dirX = reader.ReadSingle();
        float dirY = reader.ReadSingle();
        float dirZ = reader.ReadSingle();

        return new AudioPacket
        {
            PlayerId = playerId,
            AudioData = audioData,
            Position = new Vector3(posX, posY, posZ),
            Direction = new Vector3(dirX, dirY, dirZ)
        };
    }
}