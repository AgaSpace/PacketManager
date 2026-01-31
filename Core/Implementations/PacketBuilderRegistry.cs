using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using PacketManager.Core.Abstractions;
using PacketManager.Core.Data;

namespace PacketManager.Core.Implementations;

/// <summary>
/// Потокобезопасная реализация реестра билдеров пакетов.
/// Хранит коллекцию билдеров для каждого игрока отдельно, организованную по типам пакетов (MessageId).
/// Поддерживает сортировку по приоритету и выборку билдера с максимальным приоритетом.
/// </summary>
/// <remarks>
/// Использует <see cref="ConcurrentDictionary{TKey,TValue}"/> для потокобезопасного доступа к игрокам
/// и <see cref="ReaderWriterLockSlim"/> для синхронизации доступа к коллекции билдеров конкретного игрока.
/// Внутреннее хранилище использует <see cref="ImmutableSortedSet{T}"/> для эффективной сортировки по приоритету.
/// </remarks>
/// <remarks>
/// Создает новый экземпляр реестра с указанным максимальным количеством игроков.
/// </remarks>
/// <param name="maxPlayers">Максимальное количество игроков (размер массива/пула).</param>
internal class PacketBuilderRegistry(int maxPlayers) : IPacketBuilderRegistry, IDisposable
{
    private readonly ConcurrentDictionary<int, PlayerBuilders> _players = new(
            Enumerable.Range(0, maxPlayers).ToDictionary(i => i, _ => new PlayerBuilders()));

    /// <summary>
    /// Добавляет билдер пакета для указанного игрока.
    /// </summary>
    /// <param name="playerId">Индекс игрока (whoAmI), должен быть меньше maxPlayers.</param>
    /// <param name="builder">Экземпляр билдера для добавления. Не может быть null.</param>
    /// <returns>
    /// <see cref="Result.Success"/> при успешном добавлении;
    /// <see cref="Result.Failure"/> с сообщением "Invalid player ID", если индекс вне диапазона.
    /// </returns>
    /// <remarks>
    /// Можно добавить несколько билдеров с одинаковым MessageId и даже одинаковым приоритетом.
    /// Они различаются по ссылке (hash code), что позволяет хранить в одном SortedSet множество билдеров.
    /// </remarks>
    public Result AddBuilder(int playerId, IPacketBuilder builder)
    {
        if (!_players.TryGetValue(playerId, out var state))
            return Result.Failure("Invalid player ID");
        return state.Add(builder);
    }

    /// <summary>
    /// Удаляет ранее добавленный билдер у указанного игрока.
    /// </summary>
    /// <param name="playerId">Индекс игрока.</param>
    /// <param name="builder">Тот же экземпляр билдера, который был добавлен ранее (сравнение по ссылке).</param>
    /// <returns>
    /// <see cref="Result.Success"/> при успешном удалении;
    /// <see cref="Result.Failure"/> с сообщением "Player not found" или "Not found", 
    /// если игрок или билдер не найдены.
    /// </returns>
    public Result RemoveBuilder(int playerId, IPacketBuilder builder)
    {
        if (!_players.TryGetValue(playerId, out var state))
            return Result.Failure("Player not found");
        return state.Remove(builder);
    }

    /// <summary>
    /// Проверяет, содержит ли реестр указанный экземпляр билдера для конкретного игрока.
    /// </summary>
    /// <param name="playerId">Индекс игрока.</param>
    /// <param name="builder">Экземпляр билдера для проверки (сравнение по ссылке).</param>
    /// <returns>true, если указанный билдер присутствует в реестре для данного игрока; иначе false.</returns>
    public bool Contains(int playerId, IPacketBuilder builder)
    {
        return _players.TryGetValue(playerId, out var state) && state.Contains(builder);
    }

    /// <summary>
    /// Проверяет, есть ли хотя бы один билдер для указанного типа пакета у игрока.
    /// </summary>
    /// <param name="playerId">Индекс игрока.</param>
    /// <param name="messageId">Идентификатор типа пакета (Message ID).</param>
    /// <returns>true, если для данного типа пакета зарегистрирован хотя бы один билдер.</returns>
    public bool HasBuilder(int playerId, byte messageId)
    {
        return _players.TryGetValue(playerId, out var state) && state.Has(messageId);
    }

