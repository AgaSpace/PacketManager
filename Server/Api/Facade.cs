using PacketManager.Core.Abstractions;

namespace PacketManager.Server.Api;

/// <summary>
/// Фасад для доступа к API PacketManager из других плагинов.
/// Предоставляет статические методы для управления билдерами без необходимости 
/// получения ссылок на сервисы через Dependency Injection.
/// </summary>
public static class Facade
{
    private static Core.Implementations.PacketManager? _manager;

    /// <summary>
    /// Инициализирует фасад. Вызывается автоматически при загрузке PacketManagerPlugin.
    /// </summary>
    /// <param name="manager">Экземпляр менеджера пакетов.</param>
    /// <exception cref="InvalidOperationException">Выбрасывается, если фасад уже инициализирован.</exception>
    internal static void Initialize(Core.Implementations.PacketManager manager) => _manager = manager;

    /// <summary>
    /// Получает реестр билдеров для прямого управления.
    /// </summary>
    /// <exception cref="InvalidOperationException">Выбрасывается, если плагин не инициализирован.</exception>
    public static IPacketBuilderRegistry Registry =>
        _manager?.Registry ?? throw new InvalidOperationException("PacketManager not initialized. Ensure PacketManagerPlugin is loaded before using Facade.");

    /// <summary>
    /// Добавляет билдер пакета для указанного игрока.
    /// </summary>
    /// <param name="playerId">Индекс игрока (whoAmI).</param>
    /// <param name="builder">Билдер для добавления.</param>
    /// <returns>true при успешном добавлении, false при ошибке.</returns>
    public static bool AddBuilder(int playerId, IPacketBuilder builder) =>
        Registry.AddBuilder(playerId, builder).IsSuccess;

    /// <summary>
    /// Удаляет билдер пакета у указанного игрока.
    /// </summary>
    /// <param name="playerId">Индекс игрока.</param>
    /// <param name="builder">Экземпляр билдера для удаления (должен быть тем же объектом, что был добавлен).</param>
    /// <returns>true при успешном удалении, false если билдер не найден.</returns>
    public static bool RemoveBuilder(int playerId, IPacketBuilder builder) =>
        Registry.RemoveBuilder(playerId, builder).IsSuccess;

    /// <summary>
    /// Проверяет наличие билдера для указанного типа пакета у игрока.
    /// </summary>
    /// <param name="playerId">Индекс игрока.</param>
    /// <param name="messageId">Идентификатор типа пакета (Message ID).</param>
    /// <returns>true, если для данного типа пакета зарегистрирован билдер.</returns>
    public static bool HasBuilder(int playerId, byte messageId) =>
        Registry.HasBuilder(playerId, messageId);
}