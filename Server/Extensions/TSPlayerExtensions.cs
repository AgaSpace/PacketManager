using PacketManager.Server.Api;
using Terraria;
using TShockAPI;

namespace PacketManager.Server.Extensions;

/// <summary>
/// Методы расширения для TSPlayer и Player, предоставляющие доступ к менеджеру пакетов.
/// </summary>
public static class TSPlayerExtensions
{
    /// <summary>
    /// Получает менеджер пакетов для <see cref="TSPlayer"/>.
    /// </summary>
    /// <param name="player">Экземпляр <see cref="TSPlayer"/>.</param>
    /// <returns>Менеджер билдеров для данного игрока.</returns>
    public static PlayerPacketManager GetPacketManager(this TSPlayer player) =>
        new(player.Index);

    /// <summary>
    /// Получает менеджер пакетов для игрока <see cref="Terraria"/>.
    /// </summary>
    /// <param name="player">Экземпляр <see cref="Terraria.Player"/>.</param>
    /// <returns>Менеджер билдеров для данного игрока.</returns>
    public static PlayerPacketManager GetPacketManager(this Player player) =>
        new(player.whoAmI);
}