using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using Concentus.Structs;
using Concentus.Enums;
using NAudio.Wave;
using System.Numerics;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace SimpleVoiceChat;

public class ClientVoiceSender
{
    private readonly UdpClient udpClient;
    private readonly IPEndPoint serverEndpoint;

    private readonly OpusEncoder encoder;
    private readonly WaveInEvent waveIn;
    private readonly string playerId;
    private readonly Func<Vector3> getPosition;
    private readonly Func<Vector3> getDirection;

    private const int SampleRate = 48000;
    private const int Channels = 1;
    private const int FrameSize = 960; // 20ms frame at 48kHz

    private readonly byte[] encodeBuffer = new byte[4000];

    public ClientVoiceSender(string serverIp, int serverPort, string playerId, ICoreClientAPI api)
    {
        this.playerId = playerId;

        // Przechowaj api w zmiennej lokalnej, żeby lambda je widziała
        getPosition = () => new Vector3(
            (float)api.World.Player.Entity.Pos.X,
            (float)api.World.Player.Entity.Pos.Y,
            (float)api.World.Player.Entity.Pos.Z
        );

        getDirection = () => new Vector3(
            (float)api.World.Player.Entity.SidedPos.Motion.X,
            (float)api.World.Player.Entity.SidedPos.Motion.Y,
            (float)api.World.Player.Entity.SidedPos.Motion.Z
        );

        serverEndpoint = new IPEndPoint(IPAddress.Parse(serverIp), serverPort);
        udpClient = new UdpClient();

        encoder = OpusEncoder.Create(SampleRate, Channels, OpusApplication.OPUS_APPLICATION_VOIP);
        encoder.Bitrate = 24000;

        waveIn = new WaveInEvent
        {
            WaveFormat = new WaveFormat(SampleRate, Channels),
            BufferMilliseconds = 20
        };

        waveIn.DataAvailable += OnDataAvailable;
    }

    public void Start()
    {
        waveIn.StartRecording();
        Console.WriteLine("[ClientVoiceSender] Started recording...");
    }

    public void Stop()
    {
        waveIn.StopRecording();
        udpClient?.Close();
        Console.WriteLine("[ClientVoiceSender] Stopped recording and closed UDP.");
    }

    private void OnDataAvailable(object sender, WaveInEventArgs e)
    {
        // Konwersja bajtów na 16-bit samples
        short[] samples = new short[e.BytesRecorded / 2];
        Buffer.BlockCopy(e.Buffer, 0, samples, 0, e.BytesRecorded);

        int encodedLength = encoder.Encode(samples, 0, FrameSize, encodeBuffer, 0, encodeBuffer.Length);
        byte[] encodedAudio = encodeBuffer.Take(encodedLength).ToArray();

        var packet = new AudioPacket
        {
            PlayerId = playerId,
            AudioData = encodedAudio,
            Position = getPosition(),
            Direction = getDirection()
        };

        byte[] data = packet.Serialize();
        udpClient.Send(data, data.Length, serverEndpoint);
    }
}
