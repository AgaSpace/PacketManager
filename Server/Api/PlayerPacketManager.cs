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
    private readonly int _id = id;

    /// <summary>
    /// Добавляет билдер пакета для этого игрока.
    /// </summary>
    /// <param name="builder">Билдер для добавления.</param>
    /// <returns>true при успехе.</returns>
    public bool Add(IPacketBuilder builder) => Facade.AddBuilder(_id, builder);

    /// <summary>
    /// Удаляет билдер пакета у этого игрока.
    /// </summary>
    /// <param name="builder">Билдер для удаления.</param>
    /// <returns>true при успехе.</returns>
    public bool Remove(IPacketBuilder builder) => Facade.RemoveBuilder(_id, builder);

    /// <summary>
    /// Проверяет наличие билдера для указанного типа пакета.
    /// </summary>
    /// <param name="messageId">Идентификатор типа пакета.</param>
    /// <returns>true, если билдер зарегистрирован.</returns>
    public bool Has(byte messageId) => Facade.HasBuilder(_id, messageId);
}