    /// <summary>
    /// Получает билдер с максимальным приоритетом для указанного типа пакета и игрока.
    /// </summary>
    /// <param name="playerId">Индекс игрока.</param>
    /// <param name="messageId">Идентификатор типа пакета.</param>
    /// <returns>
    /// Билдер с наивысшим значением <see cref="IPacketBuilder.Priority"/> для данного типа пакета;
    /// или null, если для данного типа нет зарегистрированных билдеров.
    /// </returns>
    /// <remarks>
    /// Использует <see cref="ImmutableSortedSet{T}.Max"/> для получения элемента с максимальным приоритетом
    /// за O(log n) или O(1) (амортизированно).
    /// </remarks>
    public IPacketBuilder? GetBuilder(int playerId, byte messageId)
    {
        if (!_players.TryGetValue(playerId, out var state))
            return null;
        return state.GetMax(messageId);
    }

    /// <summary>
    /// Освобождает ресурсы реестра, включая все ReaderWriterLock экземпляров игроков.
    /// </summary>
    public void Dispose()
    {
        foreach (var p in _players.Values) p.Dispose();
    }

    /// <summary>
    /// Внутренний класс, управляющий коллекцией билдеров для одного конкретного игрока.
    /// </summary>
    /// <remarks>
    /// Использует ReaderWriterLockSlim для оптимизации чтения (множественные читатели) 
    /// и записи (эксклюзивный доступ).
    /// </remarks>
    private class PlayerBuilders : IDisposable
    {
        private readonly ConcurrentDictionary<byte, ImmutableSortedSet<IPacketBuilder>> _builders;
        private readonly ReaderWriterLockSlim _lock;

        /// <summary>
        /// Создает новый экземпляр хранилища билдеров для игрока.
        /// </summary>
        public PlayerBuilders()
        {
            _builders = new ConcurrentDictionary<byte, ImmutableSortedSet<IPacketBuilder>>();
            _lock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
        }

        /// <summary>
        /// Добавляет билдер в коллекцию данного игрока.
        /// </summary>
        /// <param name="builder">Билдер для добавления.</param>
        /// <returns>Результат операции.</returns>
        /// <remarks>
        /// Использует UpgradeableReadLock для безопасной проверки существования ключа
        /// с последующим переходом на WriteLock только если требуется создание нового SortedSet.
        /// </remarks>
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

        /// <summary>
        /// Удаляет билдер из коллекции данного игрока.
        /// </summary>
        /// <param name="builder">Билдер для удаления.</param>
        /// <returns>Результат операции.</returns>
        /// <remarks>
        /// Если после удаления коллекция становится пустой, ключ удаляется из словаря для экономии памяти.
        /// </remarks>
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

        /// <summary>
        /// Проверяет наличие конкретного экземпляра билдера в коллекции.
        /// </summary>
        /// <param name="builder">Билдер для проверки.</param>
        /// <returns>true, если билдер найден.</returns>
        public bool Contains(IPacketBuilder builder)
        {
            return _builders.TryGetValue(builder.MessageId, out var set) && set.Contains(builder);
        }

        /// <summary>
        /// Проверяет, есть ли билдеры для указанного типа сообщения.
        /// </summary>
        /// <param name="messageId">Идентификатор типа пакета.</param>
        /// <returns>true, если есть хотя бы один билдер.</returns>
        public bool Has(byte messageId)
        {
            return _builders.ContainsKey(messageId) && !_builders[messageId].IsEmpty;
        }

        /// <summary>
        /// Получает билдер с максимальным приоритетом для указанного типа сообщения.
        /// </summary>
        /// <param name="messageId">Идентификатор типа пакета.</param>
        /// <returns>Билдер с max приоритетом или null.</returns>
        public IPacketBuilder? GetMax(byte messageId)
        {
            return _builders.TryGetValue(messageId, out var set) && !set.IsEmpty
                ? set.Max
                : null;
        }

        /// <summary>
        /// Освобождает ReaderWriterLockSlim.
        /// </summary>
        public void Dispose() => _lock.Dispose();
    }

    /// <summary>
    /// Компаратор для сортировки билдеров по приоритету.
    /// </summary>
    /// <remarks>
    /// При равных приоритетах использует RuntimeHelpers.GetHashCode как tie-breaker,
    /// что позволяет хранить в SortedSet несколько билдеров с одинаковым Priority.
    /// </remarks>
    private class PriorityComparer : IComparer<IPacketBuilder>
    {
        /// <summary>
        /// Сравнивает два билдера по приоритету.
        /// </summary>
        /// <param name="x">Первый билдер.</param>
        /// <param name="y">Второй билдер.</param>
        /// <returns>
        /// Отрицательное значение, если x меньше y;
        /// ноль, если ссылки равны;
        /// положительное значение, если x больше y.
        /// </returns>
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