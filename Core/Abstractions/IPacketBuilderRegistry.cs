using PacketManager.Core.Data;

namespace PacketManager.Core.Abstractions;

/// <summary>
/// Реестр билдеров пакетов. Управляет добавлением, удалением и поиском 
/// билдеров для каждого игрока отдельно.
/// </summary>
/// <remarks>
/// Реализация потокобезопасна и может использоваться из множества потоков одновременно.
/// </remarks>
public interface IPacketBuilderRegistry
{
    /// <summary>
    /// Добавляет билдер пакета для указанного игрока.
    /// </summary>
    /// <param name="playerId">Индекс игрока (whoAmI), обычно от 0 до 255.</param>
    /// <param name="builder">Экземпляр билдера для добавления.</param>
    /// <returns>
    /// Результат операции. <see cref="Result.IsSuccess"/> будет true при успешном добавлении.
    /// Возвращает ошибку, если playerId вне допустимого диапазона.
    /// </returns>
    /// <remarks>
    /// Можно добавить несколько билдеров с одинаковым MessageId, но с разными приоритетами.
    /// При отправке пакета будет использован только билдер с максимальным приоритетом.
    /// </remarks>
    Result AddBuilder(int playerId, IPacketBuilder builder);

    /// <summary>
    /// Удаляет ранее добавленный билдер у указанного игрока.
    /// </summary>
    /// <param name="playerId">Индекс игрока (whoAmI).</param>
    /// <param name="builder">Тот же экземпляр билдера, который был добавлен ранее.</param>
    /// <returns>
    /// Результат операции. Ошибка возвращается, если билдер не найден или playerId неверен.
    /// </returns>
    Result RemoveBuilder(int playerId, IPacketBuilder builder);

    /// <summary>
    /// Проверяет, содержит ли реестр указанный экземпляр билдера для игрока.
    /// </summary>
    /// <param name="playerId">Индекс игрока.</param>
    /// <param name="builder">Экземпляр билдера для проверки (сравнение по ссылке).</param>
    /// <returns>true, если билдер добавлен для данного игрока; иначе false.</returns>
    bool Contains(int playerId, IPacketBuilder builder);

    /// <summary>
    /// Проверяет, есть ли хотя бы один билдер для указанного типа пакета у игрока.
    /// </summary>
    /// <param name="playerId">Индекс игрока.</param>
    /// <param name="messageId">Идентификатор типа пакета.</param>
    /// <returns>true, если для данного типа пакета зарегистрирован билдер.</returns>
    bool HasBuilder(int playerId, int messageId);

    /// <summary>
    /// Получает билдер с максимальным приоритетом для указанного типа пакета.
    /// </summary>
    /// <param name="playerId">Индекс игрока.</param>
    /// <param name="messageId">Идентификатор типа пакета.</param>
    /// <returns>
    /// Билдер с наивысшим приоритетом или null, если билдеров для данного типа нет.
    /// </returns>
    IPacketBuilder? GetBuilder(int playerId, int messageId);
}