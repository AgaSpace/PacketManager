namespace PacketManager.Core.Abstractions;

/// <summary>
/// Сервис для сетевых операций. Абстракция над конкретной реализацией сети (Terraria/другая игра).
/// </summary>
public interface INetworkService
{
    /// <summary>
    /// Отправляет данные указанным клиентам.
    /// </summary>
    /// <param name="clients">Коллекция клиентов-получателей.</param>
    /// <param name="data">Буфер данных для отправки.</param>
    void SendTo(IEnumerable<INetworkClient> clients, ReadOnlyMemory<byte> data);

    /// <summary>
    /// Получает список активных (подключенных) клиентов.
    /// </summary>
    /// <param name="excludeId">Идентификатор клиента для исключения из списка (optional).</param>
    /// <returns>Перечисление активных клиентов.</returns>
    IEnumerable<INetworkClient> GetActiveClients(int? excludeId = null);
}