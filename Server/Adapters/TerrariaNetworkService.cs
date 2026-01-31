#region Using

using Terraria;

using PacketManager.Core.Abstractions;

#endregion

namespace PacketManager.Server.Adapters;

public class TerrariaNetworkService : INetworkService
{
    public void SendTo(IEnumerable<INetworkClient> clients, ReadOnlyMemory<byte> data)
    {
        foreach (var client in clients.Cast<TerrariaNetworkClient>())
        {
            client.Send(data);
        }
    }

    public IEnumerable<INetworkClient> GetActiveClients(int? excludeId = null)
    {
        return Netplay.Clients
            .Where(c => c?.IsActive == true && c.Id != excludeId)
            .Select(c => new TerrariaNetworkClient(c));
    }
}
