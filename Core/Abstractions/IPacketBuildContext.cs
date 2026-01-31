using PacketManager.Core.Data;

namespace PacketManager.Core.Abstractions;

/// <summary>
/// Контекст, предоставляемый билдеру пакета для доступа к данным и инструментам генерации.
/// </summary>
public interface IPacketBuildContext
{
    /// <summary>
    /// Получает идентификатор типа обрабатываемого пакета.
    /// </summary>
    int MessageId { get; }

    /// <summary>
    /// Получает бинарный писатель для записи данных пакета.
    /// </summary>
    /// <remarks>
    /// Позиция потока находится сразу после записи <see cref="MessageId"/> (смещение 3).
    /// Для изменения уже записанных данных можно использовать Seek: 
    /// <code>Writer.BaseStream.Position = 3;</code>
    /// </remarks>
    BinaryWriter Writer { get; }

    /// <summary>
    /// Получает коллекцию клиентов, которым будет отправлен сгенерированный пакет.
    /// </summary>
    IReadOnlyCollection<INetworkClient> Targets { get; }

    /// <summary>
    /// Получает оригинальные данные пакета, переданные в SendData.
    /// </summary>
    /// <remarks>
    /// Поле Number обычно содержит ID игрока (whoAmI) для пакетов, связанных с игроками.
    /// </remarks>
    PacketData OriginalData { get; }
}