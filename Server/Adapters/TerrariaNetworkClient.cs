using Terraria;
using Terraria.Net.Sockets;
using PacketManager.Core.Abstractions;

namespace PacketManager.Server.Adapters;

/// <summary>
/// Адаптер клиента Terraria (RemoteClient) для интерфейса INetworkClient.
/// Оборачивает функциональность RemoteClient в абстракцию Core.
/// </summary>
public class TerrariaNetworkClient : INetworkClient
{
    private readonly RemoteClient _client;

    /// <summary>
    /// Получает идентификатор клиента (whoAmI).
    /// </summary>
    public int Id => _client.Id;

    /// <summary>
    /// Получает значение, указывающее, активно ли подключение клиента.
    /// </summary>
    public bool IsConnected => _client.IsActive;

    /// <summary>
    /// Получает оригинальный RemoteClient для доступа к специфичным методам Terraria.
    /// </summary>
    public RemoteClient Native => _client;

    /// <summary>
    /// Создает новый адаптер для RemoteClient.
    /// </summary>
    /// <param name="client">Оригинальный клиент Terraria.</param>
    public TerrariaNetworkClient(RemoteClient client)
    {
        _client = client;
    }

    /// <summary>
    /// Отправляет данные клиенту через сокет Terraria.
    /// Подавляет исключения IOException, ObjectDisposedException и InvalidOperationException,
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