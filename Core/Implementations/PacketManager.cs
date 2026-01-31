using PacketManager.Core.Abstractions;
using PacketManager.Core.Data;

namespace PacketManager.Core.Implementations;

public class PacketManager(int maxPlayers, IPacketGenerator generator, INetworkService network) : IDisposable
{
    private readonly IPacketGenerator _generator = generator;
    private readonly INetworkService _network = network;

    public IPacketBuilderRegistry Registry { get; } = new PacketBuilderRegistry(maxPlayers);
    public IPacketGenerator Generator => _generator;

    public bool ProcessOutgoingPacket(byte messageId, PacketData data, int ignoreClient, int remoteClient)
    {
        var clients = remoteClient == -1
            ? _network.GetActiveClients(ignoreClient).ToList()
            : [new DummyClient(remoteClient)];

        if (clients.Count == 0) return false;

        var groups = clients.GroupBy(c =>
            Registry.GetBuilder(c.Id, messageId),
            new PacketBuilderEqualityComparer());

        var groupList = groups.ToList();
        if (groupList.Count == 1 && groupList[0].Key == null)
            return false;

        foreach (var group in groupList)
        {
            byte[] buffer = group.Key == null
                ? _generator.GenerateOriginal(messageId, data)
                : _generator.GenerateCustom(group.Key, messageId, data, [.. group]);

            if (buffer.Length > 0)
                _network.SendTo(group, buffer);
        }

        return true;
    }

    private class PacketBuilderEqualityComparer : IEqualityComparer<IPacketBuilder?>
    {
        public bool Equals(IPacketBuilder? x, IPacketBuilder? y) => ReferenceEquals(x, y);
        public int GetHashCode(IPacketBuilder? obj) => obj?.GetHashCode() ?? 0;
    }

    private class DummyClient(int id) : INetworkClient
    {
        public int Id { get; } = id;
        public bool IsConnected => true;
        public void Send(ReadOnlyMemory<byte> data) { }
    }

    public void Dispose()
    {
        (Registry as IDisposable)?.Dispose();
    }
}