#region Using

using PacketManager.Server.Api;

using Terraria;
using TShockAPI;

#endregion

namespace PacketManager.Server.Extensions;

public static class TSPlayerExtensions
{
    public static PlayerPacketManager GetPacketManager(this TSPlayer player) =>
        new(player.Index);

    public static PlayerPacketManager GetPacketManager(this Player player) =>
        new(player.whoAmI);
}
