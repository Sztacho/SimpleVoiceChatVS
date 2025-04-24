using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using Vintagestory.API.MathTools;

namespace SimpleVoiceChat;

public class VoiceChatServer
{
    private readonly UdpClient udpClient;
    private readonly Config config;
    private volatile bool running = true;

    private readonly Dictionary<string, VoiceClientHandler> clients = new();
    private readonly Dictionary<IPEndPoint, string> endpointToPlayer = new(); // Pomocnicza mapa

    public VoiceChatServer(Config config)
    {
        this.config = config;
        udpClient = new UdpClient(config.Port);
    }

    public void Start()
    {
        Console.WriteLine($"[VoiceChatServer] Listening on port {config.Port}");
        while (running)
        {
            try
            {
                var result = udpClient.ReceiveAsync().Result;
                HandleIncomingPacket(result.Buffer, result.RemoteEndPoint);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[VoiceChatServer] Error: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Aktualizuje pozycję i kierunek patrzenia gracza.
    /// Wołane z PositionSync co tick.
    /// </summary>
    public void UpdatePlayerPosition(string playerId, IVec3 position, IVec3 direction)
    {
        if (clients.TryGetValue(playerId, out var client))
        {
            client.Position = new Vector3(position.XAsInt, position.YAsInt, position.ZAsInt);
            client.Direction = new Vector3(direction.XAsFloat, direction.YAsFloat, direction.ZAsFloat);
        }
        else
        {
            // Gracz jeszcze nie zarejestrował się przez voice — dodaj placeholder
            clients[playerId] = new VoiceClientHandler
            {
                PlayerId = playerId,
                Position = new Vector3(position.XAsInt, position.YAsInt, position.ZAsInt),
                Direction = new Vector3(direction.XAsFloat, direction.YAsFloat, direction.ZAsFloat)
            };
        }
    }

    /// <summary>
    /// Usuwa klienta, który się rozłączył (można wywoływać np. po timeoutach).
    /// </summary>
    public void RemoveClient(string playerId)
    {
        if (clients.TryGetValue(playerId, out var client))
        {
            endpointToPlayer.Remove(client.EndPoint);
            clients.Remove(playerId);
            Console.WriteLine($"[VoiceChatServer] Client {playerId} removed.");
        }
    }

    public void Stop()
    {
        running = false;
        udpClient?.Close();
        Console.WriteLine("[VoiceChatServer] Stopped.");
    }
    
    private void HandleIncomingPacket(byte[] data, IPEndPoint sender)
    {
        try
        {
            var packetType = DetectPacketType(data);

            if (packetType == "handshake")
            {
                var reply = new ControlPacket
                {
                    Type = "handshake-reply",
                    ServerIp = ((IPEndPoint)sender).Address.ToString(),
                    ServerPort = config.Port
                };

                byte[] replyData = reply.Serialize();
                udpClient.Send(replyData, replyData.Length, sender);
                Console.WriteLine($"[VoiceChatServer] Sent handshake-reply to {sender.Address}");
                return;
            }

            var packet = AudioPacket.Deserialize(data);
            if (!clients.TryGetValue(packet.PlayerId, out var senderHandler))
            {
                senderHandler = new VoiceClientHandler
                {
                    PlayerId = packet.PlayerId,
                    EndPoint = sender
                };
                clients[packet.PlayerId] = senderHandler;
            }

            senderHandler.Position = packet.Position;
            senderHandler.Direction = packet.Direction;

            foreach (var client in clients.Values)
            {
                if (client.PlayerId == packet.PlayerId) continue;

                float distance = senderHandler.DistanceTo(client);
                if (distance <= config.MaxRange)
                {
                    float volume = AudioProcessor.CalculateLogarithmicVolume(distance, config.MaxRange);
                    byte[] processedAudio = AudioProcessor.ApplyEffects(packet.AudioData, distance, config.MaxRange);
                    client.SendAudio(processedAudio, volume, packet.Direction);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[VoiceChatServer] Error in packet handling: {ex.Message}");
        }
    }

    private string DetectPacketType(byte[] data)
    {
        using var ms = new MemoryStream(data);
        using var reader = new BinaryReader(ms);
        return reader.ReadString();
    }

}
