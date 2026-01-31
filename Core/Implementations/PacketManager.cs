using PacketManager.Core.Abstractions;
using PacketManager.Core.Data;

namespace PacketManager.Core.Implementations;

/// <summary>
/// Основной класс для управления исходящими сетевыми пакетами.
/// Отвечает за маршрутизацию (custom vs original), группировку клиентов по билдерам
/// и отправку сгенерированных пакетов через INetworkService.
/// </summary>
/// <remarks>
/// Является точкой входа для обработки всех исходящих пакетов.
/// Потокобезопасен при условии использования потокобезопасных реализаций зависимостей.
/// </remarks>
public class PacketManager(IPacketGenerator generator, INetworkService network) : IDisposable
{
    private readonly IPacketGenerator _generator = generator;
    private readonly INetworkService _network = network;

    /// <summary>
    /// Получает реестр билдеров для управления ими (добавление, удаление, проверка).
    /// </summary>
    /// <value>Экземпляр <see cref="IPacketBuilderRegistry"/>, созданный при конструировании.</value>
    public IPacketBuilderRegistry Registry { get; } = new PacketBuilderRegistry();

    /// <summary>
    /// Получает генератор пакетов, используемый для сериализации пакетов в байты.
    /// </summary>
    /// <value>Экземпляр <see cref="IPacketGenerator"/>, переданный в конструктор.</value>
    public IPacketGenerator Generator => _generator;

    /// <summary>
    /// Обрабатывает исходящий пакет: группирует клиентов по их билдерам,
    /// генерирует кастомные или оригинальные пакеты для каждой группы и отправляет их.
    /// </summary>
    /// <param name="messageId">Идентификатор типа пакета (Message ID).</param>
    /// <param name="data">Оригинальные данные пакета, полученные из SendData.</param>
    /// <param name="ignoreClient">Индекс клиента, которому не следует отправлять пакет (-1 если не требуется).</param>
    /// <param name="remoteClient">Целевой клиент (-1 для широковещательной отправки всем, кроме ignoreClient).</param>
    /// <returns>
    /// <c>true</c>, если пакет был обработан и отправлен через PacketManager (имеются кастомные билдеры);
    /// <c>false</c>, если у всех клиентов отсутствуют билдеры (используется оригинальная логика игры).
    /// </returns>
    /// <remarks>
    /// Логика работы:
    /// <list type="number">
    /// <item>Получает список целевых клиентов (все активные или конкретный remoteClient).</item>
    /// <item>Для каждого клиента получает его билдер с максимальным приоритетом через <see cref="Registry.GetBuilder"/>.</item>
    /// <item>Группирует клиентов с одинаковыми билдерами (ReferenceEquals) для оптимизации - один пакет на группу.</item>
    /// <item>Для каждой группы вызывает <see cref="IPacketGenerator.GenerateCustom"/> (если есть билдер) или <see cref="IPacketGenerator.GenerateOriginal"/> (если билдер null).</item>
    /// <item>Отправляет сгенерированные байты всем клиентам группы через <see cref="INetworkService.SendTo"/>.</item>
    /// </list>
    /// </remarks>
    public bool ProcessOutgoingPacket(int messageId, PacketData data, int ignoreClient, int remoteClient)
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

    /// <summary>
    /// Компаратор для сравнения билдеров по ссылке (ReferenceEquals).
    /// Используется в <see cref="ProcessOutgoingPacket"/> для GroupBy, 
    /// чтобы объединить клиентов с одним и тем же экземпляром билдера.
    /// </summary>
    private class PacketBuilderEqualityComparer : IEqualityComparer<IPacketBuilder?>
    {
        /// <summary>
        /// Определяет, равны ли два билдера (по ссылке).
        /// </summary>
        /// <param name="x">Первый билдер.</param>
        /// <param name="y">Второй билдер.</param>
        /// <returns>true, если ссылки равны; иначе false.</returns>
        public bool Equals(IPacketBuilder? x, IPacketBuilder? y) => ReferenceEquals(x, y);

        /// <summary>
        /// Получает хеш-код объекта билдера.
        /// </summary>
        /// <param name="obj">Билдер.</param>
        /// <returns>Хеш-код объекта или 0 для null.</returns>
        public int GetHashCode(IPacketBuilder? obj) => obj?.GetHashCode() ?? 0;
    }

    /// <summary>
    /// Заглушка клиента для сценария единичной отправки (когда <paramref name="remoteClient"/> != -1).
    /// Не выполняет реальной отправки, служит только для передачи ID в группировку.
    /// </summary>
    private class DummyClient(int id) : INetworkClient
    {
        /// <summary>
        /// Получает идентификатор клиента.
        /// </summary>
        public int Id { get; } = id;

        /// <summary>
        /// Всегда возвращает true, так как это синтетический клиент для группировки.
        /// </summary>
        public bool IsConnected => true;

        /// <summary>
        /// Не выполняет никаких действий (заглушка).
        /// </summary>
        /// <param name="data">Игнорируется.</param>
        public void Send(ReadOnlyMemory<byte> data) { }
    }

    /// <summary>
    /// Освобождает ресурсы, занятые реестром билдеров.
    /// </summary>
    /// <remarks>
    /// Вызывает Dispose у <see cref="Registry"/>, если он реализует IDisposable.
    /// Это освобождает ReaderWriterLockSlim'ы всех игроков.
    /// </remarks>
    public void Dispose()
    {
        (Registry as IDisposable)?.Dispose();
    }
}