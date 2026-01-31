using PacketManager.Core.Abstractions;
using PacketManager.Core.Data;

namespace PacketManager.Server.Adapters;

/// <summary>
/// Реализация контекста сборки пакета для <see cref="Terraria"/>.
/// Расширяет стандартный контекст дополнительным полем <see cref="num"/>, 
/// получаемым из оригинального метода <see cref="Terraria.NetMessage.OnPacketWrite"/>.
/// </summary>
/// <remarks>
/// Создает новый экземпляр контекста для <see cref="Terraria"/>.
/// </remarks>
/// <param name="messageId">Идентификатор типа пакета.</param>
/// <param name="writer">Бинарный писатель.</param>
/// <param name="targets">Список клиентов <see cref="Terraria"/>.</param>
/// <param name="data">Оригинальные данные пакета.</param>
/// <param name="num">Вспомогательное числовое значение из <see cref="Terraria.NetMessage.OnPacketWrite"/>.</param>
internal class TerrariaBuildContext(int messageId, BinaryWriter writer,
    List<TerrariaNetworkClient> targets, PacketData data, int num) : IPacketBuildContext
{
    /// <summary>
    /// Получает идентификатор типа пакета.
    /// </summary>
    public int MessageId { get; } = messageId;

    /// <summary>
    /// Получает бинарный писатель для записи данных.
    /// </summary>
    public BinaryWriter Writer { get; } = writer;

    /// <summary>
    /// Получает список целевых клиентов <see cref="Terraria"/>.
    /// </summary>
    public IReadOnlyCollection<INetworkClient> Targets { get; } = targets;

    /// <summary>
    /// Получает оригинальные данные пакета.
    /// </summary>
    public PacketData OriginalData { get; } = data;

    /// <summary>
    /// Получает вспомогательное числовое значение <see cref="num"/>, переданное из <see cref="Terraria.NetMessage.OnPacketWrite"/>.
    /// Может использоваться для специфичной логики <see cref="Terraria"/> (например, индекса игрока).
    /// </summary>
    public int Num { get; } = num;
}