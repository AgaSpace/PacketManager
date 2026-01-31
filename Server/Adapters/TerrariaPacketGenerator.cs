using Terraria;
using Terraria.Localization;
using PacketManager.Core.Abstractions;
using PacketManager.Core.Data;

namespace PacketManager.Server.Adapters;

/// <summary>
/// Генератор пакетов для <see cref="Terraria"/>, использующий <see cref="IL"/>-хуки для перехвата оригинальных байтов
/// и создания кастомных пакетов через билдеры.
/// </summary>
/// <remarks>
/// Использует статические поля для хранения перехваченных данных, так как <see cref="Terraria.NetMessage.SendData"/>
/// работает через глобальное состояние игры. Потокобезопасность обеспечивается через <see cref="Lock"/>.
/// </remarks>
public class TerrariaPacketGenerator : IPacketGenerator, IDisposable
{
    private static readonly Lock Lock = new();
    private static byte[] _capturedBuffer = [];
    private static int _lastNum;
    private static readonly int NameHash = "PacketManagerAPI".GetHashCode();

    /// <summary>
    /// Генерирует оригинальный пакет, используя стандартный метод <see cref="Terraria"/> <see cref="NetMessage.SendData"/>.
    /// </summary>
    /// <param name="messageId">Идентификатор типа пакета.</param>
    /// <param name="data">Данные пакета (параметры <see cref="NetMessage.SendData"/>).</param>
    /// <returns>Массив байтов оригинального пакета, включая заголовок длины.</returns>
    /// <remarks>
    /// Вызывает <see cref="NetMessage.SendData"/> с специальным флагом ignoreClient = <see cref="NameHash"/>,
    /// что вызывает перехват байтов в <see cref="NetMessage.OnPacketWrite"/> без реальной отправки по сети.
    /// Использует <see cref="Lock"/> для потокобезопасности.
    /// </remarks>
    public byte[] GenerateOriginal(int messageId, PacketData data)
    {
        lock (Lock)
        {
            NetMessage.orig_SendData(
                messageId,
                data.RemoteClient,
                NameHash,
                data.Text != null ? NetworkText.FromLiteral(data.Text) : null,
                data.Number,
                data.Number2,
                data.Number3,
                data.Number4,
                data.Number5,
                data.Number6,
                data.Number7
            );

            return [.. _capturedBuffer];
        }
    }

    /// <summary>
    /// Генерирует кастомный пакет, используя предоставленный <see cref="IPacketBuilder"/>.
    /// </summary>
    /// <param name="builder"><see cref="IPacketBuilder"/> для генерации содержимого пакета.</param>
    /// <param name="messageId">Идентификатор типа пакета.</param>
    /// <param name="data">Оригинальные данные для передачи в контекст <see cref="IPacketBuilder"/>.</param>
    /// <param name="targets">Целевые клиенты.</param>
    /// <returns>Массив байтов готового пакета с заголовком длины.</returns>
    /// <remarks>
    /// Создает <see cref="MemoryStream"/>, записывает <paramref name="messageId"/>, вызывает <see cref="IPacketBuilder.Build"/>,
    /// затем возвращается в начало и записывает длину пакета.
    /// </remarks>
    public byte[] GenerateCustom(IPacketBuilder builder, int messageId, PacketData data,
        IReadOnlyCollection<INetworkClient> targets)
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        ms.Position = 2;
        writer.Write(messageId);

        var terrariaTargets = targets.Cast<TerrariaNetworkClient>().ToList();

        var context = new TerrariaBuildContext(messageId, writer, terrariaTargets, data, _lastNum);
        builder.Build(context);

        var len = (short)ms.Position;
        ms.Position = 0;
        writer.Write(len);

        return ms.ToArray();
    }

    /// <summary>
    /// Сохраняет перехваченный буфер байтов из оригинального метода <see cref="NetMessage.SendData"/>.
    /// </summary>
    /// <param name="buffer">Массив байтов пакета.</param>
    /// <param name="length">Фактическая длина пакета.</param>
    /// <remarks>
    /// Вызывается из <see cref="NetMessage.OnPacketWrite"/> хука при обнаружении флага NameHash.
    /// </remarks>
    public static void CaptureBuffer(byte[] buffer, int length)
    {
        _capturedBuffer = [.. buffer.Take(length)];
    }

    /// <summary>
    /// Сохраняет вспомогательное значение Num из <see cref="NetMessage.OnPacketWrite"/>.
    /// </summary>
    /// <param name="num">Значение параметра num.</param>
    public static void SetLastNum(int num) => _lastNum = num;

    /// <summary>
    /// Получает хеш-идентификатор для маркировки перехваченных пакетов.
    /// </summary>
    /// <returns>Хеш строки "PacketManagerAPI".</returns>
    public static int GetNameHash() => NameHash;

    /// <summary>
    /// Освобождает ресурсы генератора.
    /// </summary>
    public void Dispose() { }
}