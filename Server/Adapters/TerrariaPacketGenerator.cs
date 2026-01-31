using Terraria;
using Terraria.Localization;
using PacketManager.Core.Abstractions;
using PacketManager.Core.Data;

namespace PacketManager.Server.Adapters;

/// <summary>
/// Генератор пакетов для Terraria, использующий IL-хуки для перехвата оригинальных байтов
/// и создания кастомных пакетов через билдеры.
/// </summary>
/// <remarks>
/// Использует статические поля для хранения перехваченных данных, так как <see cref="Terraria.NetMessage.SendData(int, int, int, NetworkText, int, float, float, float, int, int, int)"/>
/// работает через глобальное состояние игры. Потокобезопасность обеспечивается через Lock.
/// </remarks>
public class TerrariaPacketGenerator : IPacketGenerator, IDisposable
{
    private static readonly Lock Lock = new();
    private static byte[] _capturedBuffer = [];
    private static int _lastNum;
    private static readonly int NameHash = "PacketManagerAPI".GetHashCode();

    /// <summary>
    /// Генерирует оригинальный пакет, используя стандартный метод Terraria <see cref="Terraria.NetMessage.SendData(int, int, int, NetworkText, int, float, float, float, int, int, int)"/>.
    /// </summary>
    /// <param name="messageId">Идентификатор типа пакета.</param>
    /// <param name="data">Данные пакета (параметры <see cref="Terraria.NetMessage.SendData(int, int, int, NetworkText, int, float, float, float, int, int, int)"/>).</param>
    /// <returns>Массив байтов оригинального пакета, включая заголовок длины.</returns>
    /// <remarks>
    /// Вызывает <see cref="Terraria.NetMessage.SendData(int, int, int, NetworkText, int, float, float, float, int, int, int)"/> с специальным флагом ignoreClient = NameHash,
    /// что вызывает перехват байтов в <see cref="NetMessage.OnPacketWrite(int, MemoryStream, OTAPI.PacketWriter, int, int, int, NetworkText, int, float, float, float, int, int, int)"/> без реальной отправки по сети.
    /// Использует Lock для потокобезопасности.
    /// </remarks>
    public byte[] GenerateOriginal(byte messageId, PacketData data)
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
    /// Генерирует кастомный пакет, используя предоставленный билдер.
    /// </summary>
    /// <param name="builder">Билдер для генерации содержимого пакета.</param>
    /// <param name="messageId">Идентификатор типа пакета.</param>
    /// <param name="data">Оригинальные данные для передачи в контекст билдера.</param>
    /// <param name="targets">Целевые клиенты.</param>
    /// <returns>Массив байтов готового пакета с заголовком длины.</returns>
    /// <remarks>
    /// Создает <see cref="MemoryStream"/>, записывает <paramref name="messageId"/>, вызывает <see cref="IPacketBuilder.Build(IPacketBuildContext)"/>,
    /// затем возвращается в начало и записывает длину пакета.
    /// </remarks>
    public byte[] GenerateCustom(IPacketBuilder builder, byte messageId, PacketData data,
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
    /// Сохраняет перехваченный буфер байтов из оригинального метода <see cref="Terraria.NetMessage.SendData(int, int, int, NetworkText, int, float, float, float, int, int, int)"/>.
    /// </summary>
    /// <param name="buffer">Массив байтов пакета.</param>
    /// <param name="length">Фактическая длина пакета.</param>
    /// <remarks>
    /// Вызывается из <see cref="NetMessage.OnPacketWrite(int, MemoryStream, OTAPI.PacketWriter, int, int, int, NetworkText, int, float, float, float, int, int, int)"/> хука при обнаружении флага NameHash.
    /// </remarks>
    public static void CaptureBuffer(byte[] buffer, int length)
    {
        _capturedBuffer = [.. buffer.Take(length)];
    }

    /// <summary>
    /// Сохраняет вспомогательное значение Num из <see cref="NetMessage.OnPacketWrite(int, MemoryStream, OTAPI.PacketWriter, int, int, int, NetworkText, int, float, float, float, int, int, int)"/>.
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