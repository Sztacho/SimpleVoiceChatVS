using System;
using Vintagestory.API.Server;

namespace SimpleVoiceChat;

public class PositionSync
{
    private ICoreServerAPI api;
    private VoiceChatServer voiceChatServer;

    public PositionSync(ICoreServerAPI api, VoiceChatServer voiceChatServer)
    {
        this.api = api;
        this.voiceChatServer = voiceChatServer;
        api.Event.RegisterGameTickListener(OnTick, 50); // Co 50ms
    }

    private void OnTick(float dt)
    {
        foreach (var player in api.World.AllOnlinePlayers)
        {
            var position = player.Entity.Pos.XYZ;
            var direction = player.Entity.SidedPos.Motion;
            voiceChatServer.UpdatePlayerPosition(player.PlayerUID, position, direction);
        }
    }
}