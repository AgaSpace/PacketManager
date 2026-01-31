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

        var clientsWithChains = clients
            .Select(c => new
            {
                Client = c,
                Builders = Registry.GetBuilders(c.Id, messageId)
            })
            .ToList();

        if (clientsWithChains.All(x => x.Builders.Count == 0))
            return false;

        var groups = clientsWithChains
            .Where(x => x.Builders.Count > 0)
            .GroupBy(x => x.Builders, new BuilderListEqualityComparer());

        foreach (var group in groups)
        {
            var builders = group.Key;
            var groupClients = group.Select(x => x.Client).ToList();

            var buffer = _generator.GenerateCustom(builders, messageId, data, groupClients);

            if (buffer.Length > 0)
                _network.SendTo(groupClients, buffer);
        }

        // Обрабатываем клиентов без билдеров (если есть) — отправляем оригинал
        var noBuilderClients = clientsWithChains
            .Where(x => x.Builders.Count == 0)
            .Select(x => x.Client)
            .ToList();

        if (noBuilderClients.Count > 0)
        {
            var originalBuffer = _generator.GenerateOriginal(messageId, data);
            _network.SendTo(noBuilderClients, originalBuffer);
        }

        return true;
    }

    // Компаратор для сравнения списков билдеров по ссылкам (для группировки)
    private class BuilderListEqualityComparer : IEqualityComparer<IReadOnlyList<IPacketBuilder>>
    {
        public bool Equals(IReadOnlyList<IPacketBuilder>? x, IReadOnlyList<IPacketBuilder>? y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (x == null || y == null) return false;
            if (x.Count != y.Count) return false;

            // Сравниваем по ссылкам элементов
            for (int i = 0; i < x.Count; i++)
            {
                if (!ReferenceEquals(x[i], y[i]))
                    return false;
            }
            return true;
        }

        public int GetHashCode(IReadOnlyList<IPacketBuilder> obj)
        {
            int hash = 17;
            foreach (var builder in obj)
            {
                hash = hash * 31 + (builder?.GetHashCode() ?? 0);
            }
            return hash;
        }
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