namespace PacketManager.Core.Abstractions;

public interface INetworkService
{
    void SendTo(IEnumerable<INetworkClient> clients, ReadOnlyMemory<byte> data);
    IEnumerable<INetworkClient> GetActiveClients(int? excludeId = null);
}
