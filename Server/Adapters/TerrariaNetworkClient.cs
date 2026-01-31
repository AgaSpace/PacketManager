#region Using

using Terraria;
using Terraria.Net.Sockets;

using PacketManager.Core.Abstractions;

#endregion

namespace PacketManager.Server.Adapters;

public class TerrariaNetworkClient(RemoteClient client) : INetworkClient
{
    private readonly RemoteClient _client = client;

    public int Id => _client.Id;
    public bool IsConnected => _client.IsActive;
    public RemoteClient Native => _client;

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
