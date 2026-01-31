using PacketManager.Core.Abstractions;

namespace PacketManager.Server.Api;

/// <summary>
/// Контекстно-зависимый менеджер билдеров для конкретного игрока.
/// Предоставляет упрощенный API для работы с билдерами без указания playerId в каждом вызове.
/// </summary>
/// <remarks>
/// Получается через расширение <see cref="Extensions.TSPlayerExtensions.GetPacketManager(Terraria.Player)"/>.
/// </remarks>
/// <remarks>
/// Создает новый экземпляр менеджера для указанного игрока.
/// </remarks>
/// <param name="id">Индекс игрока (<see cref="Terraria.RemoteClient.Id"/>).</param>
public class PlayerPacketManager(int id)
{
    private readonly int _id = id;

    /// <summary>
    /// Добавляет <see cref="IPacketBuilder"/> пакета для этого игрока.
    /// </summary>
    /// <param name="builder"><see cref="IPacketBuilder"/> для добавления.</param>
    /// <returns>true при успехе.</returns>
    public bool Add(IPacketBuilder builder) => Facade.AddBuilder(_id, builder);

    /// <summary>
    /// Удаляет <see cref="IPacketBuilder"/> пакета у этого игрока.
    /// </summary>
    /// <param name="builder"><see cref="IPacketBuilder"/> для удаления.</param>
    /// <returns>true при успехе.</returns>
    public bool Remove(IPacketBuilder builder) => Facade.RemoveBuilder(_id, builder);

    /// <summary>
    /// Проверяет наличие <see cref="IPacketBuilder"/> для указанного типа пакета.
    /// </summary>
    /// <param name="messageId">Идентификатор типа пакета.</param>
    /// <returns>true, если <see cref="IPacketBuilder"/> зарегистрирован.</returns>
    public bool Has(int messageId) => Facade.HasBuilder(_id, messageId);
}