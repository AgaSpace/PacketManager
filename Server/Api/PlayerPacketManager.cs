using PacketManager.Core.Abstractions;

namespace PacketManager.Server.Api;

/// <summary>
/// Контекстно-зависимый менеджер билдеров для конкретного игрока.
/// Предоставляет упрощенный API для работы с билдерами без указания playerId в каждом вызове.
/// </summary>
/// <remarks>
/// Получается через расширение <see cref="Extensions.TSPlayerExtensions.GetPacketManager(TSPlayer)"/>.
/// </remarks>
/// <remarks>
/// Создает новый экземпляр менеджера для указанного игрока.
/// </remarks>
/// <param name="id">Индекс игрока (whoAmI).</param>
public class PlayerPacketManager(int id)
{
    public readonly int Index = id;

    /// <summary>
    /// Добавляет <see cref="IPacketBuilder"/> для этого игрока.
    /// </summary>
    /// <param name="builder"><see cref="IPacketBuilder"/> для добавления.</param>
    /// <returns><see cref="true"/> при успехе.</returns>
    public bool Add(IPacketBuilder builder) => Facade.AddBuilder(Index, builder);

    /// <summary>
    /// Удаляет <see cref="IPacketBuilder"/> у этого игрока.
    /// </summary>
    /// <param name="builder"><see cref="IPacketBuilder"/> для удаления.</param>
    /// <returns><see cref="true"/> при успехе.</returns>
    public bool Remove(IPacketBuilder builder) => Facade.RemoveBuilder(Index, builder);

    /// <summary>
    /// Проверяет, содержит ли реестр указанный экземпляр <see cref="IPacketBuilder"/> для этого игрока.
    /// </summary>
    /// <param name="builder"><see cref="IPacketBuilder"/> для проверки (сравнение по ссылке).</param>
    /// <returns><see cref="true"/>, если <see cref="IPacketBuilder"/> добавлен для данного игрока; иначе <see cref="false"/>.</returns>
    public bool Contains(IPacketBuilder builder) => Facade.Contains(Index, builder);

    /// <summary>
    /// Проверяет наличие <see cref="IPacketBuilder"/> для указанного типа пакета.
    /// </summary>
    /// <param name="messageId">Идентификатор типа пакета.</param>
    /// <returns><see cref="true"/>, если существует хоть один <see cref="IPacketBuilder"/>.</returns>
    public bool Has(int messageId) => Facade.HasBuilder(Index, messageId);
}