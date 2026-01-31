using PacketManager.Core.Data;

namespace PacketManager.Core.Abstractions;

/// <summary>
/// Генератор байтовых массивов пакетов. Отвечает за сериализацию пакетов 
/// с использованием оригинальной логики игры или кастомных билдеров.
/// </summary>
public interface IPacketGenerator
{
    /// <summary>
    /// Генерирует оригинальный пакет, используя стандартную логику игры (SendData).
    /// </summary>
    /// <param name="messageId">Идентификатор типа пакета.</param>
    /// <param name="data">Оригинальные данные пакета (параметры SendData).</param>
    /// <returns>Массив байтов готового пакета, включая заголовок длины (2 байта).</returns>
    /// <remarks>
    /// Использует IL-хуки или Reflection для вызова оригинального метода SendData
    /// и перехвата сгенерированных байтов.
    /// </remarks>
    byte[] GenerateOriginal(int messageId, PacketData data);

    /// <summary>
    /// Генерирует кастомный пакет, используя предоставленный билдер.
    /// </summary>
    /// <param name="builder">Билдер для генерации содержимого пакета.</param>
    /// <param name="messageId">Идентификатор типа пакета.</param>
    /// <param name="data">Оригинальные данные для передачи в контекст билдера.</param>
    /// <param name="targets">Целевые клиенты, которым будет отправлен пакет.</param>
    /// <returns>Массив байтов готового пакета с заголовком длины.</returns>
    byte[] GenerateCustom(IPacketBuilder builder, int messageId, PacketData data,
        IReadOnlyCollection<INetworkClient> targets);
}