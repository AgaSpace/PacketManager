using PacketManager.Core.Abstractions;
using PacketManager.Core.Data;

namespace PacketManager.Core.Implementations;

/// <summary>
/// Упрощенная реализация генератора пакетов, не зависящая от специфичных механизмов Terraria.
/// Используется для тестирования, unit-тестов или как fallback при отсутствии IL-хуков.
/// </summary>
/// <remarks>
/// В отличие от <see cref="TerrariaPacketGenerator"/>, не использует NetMessage.orig_SendData
/// и не зависит от сборок Terraria. GenerateOriginal создает минимальный пакет,
/// а GenerateCustom делегирует заполнение тела переданному билдеру.
/// </remarks>
public class SimplePacketGenerator : IPacketGenerator
{
    /// <summary>
    /// Генерирует базовый "заглушечный" пакет с указанным MessageId.
    /// </summary>
    /// <param name="messageId">Идентификатор типа пакета.</param>
    /// <param name="data">Данные пакета (в данной реализации не используются для заполнения тела).</param>
    /// <returns>Массив байтов пакета длиной 3 байта (2 байта длины + 1 байт MessageId).</returns>
    /// <remarks>
    /// Используется в тестовых сценариях или когда требуется минимальная реализация.
    /// Не воспроизводит полную сериализацию пакетов Terraria.
    /// </remarks>
    public virtual byte[] GenerateOriginal(byte messageId, PacketData data)
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);
        writer.Write((short)0); // Резерв под длину (2 байта)
        writer.Write(messageId); // ID пакета (1 байт)
        return ms.ToArray();
    }

    /// <summary>
    /// Генерирует кастомный пакет, используя предоставленный билдер для заполнения содержимого.
    /// </summary>
    /// <param name="builder">Билдер, который заполнит тело пакета. Не может быть null.</param>
    /// <param name="messageId">Идентификатор типа пакета.</param>
    /// <param name="data">Оригинальные данные пакета для передачи в контекст билдера.</param>
    /// <param name="targets">Целевые клиенты, которым будет отправлен пакет.</param>
    /// <returns>Массив байтов готового пакета с корректным заголовком длины в первых двух байтах.</returns>
    /// <remarks>
    /// Алгоритм работы:
    /// <list type="number">
    /// <item>Создает MemoryStream и BinaryWriter.</item>
    /// <item>Пропускает 2 байта (резерв для длины), записывает MessageId.</item>
    /// <item>Вызывает <see cref="IPacketBuilder.Build"/> для заполнения остального содержимого.</item>
    /// <item>Вычисляет фактическую длину, возвращается в начало и записывает её.</item>
    /// </list>
    /// </remarks>
    public byte[] GenerateCustom(IPacketBuilder builder, byte messageId, PacketData data,
        IReadOnlyCollection<INetworkClient> targets)
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        ms.Position = 2; // Резерв под длину (short)
        writer.Write(messageId);

        // Вызываем билдер для заполнения тела пакета
        var context = new PacketBuildContext(messageId, writer, targets, data);
        builder.Build(context);

        // Записываем длину в начало
        short len = (short)ms.Position;
        ms.Position = 0;
        writer.Write(len);

        return ms.ToArray();
    }
}