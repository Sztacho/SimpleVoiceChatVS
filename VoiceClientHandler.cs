using System;
using System.Net;
using System.Numerics;

namespace SimpleVoiceChat;

public class VoiceClientHandler
{
    public string PlayerId { get; set; }
    public IPEndPoint EndPoint { get; set; }
    public Vector3 Position { get; set; }
    public Vector3 Direction { get; set; }

    public float DistanceTo(VoiceClientHandler other)
    {
        return Vector3.Distance(Position, other.Position);
    }

    public void SendAudio(byte[] audioData, float volume, Vector3 direction)
    {
        // Implementacja wysyłania audio do klienta
    }
}