using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using System.Threading;
using Concentus.Structs;
using Concentus.Enums;
using NAudio.Wave;
using Vintagestory.API.Client;

namespace SimpleVoiceChat;

public class ClientVoiceEndpoint
{
    private readonly UdpClient udpClient;
    private IPEndPoint serverEndpoint;  // <-- Usunięty readonly, bo ustawiamy po handshaku!
    private readonly OpusEncoder encoder;
    private readonly OpusDecoder decoder;
    private readonly BufferedWaveProvider bufferedWaveProvider;
    private readonly WaveInEvent waveIn;
    private readonly WaveOutEvent waveOut;
    private readonly string playerId;
    private readonly ICoreClientAPI api;
    private volatile bool running = true;
    private readonly Config config;

    private const int SampleRate = 48000;
    private const int Channels = 1;
    private const int FrameSize = 960;

    private readonly byte[] encodeBuffer = new byte[4000];

    public ClientVoiceEndpoint(string playerId, Config config, ICoreClientAPI api)
    {
        this.playerId = playerId;
        this.api = api;
        this.config = config;

        udpClient = new UdpClient();  // Bindowanie na lokalny port dla klienta

        encoder = OpusEncoder.Create(SampleRate, Channels, OpusApplication.OPUS_APPLICATION_VOIP);
        encoder.Bitrate = 24000;

        decoder = OpusDecoder.Create(SampleRate, Channels);

        bufferedWaveProvider = new BufferedWaveProvider(new WaveFormat(SampleRate, Channels))
        {
            BufferDuration = TimeSpan.FromSeconds(2),
            DiscardOnBufferOverflow = true
        };

        waveOut = new WaveOutEvent();
        waveOut.Init(bufferedWaveProvider);
        waveOut.Play();

        waveIn = new WaveInEvent
        {
            WaveFormat = new WaveFormat(SampleRate, Channels),
            BufferMilliseconds = 20
        };

        waveIn.DataAvailable += OnDataAvailable;
    }

    public void Start()
    {
        DoHandshake();
        waveIn.StartRecording();
        Thread receiverThread = new Thread(ReceiveLoop) { IsBackground = true };
        receiverThread.Start();
        api.Logger.Notification("[SimpleVoiceChat] Voice endpoint started");
    }

    private void DoHandshake()
    {
        var handshakeRequest = new ControlPacket
        {
            Type = "handshake"
        };

        byte[] data = handshakeRequest.Serialize();
        var initialTarget = new IPEndPoint(IPAddress.Loopback, config.Port); // handshake na loopback

        udpClient.Send(data, data.Length, initialTarget);

        var result = udpClient.ReceiveAsync().Result;
        var reply = ControlPacket.Deserialize(result.Buffer);

        if (reply.Type == "handshake-reply")
        {
            serverEndpoint = new IPEndPoint(IPAddress.Parse(reply.ServerIp), reply.ServerPort);
            api.Logger.Notification($"[SimpleVoiceChat] Handshake successful, server IP: {reply.ServerIp}, port: {reply.ServerPort}");
        }
        else
        {
            api.Logger.Error("[SimpleVoiceChat] Invalid handshake response!");
        }
    }

    public void Stop()
    {
        running = false;
        waveIn?.StopRecording();
        udpClient?.Close();
        waveOut?.Stop();
        waveOut?.Dispose();
        api.Logger.Notification("[SimpleVoiceChat] Voice endpoint stopped");
    }

    private void OnDataAvailable(object sender, WaveInEventArgs e)
    {
        if (serverEndpoint == null) return;  // 🛑 Upewniamy się, że handshake się odbył

        short[] samples = new short[e.BytesRecorded / 2];
        Buffer.BlockCopy(e.Buffer, 0, samples, 0, e.BytesRecorded);

        int encodedLength = encoder.Encode(samples, 0, FrameSize, encodeBuffer, 0, encodeBuffer.Length);
        byte[] encodedAudio = encodeBuffer.Take(encodedLength).ToArray();

        var packet = new AudioPacket
        {
            PlayerId = playerId,
            AudioData = encodedAudio,
            Position = GetPlayerPosition(),
            Direction = GetPlayerDirection()
        };

        byte[] data = packet.Serialize();
        udpClient.Send(data, data.Length, serverEndpoint);
    }

    private void ReceiveLoop()
    {
        while (running)
        {
            try
            {
                var result = udpClient.ReceiveAsync().Result;
                HandleIncomingPacket(result.Buffer);
            }
            catch (Exception ex)
            {
                api.Logger.Error($"[SimpleVoiceChat] Receive error: {ex.Message}");
            }
        }
    }

    private void HandleIncomingPacket(byte[] data)
    {
        var packet = AudioPacket.Deserialize(data);

        if (packet.PlayerId == playerId) return;  // 🛑 Echo prevention

        short[] pcmShort = new short[FrameSize];
        int decodedSamples = decoder.Decode(packet.AudioData, 0, packet.AudioData.Length, pcmShort, 0, FrameSize, false);

        byte[] decodedPcm = new byte[decodedSamples * 2];
        Buffer.BlockCopy(pcmShort, 0, decodedPcm, 0, decodedSamples * 2);

        bufferedWaveProvider.AddSamples(decodedPcm, 0, decodedSamples * 2);
    }

    private Vector3 GetPlayerPosition()
    {
        return new Vector3(
            (float)api.World.Player.Entity.Pos.X,
            (float)api.World.Player.Entity.Pos.Y,
            (float)api.World.Player.Entity.Pos.Z
        );
    }

    private Vector3 GetPlayerDirection()
    {
        return new Vector3(
            (float)api.World.Player.Entity.SidedPos.Motion.X,
            (float)api.World.Player.Entity.SidedPos.Motion.Y,
            (float)api.World.Player.Entity.SidedPos.Motion.Z
        );
    }
}
