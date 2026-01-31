using PacketManager.Core.Abstractions;
using PacketManager.Core.Data;

namespace PacketManager.Server.Adapters;

/// <summary>
/// Реализация контекста сборки пакета для Terraria.
/// Расширяет стандартный контекст дополнительным полем Num, 
/// получаемым из оригинального метода OnPacketWrite.
/// </summary>
/// <remarks>
/// Создает новый экземпляр контекста для Terraria.
/// </remarks>
/// <param name="messageId">Идентификатор типа пакета.</param>
/// <param name="writer">Бинарный писатель.</param>
/// <param name="targets">Список клиентов Terraria.</param>
/// <param name="data">Оригинальные данные пакета.</param>
/// <param name="num">Вспомогательное числовое значение из OnPacketWrite.</param>
internal class TerrariaBuildContext(byte messageId, BinaryWriter writer,
    List<TerrariaNetworkClient> targets, PacketData data, int num) : IPacketBuildContext
{
    /// <summary>
    /// Получает идентификатор типа пакета.
    /// </summary>
    public byte MessageId { get; } = messageId;

    /// <summary>
    /// Получает бинарный писатель для записи данных.
    /// </summary>
    public BinaryWriter Writer { get; } = writer;

    /// <summary>
    /// Получает список целевых клиентов Terraria.
    /// </summary>
    public IReadOnlyCollection<INetworkClient> Targets { get; } = targets;

    /// <summary>
    /// Получает оригинальные данные пакета.
    /// </summary>
    public PacketData OriginalData { get; } = data;

    /// <summary>
    /// Получает вспомогательное числовое значение Num, переданное из OnPacketWrite.
    /// Может использоваться для специфичной логики Terraria (например, индекса пакета).
    /// </summary>
    public int Num { get; } = num;
}