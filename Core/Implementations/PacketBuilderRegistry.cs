using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using PacketManager.Core.Abstractions;
using PacketManager.Core.Data;

namespace PacketManager.Core.Implementations;

/// <summary>
/// Потокобезопасный реестр билдеров пакетов с ленивой инициализацией.
/// Поддерживает неограниченное количество игроков (динамическое расширение).
/// </summary>
/// <remarks>
/// В отличие от предыдущих версий, не требует указания maxPlayers при создании
/// и не предзаполняет словарь. PlayerBuilders создаются по требованию (lazy),
/// что позволяет использовать библиотеку с модами, увеличивающими слоты игроков
/// (например, до 500, 1000 и более).
/// </remarks>
internal class PacketBuilderRegistry : IPacketBuilderRegistry, IDisposable
{
    private readonly ConcurrentDictionary<int, PlayerBuilders> _players = new();

    /// <summary>
    /// Получает или создает хранилище билдеров для указанного игрока.
    /// </summary>
    /// <param name="playerId">ID игрока (неотрицательное число).</param>
    /// <returns>Хранилище билдеров для игрока (создается при первом обращении).</returns>
    private PlayerBuilders GetOrCreateBuilders(int playerId)
    {
        return _players.GetOrAdd(playerId, static _ => new PlayerBuilders());
    }

    /// <summary>
    /// Добавляет билдер пакета для указанного игрока.
    /// </summary>
    /// <param name="playerId">Индекс игрока (whoAmI). Должен быть >= 0.</param>
    /// <param name="builder">Экземпляр билдера для добавления.</param>
    /// <returns>
    /// <see cref="Result.Success"/> при успешном добавлении;
    /// <see cref="Result.Failure"/> если playerId отрицательный.
    /// </returns>
    /// <remarks>
    /// Автоматически создает внутренние структуры для playerId при первом обращении.
    /// Поддерживает playerId > 255 для модов с увеличенным количеством слотов.
    /// </remarks>
    public Result AddBuilder(int playerId, IPacketBuilder builder)
    {
        if (playerId < 0)
            return Result.Failure("Player ID cannot be negative");

        var state = GetOrCreateBuilders(playerId);
        return state.Add(builder);
    }

    /// <summary>
    /// Удаляет ранее добавленный билдер у указанного игрока.
    /// </summary>
    /// <param name="playerId">Индекс игрока.</param>
    /// <param name="builder">Экземпляр билдера для удаления.</param>
    /// <returns>
    /// <see cref="Result.Success"/> при успешном удалении;
    /// <see cref="Result.Failure"/> если игрок или билдер не найдены.
    /// </returns>
    public Result RemoveBuilder(int playerId, IPacketBuilder builder)
    {
        if (!_players.TryGetValue(playerId, out var state))
            return Result.Failure("Player not found");

        return state.Remove(builder);
    }

    /// <summary>
    /// Проверяет, содержит ли реестр указанный экземпляр билдера.
    /// </summary>
    /// <param name="playerId">Индекс игрока.</param>
    /// <param name="builder">Билдер для проверки.</param>
    /// <returns>true, если билдер найден; иначе false.</returns>
    public bool Contains(int playerId, IPacketBuilder builder)
    {
        return _players.TryGetValue(playerId, out var state) && state.Contains(builder);
    }

    /// <summary>
    /// Проверяет, есть ли билдеры для указанного типа пакета у игрока.
    /// </summary>
    /// <param name="playerId">Индекс игрока.</param>
    /// <param name="messageId">Идентификатор типа пакета.</param>
    /// <returns>true, если есть хотя бы один билдер для данного типа.</returns>
    public bool HasBuilder(int playerId, byte messageId)
    {
        return _players.TryGetValue(playerId, out var state) && state.Has(messageId);
    }

    /// <summary>
    /// Получает билдер с максимальным приоритетом для указанного игрока и типа пакета.
    /// </summary>
    /// <param name="playerId">Индекс игрока.</param>
    /// <param name="messageId">Идентификатор типа пакета.</param>
    /// <returns>
    /// Билдер с наивысшим приоритетом или null, если билдеров нет.
    /// </returns>
    public IPacketBuilder? GetBuilder(int playerId, byte messageId)
    {
        if (!_players.TryGetValue(playerId, out var state))
            return null;
        return state.GetMax(messageId);
    }

    /// <summary>
    /// Освобождает ресурсы всех игроков и очищает реестр.
    /// </summary>
    public void Dispose()
    {
        foreach (var p in _players.Values)
            p.Dispose();
        _players.Clear();
    }

    /// <summary>
    /// Внутренний класс-хранилище для одного игрока.
    /// </summary>
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

        public bool Contains(IPacketBuilder builder)
        {
            return _builders.TryGetValue(builder.MessageId, out var set) && set.Contains(builder);
        }

        public bool Has(byte messageId) =>
            _builders.ContainsKey(messageId) && !_builders[messageId].IsEmpty;

        public IPacketBuilder? GetMax(byte messageId) =>
            _builders.TryGetValue(messageId, out var set) && !set.IsEmpty
                ? set.Max
                : null;

        public void Dispose() => _lock.Dispose();
    }

    private class PriorityComparer : IComparer<IPacketBuilder>
    {
        public int Compare(IPacketBuilder? x, IPacketBuilder? y)
        {
            if (ReferenceEquals(x, y)) return 0;

            int priorityCompare = (x?.Priority ?? 0).CompareTo(y?.Priority ?? 0);
            if (priorityCompare != 0)
                return priorityCompare;

            return RuntimeHelpers.GetHashCode(x).CompareTo(RuntimeHelpers.GetHashCode(y));
        }
    }
}