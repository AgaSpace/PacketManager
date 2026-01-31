using PacketManager.Core.Abstractions;
using PacketManager.Core.Data;

namespace PacketManager.Core.Implementations;

/// <summary>
/// Реализация контекста сборки пакета для использования в Core.
/// Предоставляет доступ к бинарному писателю и данным пакета.
/// </summary>
/// <remarks>
/// Создает новый экземпляр контекста сборки.
/// </remarks>
/// <param name="messageId">Идентификатор типа пакета.</param>
/// <param name="writer">Бинарный писатель.</param>
/// <param name="targets">Целевые клиенты.</param>
/// <param name="data">Оригинальные данные.</param>
internal class PacketBuildContext(int messageId, BinaryWriter writer,
    IReadOnlyCollection<INetworkClient> targets, PacketData data) : IPacketBuildContext
{
    /// <summary>
    /// Получает идентификатор типа пакета.
    /// </summary>
    public int MessageId { get; } = messageId;

    /// <summary>
    /// Получает бинарный писатель для записи данных пакета.
    /// </summary>
    public BinaryWriter Writer { get; } = writer;

    /// <summary>
    /// Получает список целевых клиентов для пакета.
    /// </summary>
    public IReadOnlyCollection<INetworkClient> Targets { get; } = targets;

    /// <summary>
    /// Получает оригинальные данные пакета.
    /// </summary>
    public PacketData OriginalData { get; } = data;
}