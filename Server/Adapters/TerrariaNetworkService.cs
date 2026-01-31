using Terraria;
using PacketManager.Core.Abstractions;

namespace PacketManager.Server.Adapters;

/// <summary>
/// Реализация сетевого сервиса для <see cref="Terraria"/>.
/// Обеспечивает отправку данных клиентам и получение списка активных игроков.
/// </summary>
public class TerrariaNetworkService : INetworkService
{
    /// <summary>
    /// Отправляет данные указанным клиентам через их сокеты.
    /// </summary>
    /// <param name="clients">Коллекция клиентов (должны быть <see cref="TerrariaNetworkClient"/>).</param>
    /// <param name="data">Данные для отправки.</param>
    /// <exception cref="InvalidCastException">Выбрасывается, если переданы не <see cref="TerrariaNetworkClient"/>.</exception>
    public void SendTo(IEnumerable<INetworkClient> clients, ReadOnlyMemory<byte> data)
    {
        foreach (var client in clients.Cast<TerrariaNetworkClient>())
        {
            client.Send(data);
        }
    }

    /// <summary>
    /// Получает список активных (подключенных) клиентов игры.
    /// </summary>
    /// <param name="excludeId">Идентификатор клиента для исключения из списка (например, отправителя).</param>
    /// <returns>Перечисление активных клиентов, обернутых в <see cref="TerrariaNetworkClient"/>.</returns>
    public IEnumerable<INetworkClient> GetActiveClients(int? excludeId = null)
    {
        return Netplay.Clients
            .Where(c => c?.IsActive == true && c.Id != excludeId)
            .Select(c => new TerrariaNetworkClient(c));
    }
}