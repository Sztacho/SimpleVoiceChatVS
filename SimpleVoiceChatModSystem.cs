using System;
using System.Numerics;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.API.Client;
using Vintagestory.API.Util;

namespace SimpleVoiceChat;

public class SimpleVoiceChatModSystem : ModSystem
{
    private VoiceChatServer voiceChatServer;
    private PositionSync positionSync;
    private ClientVoiceEndpoint clientVoiceEndpoint;

    private Config config;

    public override void StartServerSide(ICoreServerAPI api)
    {
        base.StartServerSide(api);
        var logger = api.Logger;

        logger.Notification("[SimpleVoiceChat] Server-side started");

        config = Config.Load("voicechatconfig.json");
        if (!config.Enabled)
        {
            logger.Notification("[SimpleVoiceChat] Voice Chat is disabled in config.");
            return;
        }

        voiceChatServer = new VoiceChatServer(config);
        Task.Run(() => voiceChatServer.Start());

        // Możesz włączyć, jeśli masz PositionSync
        // positionSync = new PositionSync(api, voiceChatServer);

        logger.Notification($"[SimpleVoiceChat] Voice Chat server listening on port {config.Port}");
    }

    public override void StartClientSide(ICoreClientAPI api)
    {
        base.StartClientSide(api);
        var logger = api.Logger;

        logger.Notification("[SimpleVoiceChat] Client-side started");

        config = Config.Load("voicechatconfig.json");
        if (!config.Enabled)
        {
            logger.Notification("[SimpleVoiceChat] Voice Chat is disabled in config.");
            return;
        }

        api.Event.PlayerEntitySpawn += (clapi) =>
        {
            logger.Notification("[SimpleVoiceChat] Player loaded, initializing voice endpoint");
            logger.Notification($"[SimpleVoiceChat] Player UID: {api.World.Player.PlayerUID}");

            clientVoiceEndpoint = new ClientVoiceEndpoint(
                playerId: api.World.Player.PlayerUID,
                config: config,
                api: api
            );

            clientVoiceEndpoint.Start();
        };
    }

    public override void Dispose()
    {
        voiceChatServer?.Stop();
        voiceChatServer = null;

        clientVoiceEndpoint?.Stop();
        clientVoiceEndpoint = null;

        positionSync = null;

        base.Dispose();
    }
}
