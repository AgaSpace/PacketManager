using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using PacketManager.Core.Abstractions;
using PacketManager.Core.Data;

namespace PacketManager.Core.Implementations;

internal class PacketBuilderRegistry(int maxPlayers) : IPacketBuilderRegistry, IDisposable
{
    private readonly ConcurrentDictionary<int, PlayerBuilders> _players = new(
            Enumerable.Range(0, maxPlayers).ToDictionary(i => i, _ => new PlayerBuilders()));

    public Result AddBuilder(int playerId, IPacketBuilder builder)
    {
        if (!_players.TryGetValue(playerId, out var state))
            return Result.Failure("Invalid player ID");
        return state.Add(builder);
    }

    public Result RemoveBuilder(int playerId, IPacketBuilder builder)
    {
        if (!_players.TryGetValue(playerId, out var state))
            return Result.Failure("Player not found");
        return state.Remove(builder);
    }

    public bool HasBuilder(int playerId, byte messageId)
    {
        return _players.TryGetValue(playerId, out var state) && state.Has(messageId);
    }

    // ИЗМЕНЕНО: Возвращаем List для сохранения порядка сортировки
    public IReadOnlyList<IPacketBuilder> GetBuilders(int playerId, byte messageId)
    {
        if (!_players.TryGetValue(playerId, out var state))
            return [];
        return state.Get(messageId);
    }

    public void Dispose()
    {
        foreach (var p in _players.Values) p.Dispose();
    }

    private class PlayerBuilders : IDisposable
    {
        private readonly ConcurrentDictionary<byte, ImmutableSortedSet<IPacketBuilder>> _builders = new();
        private readonly ReaderWriterLockSlim _lock = new();

        public Result Add(IPacketBuilder builder)
        {
            var key = builder.MessageId;
            _lock.EnterUpgradeableReadLock();
            try
            {
                var set = _builders.GetOrAdd(key, _ =>
                    ImmutableSortedSet<IPacketBuilder>.Empty.WithComparer(new PriorityComparer()));

                _lock.EnterWriteLock();
                try
                {
                    _builders[key] = set.Add(builder);
                    return Result.Success();
                }
                finally { _lock.ExitWriteLock(); }
            }
            finally { _lock.ExitUpgradeableReadLock(); }
        }

        public Result Remove(IPacketBuilder builder)
        {
            var key = builder.MessageId;
            if (!_builders.TryGetValue(key, out var set))
                return Result.Failure("Not found");

            _lock.EnterWriteLock();
            try
            {
                var newSet = set.Remove(builder);
                if (newSet == set) return Result.Failure("Not found");
                if (newSet.IsEmpty) _builders.TryRemove(key, out _);
                else _builders[key] = newSet;
                return Result.Success();
            }
            finally { _lock.ExitWriteLock(); }
        }

        public bool Has(byte messageId) =>
            _builders.ContainsKey(messageId) && !_builders[messageId].IsEmpty;

        public IReadOnlyList<IPacketBuilder> Get(byte messageId) =>
            _builders.TryGetValue(messageId, out var set)
                ? set.ToList()  // ToList сохраняет порядок сортировки
                : [];

        public void Dispose() => _lock.Dispose();
    }

    // ИСПРАВЛЕНО: Считаем разными даже билдеры с одинаковым приоритетом
    private class PriorityComparer : IComparer<IPacketBuilder>
    {
        public int Compare(IPacketBuilder? x, IPacketBuilder? y)
        {
            if (ReferenceEquals(x, y)) return 0;

            int priorityCompare = (x?.Priority ?? 0).CompareTo(y?.Priority ?? 0);
            if (priorityCompare != 0)
                return priorityCompare;

            // Tie-breaker по хеш-коду объекта, чтобы можно было добавлять 
            // много билдеров с одинаковым приоритетом
            return RuntimeHelpers.GetHashCode(x).CompareTo(RuntimeHelpers.GetHashCode(y));
        }
    }
}