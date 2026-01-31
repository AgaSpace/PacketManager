namespace PacketManager.Core.Abstractions;

/// <summary>
/// Представляет сетевого клиента (игрока/подключение).
/// Абстракция над конкретной реализацией клиента (RemoteClient в Terraria).
/// </summary>
public interface INetworkClient
{
    /// <summary>
    /// Получает уникальный идентификатор клиента (whoAmI).
    /// </summary>
    int Id { get; }

    /// <summary>
    /// Получает значение, указывающее, подключен ли клиент в данный момент.
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// Отправляет данные клиенту.
    /// </summary>
    /// <param name="data">Данные для отправки.</param>
    void Send(ReadOnlyMemory<byte> data);
}