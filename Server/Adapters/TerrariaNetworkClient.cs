using Terraria;
using Terraria.Net.Sockets;
using PacketManager.Core.Abstractions;

namespace PacketManager.Server.Adapters;

/// <summary>
/// Адаптер клиента Terraria (<see cref="RemoteClient"/>) для интерфейса <see cref="INetworkClient"/>.
/// Оборачивает функциональность <see cref="RemoteClient"/> в абстракцию <see cref="Core"/>.
/// </summary>
/// <remarks>
/// Создает новый адаптер для <see cref="RemoteClient"/>.
/// </remarks>
/// <param name="client">Оригинальный клиент Terraria.</param>
public class TerrariaNetworkClient(RemoteClient client) : INetworkClient
{
    private readonly RemoteClient _client = client;

    /// <summary>
    /// Получает идентификатор клиента (<see cref="Entity.whoAmI"/>).
    /// </summary>
    public int Id => _client.Id;

    /// <summary>
    /// Получает значение, указывающее, активно ли подключение клиента.
    /// </summary>
    public bool IsConnected => _client.IsActive;

    /// <summary>
    /// Получает оригинальный <see cref="RemoteClient"/> для доступа к специфичным методам <see cref="Terraria"/>.
    /// </summary>
    public RemoteClient Native => _client;

    /// <summary>
    /// Отправляет данные клиенту через сокет <see cref="Terraria"/>.
    /// Подавляет исключения <see cref="IOException"/>, <see cref="ObjectDisposedException"/> и <see cref="InvalidOperationException"/>,
    /// которые могут возникнуть при отключении клиента во время отправки.
    /// </summary>
    /// <param name="data">Данные для отправки.</param>
    public void Send(ReadOnlyMemory<byte> data)
    {
        if (_client?.Socket == null) return;

        var array = data.ToArray();
        try
        {
            _client.Socket.AsyncSend(array, 0, array.Length,
                new SocketSendCallback(_client.ServerWriteCallBack), null);
        }
        catch (IOException) { }
        catch (ObjectDisposedException) { }
        catch (InvalidOperationException) { }
    }
}