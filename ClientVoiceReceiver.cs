using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Concentus.Structs;
using NAudio.Wave;

namespace SimpleVoiceChat;

public class ClientVoiceReceiver
{
    private readonly UdpClient udpClient;
    private readonly IPEndPoint listenEndPoint;

    private readonly OpusDecoder decoder;
    private readonly BufferedWaveProvider bufferedWaveProvider;
    private readonly WaveOutEvent waveOut;

    private volatile bool running = true;

    private const int SampleRate = 48000;
    private const int Channels = 1;
    private const int FrameSize = 960; // 20ms frame at 48kHz

    public ClientVoiceReceiver(int listenPort)
    {
        listenEndPoint = new IPEndPoint(IPAddress.Any, listenPort);
        udpClient = new UdpClient(listenEndPoint);

        decoder = OpusDecoder.Create(SampleRate, Channels);

        bufferedWaveProvider = new BufferedWaveProvider(new WaveFormat(SampleRate, Channels))
        {
            BufferDuration = TimeSpan.FromSeconds(2),
            DiscardOnBufferOverflow = true
        };

        waveOut = new WaveOutEvent();
        waveOut.Init(bufferedWaveProvider);
        waveOut.Play();
    }

    public void Start()
    {
        Thread receiveThread = new Thread(ReceiveLoop)
        {
            IsBackground = true
        };
        receiveThread.Start();
        Console.WriteLine("[ClientVoiceReceiver] Receiver started");
    }

    public void Stop()
    {
        running = false;
        udpClient?.Close();
        waveOut?.Stop();
        waveOut?.Dispose();
        Console.WriteLine("[ClientVoiceReceiver] Receiver stopped");
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
                Console.WriteLine($"[ClientVoiceReceiver] Error: {ex.Message}");
            }
        }
    }

    private void HandleIncomingPacket(byte[] data)
    {
        var packet = AudioPacket.Deserialize(data);
        byte[] decodedPcm = new byte[FrameSize * 2]; // 16-bit samples (2 bytes per sample)

        // Dekodowanie Opus do PCM (short samples)
        short[] pcmShort = new short[FrameSize];
        int decodedSamples = decoder.Decode(packet.AudioData, 0, packet.AudioData.Length, pcmShort, 0, FrameSize, false);

        // Zamiana short[] na byte[]
        Buffer.BlockCopy(pcmShort, 0, decodedPcm, 0, decodedSamples * 2);

        // TODO: Ustaw głośność w zależności od odległości (packet.Volume lub obliczenia)
        bufferedWaveProvider.AddSamples(decodedPcm, 0, decodedSamples * 2);
    }
}
